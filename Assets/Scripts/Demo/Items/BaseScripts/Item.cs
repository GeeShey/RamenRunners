using System;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public ItemBasicDetailsSO ItemBasicDetails;
    public BaseWorker ItemOwner; // Changed from Worker to BaseWorker
    public int currentStackCount = 0;
    public int StackCount;
    public bool DEBUG;

    public virtual void PreLoad(BaseWorker _ItemOwner) // Changed parameter type
    {
        ItemOwner = _ItemOwner;
    }

    //count can be negative
    public virtual void OnStackModified(int count)
    {
        currentStackCount += count;
    }
}