using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yunchang
{
    public class ClusterData
    {
        public ClusterDimensions clusterDimensions = new ClusterDimensions
        {
            x = 32,
            y = 16,
            z = 16
        };

        public ComputeShader CullingCS { get; set; }

        public float ClusterZRange = 1.0f;

        public float ClusterRate = 1.5f;
    }

    public struct AABB
    {
        public Vector4 min, max;
    }

    public struct ClusterDimensions
    {
        public int x, y, z;

        public int count => (x * y * z);
    }
}
