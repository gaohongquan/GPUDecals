using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yunchang
{
    public class ClusterPrepass : BaseRendererPass
    {
        int _ClusterNumXId;
        int _ClusterNumYId;
        int _ClusterNumZId;
        int _zCullingRangeId;
        int _ClusterRateId;
        int _CullAABBsBufferOutId;

        CommandBuffer _cb;
        
        float _ClusterZRange;
        int _CameraPixelWidth, _CameraPixelHeight;

        public ClusterData data { get; set; }

        public override void Initialize()
        {
            base.Initialize();
            _CameraPixelWidth = 0;
            _CameraPixelHeight = 0;
            _ClusterZRange = 0;

            _ClusterNumXId = Shader.PropertyToID("g_ClusterNumX");
            _ClusterNumYId = Shader.PropertyToID("g_ClusterNumY");
            _ClusterNumZId = Shader.PropertyToID("g_ClusterNumZ");
            _zCullingRangeId = Shader.PropertyToID("g_zCullingRange");
            _ClusterRateId = Shader.PropertyToID("g_ClusterRate");
            _CullAABBsBufferOutId = Shader.PropertyToID("g_CullAABBsBufferOut");

            _cb = new CommandBuffer { name = "Cluster Prepass"};
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
            if (data.CullingCS == null)
                return;

            if (_CameraPixelWidth != rendererFeatures.rawCamera.pixelWidth 
                || _CameraPixelHeight != rendererFeatures.rawCamera.pixelHeight
                || _ClusterZRange != data.ClusterZRange)
            {
                _ClusterZRange = data.ClusterZRange;
                _CameraPixelWidth = rendererFeatures.rawCamera.pixelWidth;
                _CameraPixelHeight = rendererFeatures.rawCamera.pixelHeight;

                int generateClusterKernel = data.CullingCS.FindKernel("GenerateClusterCS");
                var aabbBuffer = rendererFeatures.bufferCache.Alloc<AABB>(ComputeBufferID.CullAABBsBufferID,
                    data.clusterDimensions.count);
                _cb.SetGlobalInt(_ClusterNumXId, data.clusterDimensions.x);
                _cb.SetGlobalInt(_ClusterNumYId, data.clusterDimensions.y);
                _cb.SetGlobalInt(_ClusterNumZId, data.clusterDimensions.z);
                _cb.SetGlobalFloat(_zCullingRangeId, data.ClusterZRange);
                _cb.SetGlobalFloat(_ClusterRateId, data.ClusterRate);
                _cb.SetComputeBufferParam(data.CullingCS, generateClusterKernel,
                    _CullAABBsBufferOutId, aabbBuffer);
                _cb.DispatchCompute(data.CullingCS, generateClusterKernel,
                    data.clusterDimensions.x,
                    data.clusterDimensions.y,
                    data.clusterDimensions.z);
            }
        }
    }
}
