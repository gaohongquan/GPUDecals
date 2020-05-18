using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Yunchang;

namespace Yunchang
{
    public class DecalsRendererPass : BaseRendererPass
    {
        const int _MAX_VISIBLE_DECAL_COUNT = 8;

        int _AdditiveDecalCountId;
        int _DecalBaseMapId;
        int _DecalNormalMapId;
        //cluster
        int _PreClusterMaxNumElementsId;
        int _PreClusterMaxNumIndexId;
        int _CullTestSpheresNumId;
        int _DecalSpheresBufferId;
        int _CullAABBsBufferId;
        int _AdditiveDecalIndexBufferOutId;
        int _AdditiveDecalIndexBuffer;
        int _DecalClusterMaxNumElementsId;

        CommandBuffer _cb;

        Matrix4x4[] _worldToLocals;
        Vector4[] _uvs;
        float[] _alphas;
        float[] _normalIntensitys;

        public bool clusterEnable { get; set; }
        public bool structBufferEnable { get; set; }
        public DecalDrawData decalDrawData { get; set; }
        public ClusterData clusterData { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            _AdditiveDecalCountId = Shader.PropertyToID("g_AdditiveDecalCount");
            _DecalBaseMapId = Shader.PropertyToID("g_DecalBaseMap");
            _DecalNormalMapId = Shader.PropertyToID("g_DecalNormalMap");

            _PreClusterMaxNumElementsId = Shader.PropertyToID("g_PreClusterMaxNumElements");
            _PreClusterMaxNumIndexId = Shader.PropertyToID("g_PreClusterMaxNumIndex");
            _CullTestSpheresNumId = Shader.PropertyToID("g_CullTestSpheresNum");
            _DecalSpheresBufferId = Shader.PropertyToID("g_TestSpheresBuffer");
            _CullAABBsBufferId = Shader.PropertyToID("g_CullAABBsBuffer");
            _AdditiveDecalIndexBufferOutId = Shader.PropertyToID("g_CullIndexBufferOut");
            _AdditiveDecalIndexBuffer = Shader.PropertyToID("g_AdditiveDecalIndexBuffer");
            _DecalClusterMaxNumElementsId = Shader.PropertyToID("g_DecalClusterMaxNumElements");

            _worldToLocals = new Matrix4x4[_MAX_VISIBLE_DECAL_COUNT];
            _uvs = new Vector4[_MAX_VISIBLE_DECAL_COUNT];
            _alphas = new float[_MAX_VISIBLE_DECAL_COUNT];
            _normalIntensitys = new float[_MAX_VISIBLE_DECAL_COUNT];

            _cb = new CommandBuffer { name = "Decals Renderer" };
            rendererFeatures.rawCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _cb);
        }

