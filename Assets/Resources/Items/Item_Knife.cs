using UnityEngine;
using System;

public class Item_Knife : Item
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
        float efficiencyBonus = (WorkerBaseStats.stationEfficiency * 0.5f * currentStackCount);
        worker.CurrentStationEfficiency = WorkerBaseStats.stationEfficiency + efficiencyBonus;

        Debug.Log("current efficiency ="+ worker.CurrentStationEfficiency);

    }

}
