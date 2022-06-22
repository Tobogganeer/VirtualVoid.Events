using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtualVoid.Events;
using System;

[VVEventSubscriber]
public class ItemInit
{
    public static Item Air = null;

    [VVEventHandler]
    internal static void RegisterItems(Registry<Item> register)
    {
        Air = register.Register(new Item("air"));

    }
}