        public override void Release()
        {
            base.Release();
            rendererFeatures.rawCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _cb);
            _cb.Dispose();
        }


        public override void Clear()
        {
            _cb.Clear();
        }

        public override void Execute()
        {
            if (decalDrawData.visibleDecalRenderers.Count == 0)
            {
                _cb.DisableShaderKeyword("_ADDITIONAL_DECALS");
                _cb.SetGlobalInt(_AdditiveDecalCountId, 0);
                return;
            }

            _cb.EnableShaderKeyword("_ADDITIONAL_DECALS");

            if (clusterEnable)
                ClusterDecals();

            if (structBufferEnable)
                DrawStructBuffer();
            else
                DrawConstantBuffer();

            var decalBaseMap = new RenderTargetIdentifier(decalDrawData.decalBaseMap);
            var decalNormalMap = new RenderTargetIdentifier(decalDrawData.decalNormalMap);
            _cb.SetGlobalTexture(_DecalBaseMapId, decalBaseMap);
            _cb.SetGlobalTexture(_DecalNormalMapId, decalNormalMap);
        }

        private void ClusterDecals()
        {
            if (clusterData.CullingCS == null)
                return;
            int visibleCount = decalDrawData.visibleDecalRenderers.Count;
            NativeArray<float4> decalSpheres = new NativeArray<float4>(visibleCount, Allocator.Temp);
            for (int i = 0; i < visibleCount; i++)
            {
                decalSpheres[i] = decalDrawData.visibleDecalRenderers[i].sphere;
            }
            int clusterCullingKernel = clusterData.CullingCS.FindKernel("ClusterCullingCS");
            int maxNumElements = decalDrawData.perClusterMaxDecalCount + 1;

            var decalSpheresCB = rendererFeatures.bufferCache.Alloc<Vector4>("_DecalSpheresBufferId", visibleCount);
            decalSpheresCB.SetData(decalSpheres,0, 0, visibleCount);

            var decalIndesBuffer = rendererFeatures.bufferCache.Alloc<int>(ComputeBufferID.AdditiveDecalIndexBufferID,
                maxNumElements * clusterData.clusterDimensions.count);

            _cb.SetComputeIntParam(clusterData.CullingCS,_PreClusterMaxNumElementsId, maxNumElements);
            _cb.SetComputeIntParams(clusterData.CullingCS,_PreClusterMaxNumIndexId, decalDrawData.perClusterMaxDecalCount);
            _cb.SetComputeIntParams(clusterData.CullingCS,_CullTestSpheresNumId, visibleCount);

            _cb.SetComputeBufferParam(clusterData.CullingCS, clusterCullingKernel,
                _CullAABBsBufferId, rendererFeatures.bufferCache.Get(ComputeBufferID.CullAABBsBufferID));
            _cb.SetComputeBufferParam(clusterData.CullingCS, clusterCullingKernel,
                _DecalSpheresBufferId, decalSpheresCB);
            _cb.SetComputeBufferParam(clusterData.CullingCS, clusterCullingKernel,
                _AdditiveDecalIndexBufferOutId, decalIndesBuffer);

            _cb.DispatchCompute(clusterData.CullingCS, clusterCullingKernel,
                clusterData.clusterDimensions.x,
                clusterData.clusterDimensions.y,
                clusterData.clusterDimensions.z);

            _cb.SetGlobalInt(_DecalClusterMaxNumElementsId, maxNumElements);
            _cb.SetGlobalBuffer(_AdditiveDecalIndexBuffer, decalIndesBuffer);

            decalSpheres.Dispose();
        }

        private void DrawStructBuffer()
        {
            int visibleCount = decalDrawData.visibleDecalRenderers.Count;
            NativeArray<DecalData> bufferData = new NativeArray<DecalData>(visibleCount, Allocator.Temp);
            for (int i = 0; i < visibleCount; i++)
            {
                DecalData _data = new DecalData();
                _data.worldToLocal = decalDrawData.visibleDecalRenderers[i].transform.worldToLocalMatrix;
                _data.alpha = decalDrawData.visibleDecalRenderers[i].alpha;
                _data.normalIntensity = decalDrawData.visibleDecalRenderers[i].normalIntensity;
                _data.uv = decalDrawData.visibleDecalRenderers[i].uv;
                bufferData[i] = _data;
            }

            var decalDatasBuffer = rendererFeatures.bufferCache.Alloc<DecalData>("g_AdditiveDecalDatasBuffer", visibleCount);
            decalDatasBuffer.SetData(bufferData,0,0, visibleCount);

            _cb.SetGlobalBuffer("g_AdditiveDecalDatasBuffer", decalDatasBuffer);
            _cb.SetGlobalInt(_AdditiveDecalCountId, visibleCount);
            bufferData.Dispose();
        }

        private void DrawConstantBuffer()
        {
            int visibleCount = Mathf.Min(_MAX_VISIBLE_DECAL_COUNT, decalDrawData.visibleDecalRenderers.Count);
            for (int i = 0; i < visibleCount; i++)
            {
                _worldToLocals[i] = decalDrawData.visibleDecalRenderers[i].transform.worldToLocalMatrix;
                _uvs[i] = decalDrawData.visibleDecalRenderers[i].uv;
                _alphas[i] = decalDrawData.visibleDecalRenderers[i].alpha;
                _normalIntensitys[i] = decalDrawData.visibleDecalRenderers[i].normalIntensity;
            }
            _cb.SetGlobalMatrixArray("g_DecalWorldToLocals", _worldToLocals);
            _cb.SetGlobalVectorArray("g_Decaluvs", _uvs);
            _cb.SetGlobalFloatArray("g_DecalAlphas", _alphas);
            _cb.SetGlobalFloatArray("g_DecalNormalIntensitys", _normalIntensitys);
            _cb.SetGlobalInt(_AdditiveDecalCountId, visibleCount);
        }

        struct DecalData
        {
            public float4x4 worldToLocal;
            public float4 uv;
            public float alpha;
            public float normalIntensity;
        }
    }
}
