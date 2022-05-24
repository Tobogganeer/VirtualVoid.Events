using System;
using System.Collections.Generic;

namespace VirtualVoid.Events
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class VVEventSubscriberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class VVEventHandlerAttribute : Attribute
    {
        public Type type;

        internal VVEventHandlerAttribute(Type eventType)
        {
            type = eventType;
        }
    }

    public class VVEvent
    {
        public static void Send(VirtualVoid.Events.VVEvent e)
        {
            VVEventBus.Send(e);
        }
    }

    public static partial class VVEventBus
    {
        internal static readonly Dictionary<Type, Action<VirtualVoid.Events.VVEvent>> subscribers = new Dictionary<Type, Action<VirtualVoid.Events.VVEvent>>();
        internal static bool inited = false;

        public static void Send(VirtualVoid.Events.VVEvent e)
        {
            InitIfNecessary();
            if (subscribers.TryGetValue(e.GetType(), out Action<VirtualVoid.Events.VVEvent> action))
                action?.Invoke(e);
        }

        public static void RegisterHandler(Type type, Action<VirtualVoid.Events.VVEvent> handler)
        {
            InitIfNecessary();
            if (!subscribers.ContainsKey(type)) subscribers[type] = handler;
            else subscribers[type] += handler;
        }

        public static void DeregisterHandler(Type type, Action<VirtualVoid.Events.VVEvent> handler)
        {
            InitIfNecessary();
            if (subscribers.ContainsKey(type))
                subscribers[type] -= handler;
        }

        public static int GetNumSubscribers()
        {
            InitIfNecessary();
            return subscribers.Count;
        }

        internal static partial void InitIfNecessary();
    }
}