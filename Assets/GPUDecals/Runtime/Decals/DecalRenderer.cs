using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yunchang
{
    [ExecuteInEditMode]
    public class DecalRenderer : MonoBehaviour
    {
        public Sprite sprite;
        [Range(0, 1)]
        public float alpha = 1;
        [Range(0, 20)]
        public float normalIntensity = 1;
        public int sort = 0;
        private Vector4 _uv;

        private void Awake()
        {
            if (sprite == null)
                _uv = Vector4.zero;
            else
            {
                Vector2[] uv = sprite.uv;
                _uv = new Vector4(uv[0].x, uv[0].y, uv[3].x, uv[3].y);
            }
        }

        private void OnValidate()
        {
            Awake();
        }

        public Vector4 uv => _uv;

        public Vector4 sphere
        {
            get
            {
                Bounds bound = GetComponent<BoxCollider>().bounds;
                Vector3 position = transform.position;
                float radius = Vector3.Distance(bound.min, bound.max) * 0.5f;
                return new Vector4(position.x, position.y, position.z, radius);
            }
        }

        private static List<DecalRenderer> _decalRenderers = new List<DecalRenderer>();
        public static List<DecalRenderer> decalRenderers => _decalRenderers;

        private void OnEnable()
        {
            _decalRenderers.Add(this);
        }

        private void OnDisable()
        {
            _decalRenderers.Remove(this);
        }
    }
}
