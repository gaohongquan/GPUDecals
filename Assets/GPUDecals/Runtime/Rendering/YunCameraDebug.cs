using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yunchang
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class YunCameraDebug : MonoBehaviour
    {
        public bool showClusterEnable = false;

        private Material _drawClusterMaterial = null;

        private void OnEnable()
        {
            _drawClusterMaterial = new Material(Shader.Find("Yunchang/DebugCluster"));
        }

        private void OnDisable()
        {
            if (_drawClusterMaterial != null)
                Object.DestroyImmediate(_drawClusterMaterial);
            _drawClusterMaterial = null;
        }

        private void OnDrawGizmosSelected()
        {
            //DrawClusterBlock();
            DrawDecalCluster();
            //DrawDecalSpheres();
        }

        private void DrawClusterBlock()
        {
            if (showClusterEnable == false)
                return;

            var feature = this.GetComponent<RendererFeatures>();
            if (feature == null)
                return;
            var aabbs = feature.bufferCache.Get(ComputeBufferID.CullAABBsBufferID);
            if (aabbs == null || !aabbs.IsValid())
                return;

            GL.wireframe = true;
            _drawClusterMaterial.SetBuffer("ClusterAABBs", aabbs);
            _drawClusterMaterial.SetMatrix("_CameraWorldMatrix",this.transform.localToWorldMatrix);
            _drawClusterMaterial.SetPass(0);
            Graphics.DrawProcedural(MeshTopology.Points,
                feature.clusterPrepass.data.clusterDimensions.count);
            GL.wireframe = false;
        }

        private void DrawDecalCluster()
        {
            if (showClusterEnable == false)
                return;
            if (DecalsManager.Instance == null)
                return;

            var feature = this.GetComponent<RendererFeatures>();
            if (feature == null)
                return;
            var aabbs = feature.bufferCache.Get(ComputeBufferID.CullAABBsBufferID);
            if (aabbs == null || !aabbs.IsValid())
                return;
            var index = feature.bufferCache.Get(ComputeBufferID.AdditiveDecalIndexBufferID);
            if (index == null || !index.IsValid())
                return;

            int maxNumElements = feature.decalDrawData.perClusterMaxDecalCount + 1;
            AABB[] clusters = new AABB[aabbs.count];
            aabbs.GetData(clusters);
            int[] decalIndex = new int[index.count];
            index.GetData(decalIndex);

            List<AABB> list = new List<AABB>();
            for(int i=0; i<clusters.Length; i++)
            {
                if (decalIndex[i * maxNumElements] > 0 )
                    list.Add(clusters[i]);
            }
            if (list.Count == 0)
                return;

            var buffer = feature.bufferCache.Alloc<AABB>("debug_decal_cluster", list.Count);
            buffer.SetData(list);

            GL.wireframe = true;
            _drawClusterMaterial.SetBuffer("ClusterAABBs", buffer);
            _drawClusterMaterial.SetMatrix("_CameraWorldMatrix", this.transform.localToWorldMatrix);
            _drawClusterMaterial.SetPass(0);
            Graphics.DrawProcedural(MeshTopology.Points, list.Count);
            GL.wireframe = false;
        }

        private void DrawDecalSpheres()
        {
            var feature = this.GetComponent<RendererFeatures>();
            if (feature == null)
                return;
            var spheres = feature.bufferCache.Get("_DecalSpheresBufferId");
            if (spheres == null || !spheres.IsValid())
                return;

            float4[] _spheres = new float4[spheres.count];
            spheres.GetData(_spheres);
            for(int i=0; i< _spheres.Length; i++)
            {
                Gizmos.DrawWireSphere(new Vector3(_spheres[i].x, _spheres[i].y, _spheres[i].z),
                    _spheres[i].w);
            }
        }
    }
}
