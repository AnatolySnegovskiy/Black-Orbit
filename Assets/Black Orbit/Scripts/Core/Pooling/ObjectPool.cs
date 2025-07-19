using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Black_Orbit.Scripts.Core.Pooling
{
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly Transform _parent;
        private readonly int _expireMilliseconds;

        private readonly Dictionary<T, CancellationTokenSource> _activeTokens = new Dictionary<T, CancellationTokenSource>();

        public ObjectPool(T prefab, int initialSize, Transform parent = null, float expireSeconds = 60f)
        {
            _prefab = prefab;
            _parent = parent;
            _expireMilliseconds = (int)(expireSeconds * 1000f);

            for (int i = 0; i < initialSize; i++)
            {
                _pool.Push(CreateInstance());
            }
        }

        private T CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        public T Get()
        {
            if (_pool.Count == 0)
            {
                _pool.Push(CreateInstance());
            }

            var obj = _pool.Pop();
            obj.gameObject.SetActive(true);

            StartExpireTimer(obj);

            return obj;
        }

        public void ReturnToPool(T obj)
        {
            if (_activeTokens.TryGetValue(obj, out var token))
            {
                token.Cancel();
                _activeTokens.Remove(obj);
            }

            obj.gameObject.SetActive(false);
            _pool.Push(obj);
        }

        private void StartExpireTimer(T obj)
        {
            if (_activeTokens.TryGetValue(obj, out var existingToken))
            {
                existingToken.Cancel();
            }

            var cts = new CancellationTokenSource();
            _activeTokens[obj] = cts;

            AutoReturnAsync(obj, cts.Token);
        }

        private async void AutoReturnAsync(T obj, CancellationToken token)
        {
            try
            {
                await Task.Delay(_expireMilliseconds, token);
                if (!token.IsCancellationRequested)
                {
                    ReturnToPool(obj);
                }
            }
            catch (TaskCanceledException)
            {
                // Нормальная отмена — ничего делать не нужно
            }
        }

        public void ClearPool()
        {
            foreach (var pair in _activeTokens)
            {
                pair.Value.Cancel();
                if (pair.Key != null)
                    Object.Destroy(pair.Key.gameObject);
            }
            _activeTokens.Clear();

            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }
        }
    }
}
