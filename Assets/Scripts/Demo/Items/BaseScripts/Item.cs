using System;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public ItemBasicDetailsSO ItemBasicDetails;

    public int StackCount;
    public abstract void PreLoad();

    //count can be negative
    public abstract void OnStackModified(int count);

}
