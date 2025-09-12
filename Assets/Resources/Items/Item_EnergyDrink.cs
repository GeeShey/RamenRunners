using UnityEngine;
using System;

public class Item_EnergyDrink : Item
{
    Worker worker;
    [SerializeField]
    public int currentStackCount = 0;
    public override void PreLoad()
    {
        worker = GetComponent<Worker>();
    }

    public override void OnStackModified(int count)
    {
        currentStackCount += count;
        float movementSpeedBonus = (WorkerBaseStats.movementSpeed * 0.25f * currentStackCount);
        worker.CurrentMovementSpeed = WorkerBaseStats.movementSpeed + movementSpeedBonus;

    }

}
