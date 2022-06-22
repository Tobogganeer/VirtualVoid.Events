using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class Registry<T> : VirtualVoid.Events.VVEvent where T : IRegistryEntry
{
    public delegate T DefaultSupplier();

    private Dictionary<string, T> values;
    private DefaultSupplier defaultSupplier;

    public Registry(string RegistryName, DefaultSupplier defaultSupplier)
    {
        this.RegistryName = RegistryName;
        this.defaultSupplier = defaultSupplier;
        values = new Dictionary<string, T>();
    }

    public readonly string RegistryName;

    public T Register(T value)
    {
        values[value.RegistryKey] = value;
        return value;
    }

    public T Get(string key)
    {
        return values[key];
    }

    public bool Contains(string key)
    {
        return values.ContainsKey(key);
    }

    public T Default()
    {
        return defaultSupplier();
    }

    private void Init()
    {
        VirtualVoid.Events.VVEventBus.Send(this);
    }


    public static Registry<Item> Item = new Registry<Item>("item", () => ItemInit.Air);

    public static void InitializeRegistries()
    {
        Item.Init();
    }
}
