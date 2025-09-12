using UnityEngine;
using System;

public class Item_Knife : Item
{
    public override void OnStackModified(int count)
    {
        currentStackCount += count;
        float efficiencyBonus = (WorkerBaseStats.stationEfficiency * 0.5f * currentStackCount);
        ItemOwner.CurrentStationEfficiency = WorkerBaseStats.stationEfficiency + efficiencyBonus;

        Debug.Log("current efficiency ="+ ItemOwner.CurrentStationEfficiency);

    }

}
