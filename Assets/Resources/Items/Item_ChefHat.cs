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
        Action cleanup = null;

        cleanup = () =>
            {
                ItemOwner.onPrepStarted -= ChefHatLogic;
                ItemOwner.onPrepFinished -= cleanup;
            };

        ItemOwner.onPrepStarted += ChefHatLogic;
        ItemOwner.onPrepFinished += cleanup;

    }

    public override void OnStackModified(int count)
    {
        currentStackCount += count;

    }

    private void ChefHatLogic(string prepText)
    {

        //get the workers working on the station
        Station currentWorkstation = ItemOwner.currentWorkStation;
        List<Worker> workersInSameStation= currentWorkstation.GetAllWorkers();

        workersInSameStation
        .ForEach(w => 
        {
            if(w == ItemOwner) return; //skip self            
            w.RecieveBonusReduction(0.5f * currentStackCount);
        });

        currentWorkstation.stationWorkStarted += (BaseWorker w) => 
        {
            if(w == ItemOwner) return; //skip self            
            w.RecieveBonusReduction(0.5f * currentStackCount);
        };



    }


}
