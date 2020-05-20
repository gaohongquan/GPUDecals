using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yunchang
{
    public class DecalDrawData
    {
        public Texture decalBaseMap { get; set; }
        public Texture decalNormalMap { get; set; }
        public List<DecalRenderer> visibleDecalRenderers = new List<DecalRenderer>();
        public int perClusterMaxDecalCount { get; set; }
    }
}
