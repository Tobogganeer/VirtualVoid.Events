#define DISABLE_CLASSGEN
#define PER_TYPE_EVENTS

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VirtualVoid.Events
{
    [Generator]
    public class EventGenerator : ISourceGenerator
    {
        const string GeneratedClassesString = @"
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
";

        const string EventBusStringStart = @"
using System;
using System.Collections.Generic;

namespace VirtualVoid.Events
{
    internal partial class VVEvent { }
    //class VVEvent { }

    internal static partial class VVEventBus
    {
        static bool inited = false;

        //static partial void InitIfNecessary()
        private static void InitIfNecessary()
        {
            if (!inited)
            {
                inited = true;
";

        const string EventBusStringMid1 = @"
            }
        }

";

        const string EventBusStringMid2 = @"
        //static partial void SendPerType(Type t, VirtualVoid.Events.VVEvent e)
        private static void SendPerType(Type type, VirtualVoid.Events.VVEvent e)
        {
            if (perTypeCallbackDict.TryGetValue(type, out Action<VVEvent> callback))
                callback(e);
            else
                Console.WriteLine(""No callback for event "" + type.Name);
            //switch (type)
            //{
";

        const string EventBusStringEnd = @"
            //    default:
            //        break;
            //}
        }
    }
}
";


        public void Execute(GeneratorExecutionContext context)
        {
#if !DISABLE_CLASSGEN
            context.AddSource("VVEventsClasses.cs", SourceText.From(GeneratedClassesString, Encoding.UTF8));
#endif

            EventSubscriberReceiver receiver = (EventSubscriberReceiver)context.SyntaxReceiver;
            List<EventHandlerMethodDescription> events = receiver.methods;
            List<EventHandlerMethodDescription> defaultEvents = new List<EventHandlerMethodDescription>();
            List<EventHandlerMethodDescription> perTypeEvents = new List<EventHandlerMethodDescription>();
            List<string> checkedMethods = receiver.checkedMethods;

            StringBuilder eventBusBuilder = new StringBuilder(EventBusStringStart);

            // Sort event types
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].attribDec.ArgumentList != null
                    && events[i].attribDec.ArgumentList.Arguments.Count > 0
                    && events[i].attribDec.ArgumentList.Arguments[0].Expression.ToString().Length > 1)
                    defaultEvents.Add(events[i]);
                else
                    perTypeEvents.Add(events[i]);
            }

            // Register default event types

            for (int i = 0; i < defaultEvents.Count; i++)
            {
                string attribArg = defaultEvents[i].attribDec.ArgumentList?.Arguments[0].Expression.ToString();
                string methodFullName = defaultEvents[i].classDec.Identifier.Text + "." + defaultEvents[i].methodDec.Identifier.Text;
                eventBusBuilder.AppendLine($"\t\t\t\tVVEventBus.RegisterHandler({attribArg}, {methodFullName});");
            }

            eventBusBuilder.AppendLine();

            // Add per-type types to hashset

            HashSet<string> types = new HashSet<string>();

            for (int i = 0; i < perTypeEvents.Count; i++)
            {
                string type = perTypeEvents[i].methodDec.ParameterList?.Parameters[0].Type.ToString();
                if (!types.Contains(type))
                    types.Add(type);
                //eventBusBuilder.AppendLine($"\t\t\t\tperTypeEvents");
                //eventBusBuilder.AppendLine($"UnityEngine.Debug.Log($\"Per-Type: {typeThing}\");");
            }

            //foreach (string _type in types)
            //{
            //    eventBusBuilder.AppendLine($"\t\t\t\tperTypeEvents.Add(\"typeof({_type})\");");
            //}

            //eventBusBuilder.AppendLine();

            // Subscribe per-type events

            for (int i = 0; i < perTypeEvents.Count; i++)
            {
                string type = perTypeEvents[i].methodDec.ParameterList?.Parameters[0].Type.ToString();
                string methodFullName = perTypeEvents[i].classDec.Identifier.Text + "." + perTypeEvents[i].methodDec.Identifier.Text;
                // For generics
                type = type.Replace('<', '_').Replace(">", "");
                eventBusBuilder.AppendLine($"\t\t\t\tevent_{type} += {methodFullName};");
            }

            // Create dictionary for callbacks

            eventBusBuilder.AppendLine();

            eventBusBuilder.AppendLine(EventBusStringMid1);

            eventBusBuilder.AppendLine($"\t\t\tprivate static Dictionary<Type, Action<VVEvent>> perTypeCallbackDict = new Dictionary<Type, Action<VVEvent>>()");
            eventBusBuilder.AppendLine($"\t\t\t{{");

            for (int i = 0; i < perTypeEvents.Count; i++)
            {
                string type = perTypeEvents[i].methodDec.ParameterList?.Parameters[0].Type.ToString();
                // For generics
                string fixedType = type.Replace('<', '_').Replace(">", "");
                eventBusBuilder.AppendLine($"\t\t\t\t{{ typeof({type}), (evnt) => event_{fixedType}(evnt as {type}) }},");
            }

            eventBusBuilder.AppendLine($"\t\t\t}};");

            // Generate per-type type actions

            foreach (string _type in types)
            {
                string fixedType = _type.Replace('<', '_').Replace(">", "");
                eventBusBuilder.AppendLine($"\t\tprivate static event Action<{_type}> event_{fixedType};");
            }

            eventBusBuilder.AppendLine(EventBusStringMid2);

            // Send per-type type actions out

            /*
            foreach (string _type in types)
            {
                string fixedType = _type.Replace('<', '_').Replace(">", "");
                eventBusBuilder.AppendLine($"\t\t\t\tcase typeof({_type}):");
                eventBusBuilder.AppendLine($"\t\t\t\t\tevent_{fixedType}?.Invoke(e as {_type});");
                eventBusBuilder.AppendLine($"\t\t\t\t\tbreak;");
            }
            */

            //for (int i = 0; i < length; i++)
            //{
            //
            //}

            eventBusBuilder.AppendLine(EventBusStringEnd);

            context.AddSource("VVEventsEventBus.cs", SourceText.From(eventBusBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new EventSubscriberReceiver());
        }
    }

    internal class EventSubscriberReceiver : ISyntaxReceiver
    {
        internal List<EventHandlerMethodDescription> methods = new List<EventHandlerMethodDescription>();
        internal List<string> checkedMethods = new List<string>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDec && classDec.AttributeLists.Any())
            {
                foreach (AttributeListSyntax listC in classDec.AttributeLists)
                {
                    foreach (AttributeSyntax attribC in listC.Attributes)
                    {
                        string attribNameC = attribC.Name.ToString();
                        if (attribNameC == "VVEventSubscriber")
                        {
                            foreach (MemberDeclarationSyntax member in classDec.Members)
                            {
                                if (member is MethodDeclarationSyntax method && method.AttributeLists.Any())
                                {
                                    foreach (AttributeListSyntax list in method.AttributeLists)
                                    {
                                        foreach (AttributeSyntax attrib in list.Attributes)
                                        {
                                            string attribName = attrib.Name.ToString();
                                            if (attribName == "VVEventHandler")
                                            {
                                                checkedMethods.Add(method.Identifier.Text);
                                                methods.Add(new EventHandlerMethodDescription(classDec, method, attrib));
                                                goto NewMember;
                                            }
                                        }
                                    }
                                }
                            NewMember:;
                            }
                        }
                    }
                }
            }
        }
    }

    internal class EventHandlerMethodDescription
    {
        public ClassDeclarationSyntax classDec;
        public MethodDeclarationSyntax methodDec;
        public AttributeSyntax attribDec;

        public EventHandlerMethodDescription(ClassDeclarationSyntax classDec, MethodDeclarationSyntax methodDec, AttributeSyntax attribDec)
        {
            this.classDec = classDec;
            this.methodDec = methodDec;
            this.attribDec = attribDec;
        }
    }
}
