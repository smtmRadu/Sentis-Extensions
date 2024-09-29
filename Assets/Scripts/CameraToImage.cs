using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraToImage : MonoBehaviour
{
    // Reference to the Image component
    public Image targetImage;

    // Reference to the Camera component
    public Camera cameraToCapture;

    // RenderTexture to capture the camera's image
    private RenderTexture renderTexture;

    // Texture2D to read the RenderTexture's pixels
    private Texture2D texture;

    // Sprite to display on the Image component
    private Sprite sprite;

    private void Start()
    {
        // Create a new RenderTexture with the camera's resolution
        renderTexture = new RenderTexture(cameraToCapture.pixelWidth, cameraToCapture.pixelHeight, 24);

        // Set the RenderTexture as the camera's target
        cameraToCapture.targetTexture = renderTexture;

        // Create a new Texture2D to read the RenderTexture's pixels
        texture = new Texture2D(renderTexture.width, renderTexture.height);

        // Create a new Sprite to display on the Image component
        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        // Set the Sprite on the Image component
        targetImage.sprite = sprite;

        // Start the coroutine to update the Sprite every frame
        StartCoroutine(UpdateSprite());
    }

    private IEnumerator UpdateSprite()
    {
        while (true)
        {
            // Read the RenderTexture's pixels into the Texture2D
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            // Update the Sprite's texture
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            // Wait for the next frame
            yield return new WaitForEndOfFrame();
        }
    }

    private void OnDestroy()
    {
        // Release the RenderTexture
        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
        }

        // Stop the coroutine
        StopAllCoroutines();
    }
}