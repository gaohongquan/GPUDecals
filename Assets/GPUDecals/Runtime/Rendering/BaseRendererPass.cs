using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace Yunchang
{
    public abstract class BaseRendererPass
    {
        public bool enable { get; set; }
        public RendererFeatures rendererFeatures { get; set; }

        public virtual void Initialize(){ }

        public virtual void Release(){}

        public virtual void Clear(){}

        public abstract void Execute();

    }
}