using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DishSo", menuName = "Scriptable Objects/DishSo")]
public class DishSo : ScriptableObject
{
    public string Name;
    public string Description;
    public List<StationId> requiredStations;
    public List<string> stationPrepText;
    public int itemPrice;


    public StationId[] getRequiredStations()
    {
        return requiredStations.ToArray();

    }
    public string[] getRequiredStationsPrep()
    {
        return stationPrepText.ToArray();
    }
}
