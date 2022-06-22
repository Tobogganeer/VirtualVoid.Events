using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : IRegistryEntry
{
    public string RegistryKey => name;
    public string name;
    //group
    //rarity
    //stack
    //id

    public Item(string name)
    {
        this.name = name;
    }
}
