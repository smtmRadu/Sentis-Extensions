using kbradu;
using System.Linq;
using Unity.Sentis;
using UnityEngine;

public class TestUtils : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TensorFloat tens = TensorFloatExtensions.Random01(100, 100);

    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
