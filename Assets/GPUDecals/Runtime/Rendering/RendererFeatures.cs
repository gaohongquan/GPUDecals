using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yunchang
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class RendererFeatures : MonoBehaviour
    {
        const int _MAX_ADDITIVE_DECAL_COUNT = 1024;

        public ComputeShader ClusterCS;
        [Range(0, 1)]
        public float ClusterZRange = 0.1f;//1.0f;

        DecalsSortComparer _DecalsSortComparer = new DecalsSortComparer();
        ComputeBufferCache _BufferCache = new ComputeBufferCache();
        DecalDrawData _DecalDrawData = new DecalDrawData();
        ClusterData _ClusterData = new ClusterData();
        ClusterPrepass _ClusterPrepass;
        DecalsRendererPass _DecalsRenderer;

        CullingGroup _DecalCulling;
        BoundingSphere[] _DecalSphereCache;
        List<int> _VisibleDecalIndexCahce = new List<int>();
        int[] _VisibleDecalIndex;

        Camera _Camera;
        bool _SupportsComputeShaders;

        public ComputeBufferCache bufferCache => _BufferCache;
        public DecalDrawData decalDrawData => _DecalDrawData;
        public ClusterPrepass clusterPrepass => _ClusterPrepass;
        public DecalsRendererPass decalsRenderer => _DecalsRenderer;
        public Camera rawCamera => _Camera; 

        void OnEnable()
        {
            _SupportsComputeShaders = SystemInfo.supportsComputeShaders;
            _Camera = GetComponent<Camera>();

            _DecalSphereCache = new BoundingSphere[_MAX_ADDITIVE_DECAL_COUNT];
            _VisibleDecalIndex = new int[_MAX_ADDITIVE_DECAL_COUNT];
            _DecalCulling = new CullingGroup();
            _DecalCulling.targetCamera = _Camera;
            _DecalCulling.SetBoundingSpheres(_DecalSphereCache);
            _DecalCulling.SetBoundingSphereCount(0);
            _DecalCulling.onStateChanged += OnDecalCullingChanged;
            _VisibleDecalIndexCahce.Clear();

            _ClusterPrepass = new ClusterPrepass { rendererFeatures = this };
            _ClusterPrepass.Initialize();
            _DecalsRenderer = new DecalsRendererPass { rendererFeatures = this };
            _DecalsRenderer.Initialize();
        }

        void OnDisable()
        {
            if(_DecalCulling != null)
            {
                _DecalCulling.Dispose();
                _DecalCulling.onStateChanged -= OnDecalCullingChanged;
                _DecalCulling = null;
            }
            if (_ClusterPrepass != null)
            {
                _ClusterPrepass.Release();
                _ClusterPrepass = null;
            }
            if (_DecalsRenderer != null)
            {
                _DecalsRenderer.Release();
                _DecalsRenderer = null;
            }
            _BufferCache.Release();
        }

        void Update()
        {
            UpdateCameraCluster();
            UpdateDeclasRendering();
        }

        void OnDecalCullingChanged(CullingGroupEvent sphere)
        {
            if (sphere.isVisible && !_VisibleDecalIndexCahce.Contains(sphere.index))
                _VisibleDecalIndexCahce.Add(sphere.index);
            else if (!sphere.isVisible && _VisibleDecalIndexCahce.Contains(sphere.index))
                _VisibleDecalIndexCahce.Remove(sphere.index);
        }

        void UpdateCameraCluster()
        {
            _ClusterData.CullingCS = ClusterCS;
            _ClusterData.ClusterZRange = ClusterZRange;
        }

        void UpdateDeclasRendering()
        {
            _DecalDrawData.visibleDecalRenderers.Clear();
            if (DecalsManager.Instance == null)
                return;

            var activeDecalRenderers = DecalsManager.Instance.activeDecalRenderers;
            int maxDecalCount = Mathf.Min(_MAX_ADDITIVE_DECAL_COUNT, activeDecalRenderers.Count);
            for (int i = 0; i < maxDecalCount; i++)
            {
                Vector4 sphere = activeDecalRenderers[i].sphere;
                _DecalSphereCache[i].position = sphere;
                _DecalSphereCache[i].radius = sphere.w;
            }
            _DecalCulling.SetBoundingSphereCount(maxDecalCount);
            int visibleCount = _VisibleDecalIndexCahce.Count; //_DecalCulling.QueryIndices(true, _VisibleDecalIndex, 0);
            for (int i = 0; i < visibleCount; i++)
            {
                int index = _VisibleDecalIndexCahce[i];
                if (index < activeDecalRenderers.Count)
                    _DecalDrawData.visibleDecalRenderers.Add(activeDecalRenderers[index]);
            }
            //_DecalDrawData.visibleDecalRenderers.Clear();
            //_DecalDrawData.visibleDecalRenderers.AddRange(activeDecalRenderers);
            _DecalDrawData.visibleDecalRenderers.Sort(_DecalsSortComparer);
            _DecalDrawData.decalBaseMap = DecalsManager.Instance.decalBaseTexture;
            _DecalDrawData.decalNormalMap = DecalsManager.Instance.decalNormalTexture;
            _DecalDrawData.perClusterMaxDecalCount = 8;
        }

        void OnPreRender()
        {
            _DecalsRenderer.Clear();
            _ClusterPrepass.Clear();

            Configure();

            if(_ClusterPrepass.enable)
                _ClusterPrepass.Execute();
            if (_DecalsRenderer.enable)
                _DecalsRenderer.Execute();
        }
        
        bool IsSceneViewCamera(Camera camera)
        {
#if UNITY_EDITOR
            var cams = UnityEditor.SceneView.GetAllSceneCameras();
            foreach (var cam in cams)
            {
                if (cam == camera)
                    return true;
            }
#endif
            return false;
        }

        void Configure()
        {
            _ClusterPrepass.data = _ClusterData;
            _DecalsRenderer.clusterData = _ClusterData;
            _DecalsRenderer.decalDrawData = _DecalDrawData;

            if(IsSceneViewCamera(_Camera))
            {
                Shader.EnableKeyword("_STRUCTURED_BUFFER_SUPPORT");
                Shader.DisableKeyword("_CULLING_CLUSTER_ON");
                _ClusterPrepass.enable = false;
                _DecalsRenderer.enable = true;
                _DecalsRenderer.clusterEnable = false;
                _DecalsRenderer.structBufferEnable = true;
                return;
            }
#if !UNITY_EDITOR

            Shader.EnableKeyword("_STRUCTURED_BUFFER_SUPPORT");
            Shader.DisableKeyword("_CULLING_CLUSTER_ON");
            _ClusterPrepass.enable = false;
            _DecalsRenderer.enable = true;
            _DecalsRenderer.clusterEnable = false;
            _DecalsRenderer.structBufferEnable = true;
#else

            if (!_SupportsComputeShaders)
            {
                Shader.DisableKeyword("_ADDITIONAL_DECALS");
                return;
                
            }
            Shader.EnableKeyword("_STRUCTURED_BUFFER_SUPPORT");
            Shader.EnableKeyword("_CULLING_CLUSTER_ON");
            _ClusterPrepass.enable = true;
            _DecalsRenderer.enable = true;
            _DecalsRenderer.clusterEnable = true;
            _DecalsRenderer.structBufferEnable = true;
#endif
        }

        class DecalsSortComparer : IComparer<DecalRenderer>
        {
            public int Compare(DecalRenderer x, DecalRenderer y)
            {
                return x.sort - y.sort;
            }
        }

    }
}
