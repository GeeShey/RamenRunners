using System;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public ItemBasicDetailsSO ItemBasicDetails;
    public Worker ItemOwner;
    public int currentStackCount = 0;

    public int StackCount;

    public bool DEBUG;
    public virtual void PreLoad(Worker _ItemOwner)
    {
        ItemOwner = _ItemOwner;
    }

    //count can be negative
    public virtual void OnStackModified(int count)
    {
        currentStackCount += count;
    }

}
