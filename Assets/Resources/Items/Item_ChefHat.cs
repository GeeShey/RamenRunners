using UnityEditor.Build.Content;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;

public class Item_ChefHat : Item
{
    
    public override void PreLoad(BaseWorker _ItemOwner)
    {
        base.PreLoad(_ItemOwner);
        ItemOwner.onPrepStarted += ChefHatLogic;
    }

    public override void OnStackModified(int count)
    {
        currentStackCount += count;

    }

    private void ChefHatLogic(string prepText)
    {

        //get the workers working on the station
        Station currentWorkstation = ItemOwner.currentWorkStation;
        List<BaseWorker> workersInSameStation= currentWorkstation.GetAllWorkers();

        foreach(var w in workersInSameStation)
        {
            if(w==null)
            {
                Debug.Log("null name");
                continue;
            }
                    
            Debug.Log("Worker at station: " + w.name);
        }
        workersInSameStation
        .ForEach(w => 
        {
            if(w==null || w == ItemOwner) return; //skip self            
            w.RecieveBonusReduction(0.5f * currentStackCount);
        });

        currentWorkstation.SomeoneStartedWorkAtStation += GiveWorkerBonus;
        
        ItemOwner.onPrepFinished += () =>
        {
            currentWorkstation.SomeoneStartedWorkAtStation -= GiveWorkerBonus;
        };



    }

    public void GiveWorkerBonus(BaseWorker w)
    {
        if (w == ItemOwner) return; //skip self            
        w.RecieveBonusReduction(0.5f * currentStackCount);
    }


}
