using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Yunchang
{
    public class ComputeBufferCache
    {
        private Dictionary<string, ComputeBuffer> _computeBufferDict = new Dictionary<string, ComputeBuffer>();

        public ComputeBuffer Alloc<T>(string key, int count) where T : struct
        {
            ComputeBuffer cb;
            if (!_computeBufferDict.TryGetValue(key, out cb))
            {
                cb = new ComputeBuffer(count, Marshal.SizeOf(typeof(T)));
                _computeBufferDict.Add(key, cb);
            }
            if (count > cb.count)
            {
                cb.Dispose();
                cb = new ComputeBuffer(count, Marshal.SizeOf(typeof(T)));
                _computeBufferDict[key] = cb;
            }
            return cb;
        }

        public ComputeBuffer Get(string key)
        {
            ComputeBuffer cb;
            if (_computeBufferDict.TryGetValue(key, out cb))
                return cb;
            return null;
        }

        public void Release()
        {
            foreach (var keyValue in _computeBufferDict)
            {
                keyValue.Value.Dispose();
            }
            _computeBufferDict.Clear();
        }
    }
}
