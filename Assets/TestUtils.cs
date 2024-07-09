using kbradu;
using Unity.Sentis;
using UnityEngine;

public class TestUtils : MonoBehaviour
{
    public DisplayRuntime display;
    public Texture2D image;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TensorFloat x = new TensorFloat(new TensorShape(2, 2), new float[] { 1, 2, 3, 4 });
        print(Utils.TensorToString(x));
        Utils.Vision.AffineTransform(x, 2, -4);
        print(Utils.TensorToString(x));

        TensorFloat y = Utils.CloneTensor(x);
        print(Utils.TensorToString(y));
        print(y.ToReadOnlyArray());

    }


    // Update is called once per frame
    void Update()
    {
        TensorFloat x = Utils.TextureToTensor(image, Origin.BottomLeft, 3);
        // Utils.Vision.FlipVertically(ref x);
        Texture2D tex = Utils.TensorToTexture(x);

        display.SetTexture(tex);

        x.Dispose();
    }
}
