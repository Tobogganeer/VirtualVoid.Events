#define DISABLE_CLASSGEN

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
    public static partial class VVEventBus
    {
        internal static partial void InitIfNecessary()
        {
            if (!inited)
            {
                inited = true;
";

        const string EventBusStringEnd = @"
            }
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
            List<string> checkedMethods = receiver.checkedMethods;

            /*
            string srcThing = @"
namespace VirtualVoid.Events
{
    public static class VVDataTransfer
    { 
        public const string AllTypes = """ +
string.Join(", ", checkedMethods) + " " + events[0].attribDec.ArgumentList?.Arguments[0].Expression.ToString() + @""";
    }
}
";
            context.AddSource("VVEventsDataTransfer.cs", SourceText.From(srcThing, Encoding.UTF8));
            */

            StringBuilder eventBusBuilder = new StringBuilder(EventBusStringStart);

            for (int i = 0; i < events.Count; i++)
            {
                string attribArg = events[i].attribDec.ArgumentList?.Arguments[0].Expression.ToString();
                eventBusBuilder.Append("\t\t\t\tVVEventBus.RegisterHandler(");
                eventBusBuilder.Append(attribArg);
                eventBusBuilder.Append(", ");
                eventBusBuilder.Append(events[i].classDec.Identifier.Text + "." + events[i].methodDec.Identifier.Text);
                eventBusBuilder.AppendLine(");");
            }

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
