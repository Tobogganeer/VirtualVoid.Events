using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtualVoid.Events;

public class InventoryTest : MonoBehaviour
{
    void Start()
    {
        RegisterItemsEvent @event = new RegisterItemsEvent();
        VVEventBus.Send(@event);
        foreach (Item item in @event.items)
        {
            Debug.Log("Item: " + item.name);
        }
    }
}

internal class RegisterItemsEvent : VVEvent
{
    public List<Item> items = new List<Item>();
}

public class Item
{
    public string name; //idk item stuff here
}

[VVEventSubscriber]
public class ItemInit
{
    [VVEventHandler(typeof(RegisterItemsEvent))]
    internal static void OnRegisterItems(VVEvent e)
    {
        RegisterItemsEvent @event = e as RegisterItemsEvent;
        @event.items.Add(new Item { name = "Sword" });
        @event.items.Add(new Item { name = "Chestplate" });
    }
}

[VVEventSubscriber]
public class SomeOtherItemInitIDK
{
    [VVEventHandler(typeof(RegisterItemsEvent))]
    internal static void AnyNameHereDontMatter(VVEvent e)
    {
        RegisterItemsEvent @event = e as RegisterItemsEvent;
        @event.items.Add(new Item { name = "Bag of Gold" });
        @event.items.Add(new Item { name = "Pickaxe" });
    }

    [VVEventHandler]
    internal static void PerTypeTest(RegisterItemsEvent e)
    {
        Debug.Log("Im also getting this! Per Type!");
        e.items.Add(new Item { name = "Extra Cool B)" });
    }
}
