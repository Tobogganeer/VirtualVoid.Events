using System;
using System.Collections.Generic;

namespace VirtualVoid.Events
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal class VVEventSubscriberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    internal class VVEventHandlerAttribute : Attribute
    {
        internal Type type;

        internal VVEventHandlerAttribute() { }

        internal VVEventHandlerAttribute(Type eventType)
        {
            type = eventType;
        }
    }

    internal partial class VVEvent
    {
        internal static void Send(VirtualVoid.Events.VVEvent e)
        {
            VVEventBus.Send(e);
        }
    }

    internal static partial class VVEventBus
    {
        private static readonly Dictionary<Type, Action<VirtualVoid.Events.VVEvent>> subscribers = new Dictionary<Type, Action<VirtualVoid.Events.VVEvent>>();
<<<<<<< Updated upstream
        private static readonly HashSet<string> perTypeEvents = new HashSet<string>();
=======
        //private static readonly HashSet<string> perTypeEvents = new HashSet<string>();
>>>>>>> Stashed changes

        internal static void Send(VirtualVoid.Events.VVEvent e)
        {
            InitIfNecessary();
            Type t = e.GetType();
            if (subscribers.TryGetValue(t, out Action<VirtualVoid.Events.VVEvent> action))
                action?.Invoke(e);
            //if (perTypeEvents.Contains(t.ToString()))
            SendPerType(t, e);
        }

        internal static void RegisterHandler(Type type, Action<VirtualVoid.Events.VVEvent> handler)
        {
            InitIfNecessary();
            if (!subscribers.ContainsKey(type)) subscribers[type] = handler;
            else subscribers[type] += handler;
        }

        internal static void DeregisterHandler(Type type, Action<VirtualVoid.Events.VVEvent> handler)
        {
            InitIfNecessary();
            if (subscribers.ContainsKey(type))
                subscribers[type] -= handler;
        }

        internal static int GetNumSubscribers()
        {
            InitIfNecessary();
            return subscribers.Count;
        }

        //static partial void InitIfNecessary();

        //static partial void SendPerType(Type t);

        //internal static void InitIfNecessary() { }

        //internal static void SendPerType(Type t) { }
    }
}
