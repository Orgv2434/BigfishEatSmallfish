using System;
using System.Collections.Generic;
using UnityEngine;

namespace DistantLands.Core
{
    /// <summary>
    /// 统一事件总线 - 所有游戏事件的中央发布/订阅机制
    /// 用于完全解耦各个游戏系统，替代直接的系统间调用
    /// </summary>
    public class EventBus : MonoBehaviour
    {
        private static EventBus _instance;
        public static EventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject eventBusObj = new GameObject("[EventBus]");
                    _instance = eventBusObj.AddComponent<EventBus>();
                    DontDestroyOnLoad(eventBusObj);
                }
                return _instance;
            }
        }

        private Dictionary<Type, Delegate> eventListeners = new Dictionary<Type, Delegate>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 订阅事件（泛型版本）
        /// </summary>
        public void Subscribe<T>(Action<T> callback) where T : IGameEvent
        {
            if (callback == null)
                return;

            Type eventType = typeof(T);

            if (!eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType] = callback;
            }
            else
            {
                eventListeners[eventType] = (Action<T>)eventListeners[eventType] + callback;
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe<T>(Action<T> callback) where T : IGameEvent
        {
            if (callback == null)
                return;

            Type eventType = typeof(T);

            if (eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType] = (Action<T>)eventListeners[eventType] - callback;

                if (eventListeners[eventType] == null)
                {
                    eventListeners.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        public void Publish<T>(T gameEvent) where T : IGameEvent
        {
            Type eventType = typeof(T);

            if (eventListeners.ContainsKey(eventType))
            {
                try
                {
                    ((Action<T>)eventListeners[eventType])?.Invoke(gameEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] 事件处理异常 {eventType.Name}: {ex.Message}");
                }
            }
        }

        public void ClearAllListeners()
        {
            eventListeners.Clear();
        }
    }

    /// <summary>
    /// 所有游戏事件必须实现此接口
    /// </summary>
    public interface IGameEvent
    {
    }
}
