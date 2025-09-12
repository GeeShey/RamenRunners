using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Splines;



public class Car : MonoBehaviour
{

    public Order order;
    [SerializeField]
    public LineRenderer lineRenderer;
    public bool reachedPickupPoint = false;

    public bool DEBUG;
    public void StartOrder()
    {
        order.assignedCar = this;
        if (CarManager.instance.OrderSlotAvailable())
        {

            StartCoroutine(OrderLoop());

        }
        else
        {
            CarManager.instance.AddCarToWaitingList(this);
         
        }
    }

    private void Update()
    {
        if (DEBUG)
        {
            if (order != null && order.assignedWorker != null)
            {
                //draw line from car to worker
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, order.assignedWorker.transform.position);
            }
        }
    }

    private IEnumerator MoveTo(Vector3 destination, float timeToDestination = 2.0f, Action onComplete = null)
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
        onComplete?.Invoke();

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



        yield return StartCoroutine(MoveTo(pickupSlot.position, 2, () => reachedPickupPoint = true));

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


        yield return new WaitUntil(() => orderHanded);
        if (DEBUG)
            lineRenderer.enabled = true;

        StartCoroutine(MoveToExitSlot());

    }


    public void MoveToCheckoutSlot()
    {

    }

    private IEnumerator MoveToExitSlot()
    {
        lineRenderer.enabled = false;
        yield return StartCoroutine(MoveTo(CarManager.instance.GetExitSlot().position));
        Destroy(gameObject);
    }
}
