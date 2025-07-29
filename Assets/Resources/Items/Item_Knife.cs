using UnityEngine;
using System;

public class Item_Knife : Item
{
    Worker worker;
    int currentStackCount = 0;
    public override void PreLoad()
    {
        worker = GetComponent<Worker>();
    }

    public override void OnStackModified(int count)
    {
        currentStackCount += count;
        float movementSpeedBonus = (WorkerBaseStats.movementSpeed * 0.1f * currentStackCount);
        worker.movementSpeed = WorkerBaseStats.movementSpeed + movementSpeedBonus;

    }

}
