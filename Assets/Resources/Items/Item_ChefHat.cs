using UnityEditor.Build.Content;
using UnityEngine;

public class Item_ChefHat : Item
{

    public override void OnStackModified(int count)
    {
        currentStackCount += count;


    }


}
