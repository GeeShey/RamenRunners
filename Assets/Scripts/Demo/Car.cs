using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Splines;



public class Car : MonoBehaviour
{

    public Order order;


    public void StartOrder()
    {
        if (CarManager.instance.OrderSlotAvailable())
        {

            Debug.Log("Order Slot is available");
            StartCoroutine(OrderLoop());

        }
        else
        {
            CarManager.instance.AddCarToWaitingList(this);
         
        }
    }

    private IEnumerator MoveTo(Vector3 destination, float timeToDestination = 2.0f)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < timeToDestination)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / timeToDestination;

            // Linear interpolation from start to destination
            transform.position = Vector3.Lerp(startPosition, destination, t);

            yield return null; // Wait for next frame
        }

        // Ensure we end up exactly at the destination
        transform.position = destination;

    }


    public IEnumerator OrderLoop()
    {
        CarSlot orderSlot = CarManager.instance.GetAvailableOrderSlot();

        // MOVE TO THE ORDER SLOT
        yield return StartCoroutine(MoveTo(orderSlot.position));

        // When object has reached the slot 
        OrderManager.instance.PlaceOrder(order);

        // Wait for order to be requested
        bool orderRequested = false;
        Action onOrderRequested = () =>
        { 
            orderRequested = true;
            CarManager.instance.ResetSlot(orderSlot);
        };
        order.orderRequested += onOrderRequested;

        // Wait until order is requested
        yield return new WaitUntil(() => orderRequested);
        Debug.Log("Order is taken");

        order.orderRequested -= onOrderRequested;

        // Now move to pickup slot

        CarSlot pickupSlot = CarManager.instance.GetAvailablePickupSlot();
        Debug.Log("moving to pickup slot");

        yield return StartCoroutine(MoveTo(pickupSlot.position));

        //CREATING TWO RACE CONDITIONS TO SEE WHICH ONE WILL FINISH FIRST. EITHER THE ORDER LIMIT RUNS OUT OR THE 
        //ORDER GETS HANDED

        bool orderHanded = false;
        Action orderHandedAction = null;
        orderHandedAction += () => 
        {
            orderHanded = true;
            order.orderHanded -= orderHandedAction;
            CarManager.instance.ResetSlot(pickupSlot);
            Debug.Log("Order Handed!");
        };
        order.orderHanded += orderHandedAction;

        Coroutine waitCoroutine = StartCoroutine(WaitForOrder());
        
        yield return new WaitUntil(() => {
            if (orderHanded) return true;
            if (waitCoroutine == null) return true; // Coroutine finished
            return false;
        });

        StartCoroutine(MoveToExitSlot());

    }

    private IEnumerator WaitForOrder()
    {
        float timeElapsed = Time.time - order.orderStartTime;

        while(timeElapsed < order.customerWaitLimit)
        {
            yield return null;
        }


        Debug.Log("Order took too long");
        MoveToExitSlot();
    }

    public void MoveToCheckoutSlot()
    {

    }

    private IEnumerator MoveToExitSlot()
    {
        yield return StartCoroutine(MoveTo(CarManager.instance.GetExitSlot().position));
    }
}
