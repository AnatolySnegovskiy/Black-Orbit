namespace Black_Orbit.Scripts.Core.Pooling
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Универсальный пул объектов для Unity.
    /// Позволяет переиспользовать объекты вместо постоянного Instantiate/Destroy.
    /// </summary>
    /// <typeparam name="T">Тип компонента, который должен быть наследником Component</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly Transform _parent;

        /// <summary>
        /// Создает пул с указанным префабом и начальным размером.
        /// </summary>
        /// <param name="prefab">Префаб для создания новых объектов</param>
        /// <param name="initialSize">Количество объектов, создаваемых сразу</param>
        /// <param name="parent">Родитель для объектов пула (опционально)</param>
        public ObjectPool(T prefab, int initialSize, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                var instance = CreateInstance();
                _pool.Push(instance);
            }
        }

        private T CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        /// <summary>
        /// Получить объект из пула (активируется автоматически).
        /// Если пул пуст, создается новый объект.
        /// </summary>
        public T Get()
        {
            if (_pool.Count == 0)
            {
                _pool.Push(CreateInstance());
            }

            var obj = _pool.Pop();
            obj.gameObject.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Вернуть объект обратно в пул (деактивируется).
        /// </summary>
        public void ReturnToPool(T obj)
        {
            obj.gameObject.SetActive(false);
            _pool.Push(obj);
        }

        public void ExpandPoolIfNeeded(int desiredCount)
        {
            while (_pool.Count < desiredCount)
            {
                _pool.Push(CreateInstance());
            }
        }
    }
}
