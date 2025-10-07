using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime.Blackboard
{
    // Типобезопасные ключи Blackboard
    public readonly struct BlackboardKey<T>
    {
        public readonly string Path;
        public BlackboardKey(string path) { Path = path; }
        public override string ToString() => Path;
    }

    // Потокобезопасная доска (на практике большинство обращений с главного потока)
    public class Blackboard
    {
        private readonly Dictionary<string, object> _data = new();
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

        public bool TryGet<T>(BlackboardKey<T> key, out T value)
        {
            _lock.EnterReadLock();
            try
            {
                if (_data.TryGetValue(key.Path, out var obj) && obj is T t)
                {
                    value = t;
                    return true;
                }
            }
            finally { _lock.ExitReadLock(); }
            value = default;
            return false;
        }

        public T GetOrDefault<T>(BlackboardKey<T> key, T defaultValue = default)
        {
            return TryGet(key, out T v) ? v : defaultValue;
        }

        public void Set<T>(BlackboardKey<T> key, T value)
        {
            _lock.EnterWriteLock();
            try { _data[key.Path] = value; }
            finally { _lock.ExitWriteLock(); }
        }

        public bool Contains(string path)
        {
            _lock.EnterReadLock();
            try { return _data.ContainsKey(path); }
            finally { _lock.ExitReadLock(); }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try { _data.Clear(); }
            finally { _lock.ExitWriteLock(); }
        }
    }
}
