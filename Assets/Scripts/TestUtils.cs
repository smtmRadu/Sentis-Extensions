using kbradu;
using System.Linq;
using Unity.Sentis;
using UnityEngine;

public class TestUtils : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TensorFloat tenns = Utils.Random01Tensor(new TensorShape(100, 200, 3));
        TensorFloat tenns2 = new TensorFloat(tenns.shape, tenns.ToReadOnlyArray());
        Utils.Vision.AffineTransform(tenns, 2, 1, false);
        Utils.Vision.AffineTransform(tenns2, 2, 1, true);

        print(tenns.ToReadOnlyArray().SequenceEqual(tenns2.ToReadOnlyArray()));

    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
