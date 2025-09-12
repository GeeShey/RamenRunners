using System.Collections.Generic;
using UnityEngine;

//A common place to index arbitrary transforms
public class TransformManager : MonoBehaviour
{
    [SerializeField] public StringTransformDict transformMap = new StringTransformDict();

    public static TransformManager instance;
    private void Start()
    {
        instance = this;
    }

}
