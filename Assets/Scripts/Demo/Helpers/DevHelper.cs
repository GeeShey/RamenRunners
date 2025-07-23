using System.Collections.Generic;
using System.Linq;
using UnityEngine;



public class DevHelper : MonoBehaviour
{
    //this is for storage purposes only DO NOT SET FROM HERE
    public List<HierarchyColor> colors;


    [Header("WORKER STUFF")]
    public Station restStation;
    public void initializeAllWorkers()
    {
        GameObject[] workers = GameObject.FindGameObjectsWithTag("Worker");

        if (restStation.GetAvailableStandingLocationCount() < workers.Count()) 
        {
            Debug.Log("too many workers and not enough slots");        
        }
        else
        {

            for (int i = 0; i < workers.Count(); i++)
            {
                Transform slot = restStation.GetAvailableStandingLocation();
                workers[i].transform.position = slot.position;
                workers[i].GetComponent<Worker>().InitializeWorker();
            }
        }
    }
}
