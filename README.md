# VirtualVoid.Events

Compile-time event system for Unity using C# [Source Generators](https://docs.unity3d.com/Manual/roslyn-analyzers.html)

Methods marked will certain attributes will automatically receive events, without using reflection or IL weaving.

### Heads Up!
This is **bare minimum** working code!

It will likely fail under slight deviances, you are encouraged to edit the source (it's quite simplistic currently)

Add try/catch blocks, increase robustness, I plan to update this repo but it's not perfect!

---

Change the namespace to whatever you'd like, but the default is
```cs
// Get access to event stuff
using VirtualVoid.Events;
```

Create you own event types
```cs
// Define a custom event
class MyCustomEvent : VVEvent
{
  // ... With any data you'd like
  public string myString;
  public int myInt;
}
```

And receive those events
```cs
// Handle events with any class
[VVEventSubscriber]
class SomeEventReceiver // ... Doesn't need to be a monobehaviour!
{
  // Use the method argument...
  [VVEventHandler]
  public static void HandleMyEvent(MyCustomEvent e)
  {
    // Code here
  }

  // Or specify the type manually...
  [VVEventHandler(typeof(MyCustomEvent))]
  public static void HandleMyEvent_Manual(VVEvent e)
  {
    MyCustomEvent myEvent = e as MyCustomEvent;
    
    // Code here
  }
  
  
}
```

Send events from anywhere
```cs
// Somewhere else in your project
void SendTheEventNow()
{
  VVEventBus.Send(new MyCustomEvent());
  // or
  VVEvent.Send(new MyCustomEvent());
}
```
---

You can choose for the backing classes to either be auto-generated, or define them yourself.

This is controlled with a define in the generator proj.

Class-gen is disabled by default, as I've found Visual Studio has some problems recognizing them sometimes, so to avoid annoying red lines, define them yourself

The generator will implement the registration of the events.

---

See the example inventory system to see a use case, it should be relatively straight forward.
