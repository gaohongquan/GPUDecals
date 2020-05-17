using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yunchang
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class DecalsManager : MonoBehaviour
    {
        public Texture decalBaseTexture;
        public Texture decalNormalTexture;

        public List<DecalRenderer> activeDecalRenderers => DecalRenderer.decalRenderers;

        void OnEnable()
        {
            _instance = this;
        }

        void OnDisable()
        {
            _instance = null;
        }

        private static DecalsManager _instance;
        public static DecalsManager Instance => _instance;

    }
}
