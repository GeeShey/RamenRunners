using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FoodMap : MonoBehaviour
{
    public List<DishSo> dishes = new List<DishSo>();
    public static FoodMap instance;

    private void Start()
    {
        instance = this;
        dishes = Resources.LoadAll<DishSo>("Dishes").ToList();
    }

    public DishSo getRandomDish(StationId[] stationsAvailable)
    {
        HashSet<StationId> availableSet = new HashSet<StationId>(stationsAvailable);

        // Only include dishes where all requiredStations are present in availableSet
        List<DishSo> dishList = dishes
            .Where(dish => dish.requiredStations.All(station => availableSet.Contains(station)))
            .ToList();

        if(dishList.Count == 1)
        {
            return dishList[0];
        }
        return dishList.ElementAt(Random.Range(0, dishList.Count));
    }


}
