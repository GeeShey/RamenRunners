using System;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public ItemBasicDetailsSO ItemBasicDetails;
    public abstract void PreLoad();
    public abstract void OnStackModified(int count);

}
