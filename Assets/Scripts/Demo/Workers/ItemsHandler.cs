using System.Collections.Generic;
using UnityEngine;
using System;

public class ItemsHandler : MonoBehaviour
{
    public Dictionary<string, Component> itemDictionary = new Dictionary<string, Component>();
    public void EquipItem(string id)
    {
        Item itemInstance = null;
        
        if (itemDictionary.ContainsKey(id))
        {
            itemInstance = itemDictionary[id] as Item;
        }
        else 
        {
            Component itemComponent = ItemFactory.instance.AddItemTo(gameObject, id);
            itemInstance = itemComponent as Item;
            itemInstance.PreLoad();

            if (itemComponent != null)
            {
                itemDictionary[id] = itemComponent;
            }
            else
            {
                Debug.Log("unable to equip item");
            }
        }
        itemInstance.OnStackModified(1);

    }
}
