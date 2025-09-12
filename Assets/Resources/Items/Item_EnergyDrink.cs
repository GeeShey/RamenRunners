using UnityEngine;
using System;

public class Item_EnergyDrink : Item
{
    public override void OnStackModified(int count)
    {
        currentStackCount += count;
        float movementSpeedBonus = (WorkerBaseStats.movementSpeed * 0.25f * currentStackCount);
        ItemOwner.CurrentMovementSpeed = WorkerBaseStats.movementSpeed + movementSpeedBonus;

    }

}
