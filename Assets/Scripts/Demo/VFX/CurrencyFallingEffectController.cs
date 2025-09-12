using UnityEngine;

public class CurrencyFallingEffectController : MonoBehaviour
{
    public SplineVisualizer[] splineViz;
    public Transform EffectCollectionPoint;
    public float timeToCollectionPoint = 2.0f;
    public static CurrencyFallingEffectController instance;

    private int particleReachedCount;
    private void Start()
    {
        instance = this;
    }
    public void activateEffect()
    {
        //Action action = () => { 
        //    particleReachedCount++; 
        //    if(particleReachedCount == splineViz.Length)
        //    {

        //    }
        //};
        foreach (var spline in splineViz) 
        {
            spline.AnimateAlongPath(1);
        }

    }




}
