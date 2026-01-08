using System;
using UnityEngine;

namespace KH.Framework2D.Pool
{
    /// <summary>
    /// Lightweight handle injected by ObjectPool/PoolManager.
    /// Allows pooled objects to return themselves without knowing pool keys or managers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledHandle : MonoBehaviour
    {
        private Action _returnAction;
        private bool _isReturning;

        /// <summary>Optional debug key set by PoolManager.</summary>
        public string PoolKey { get; private set; }

        internal void SetPoolKey(string key) => PoolKey = key;

        internal void SetReturnAction(Action returnAction)
        {
            _returnAction = returnAction;
        }

        /// <summary>
        /// Try to return this object to its pool.
        /// Returns false if no return action is bound (i.e., not spawned from a pool).
        /// </summary>
        public bool TryReturnToPool()
        {
            if (_returnAction == null) return false;
            if (_isReturning) return false;

            _isReturning = true;
            try
            {
                _returnAction.Invoke();
                return true;
            }
            finally
            {
                _isReturning = false;
            }
        }

        /// <summary>Clear binding (rarely needed; mostly for debugging).</summary>
        public void ClearBinding()
        {
            _returnAction = null;
            PoolKey = null;
        }
    }
}
