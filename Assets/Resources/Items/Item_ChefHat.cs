using UnityEngine;
using System.Collections.Generic;

public class Item_ChefHat : Item
{
    [SerializeField] private float bonusPerStack = 0.5f;

    private Station currentWorkstation;
    private bool subscribed;

    public override void PreLoad(BaseWorker _ItemOwner)
    {
        base.PreLoad(_ItemOwner);
        Subscribe();
    }

    public override bool OnStackModified(int count)
    {
        return base.OnStackModified(count);
    }

    private void Subscribe()
    {
        if (ItemOwner != null && !subscribed)
        {
            ItemOwner.OnPrepStarted += OnPrepStarted;
            ItemOwner.OnPrepFinished += OnPrepFinished;
            subscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (ItemOwner != null && subscribed)
        {
            ItemOwner.OnPrepStarted -= OnPrepStarted;
            ItemOwner.OnPrepFinished -= OnPrepFinished;
            if (currentWorkstation != null)
            {
                currentWorkstation.SomeoneStartedWorkAtStation -= GiveWorkerBonus;
            }
            subscribed = false;
        }
    }

    private void OnPrepStarted(string _)
    {
        currentWorkstation = ItemOwner.WorkStation;
        if (currentWorkstation == null) return;

        List<BaseWorker> workersInSameStation = currentWorkstation.GetAllWorkers();
        float totalBonus = bonusPerStack * CurrentStackCount;

        foreach (var w in workersInSameStation)
        {
            if (w == null || w == ItemOwner) continue;
            w.ReceiveBonusReduction(totalBonus);
        }

        currentWorkstation.SomeoneStartedWorkAtStation += GiveWorkerBonus;
    }

    private void OnPrepFinished()
    {
        if (currentWorkstation != null)
        {
            currentWorkstation.SomeoneStartedWorkAtStation -= GiveWorkerBonus;
        }
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void GiveWorkerBonus(BaseWorker w)
    {
        if (w == null || w == ItemOwner) return;
        float totalBonus = bonusPerStack * CurrentStackCount;
        w.ReceiveBonusReduction(totalBonus);
    }
}
