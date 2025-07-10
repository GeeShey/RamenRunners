using UnityEngine;

[CreateAssetMenu(fileName = "DishSO", menuName = "Scriptable Objects/DishSO")]
public class DishSO : ScriptableObject
{
    public StationId[] requiredStations;
}
