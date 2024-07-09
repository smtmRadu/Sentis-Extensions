
using System;
using System.Linq;
using System.Text;
using Unity.Sentis;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

namespace kbradu
{
    public enum Origin
    {
        /// <summary>
        /// Similar to PIL/CV2.
        /// </summary>
        TopLeft,
        /// <summary>
        /// Similar to Unity.
        /// </summary>
        BottomLeft,
    }
    public static class Utils
    {
        public static TensorFloat TextureToTensor(Texture2D texture, Origin xoyPosition, int channels = 3)
        {
            if (texture == null)
            {
                Debug.LogError("Texture is null. Cannot convert to tensor.");
                return null;
            }

            int width = texture.width;
            int height = texture.height;

            TensorShape shape = new TensorShape(1, height, width, channels);
            TensorFloat tensor = TensorFloat.AllocZeros(shape);

            Color[] pixels = texture.GetPixels();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = xoyPosition == Origin.TopLeft ? (height - 1 - y) * width + x : y * width + x;
                    Color pixel = pixels[index];

                    if (channels == 1)
                        tensor[0, y, x, 0] = (pixel.r + pixel.b + pixel.g) / 3f;
                    else if (channels == 3)
                    {
                        tensor[0, y, x, 0] = pixel.r;
                        tensor[0, y, x, 1] = pixel.g;
                        tensor[0, y, x, 2] = pixel.b;
                    }
                    else if (channels == 4)
                    {
                        tensor[0, y, x, 0] = pixel.r;
                        tensor[0, y, x, 1] = pixel.g;
                        tensor[0, y, x, 2] = pixel.b;
                        tensor[0, y, x, 3] = pixel.a;
                    }
                    else
                    {
                        throw new System.NotImplementedException($"Unhandled number of channels ({channels})");
                    }
                }
            }

            return tensor;
        }
        public static Texture2D TensorToTexture(TensorFloat tensor)
        {
            if (tensor == null)
            {
                Debug.LogError("Tensor is null. Cannot convert to texture.");
                return null;
            }
            if (tensor.shape[0] > 1)
                throw new ArgumentException($"Allowed only 1 batch size, not {tensor.shape[0]}.");
            int height = tensor.shape[1];
            int width = tensor.shape[2];
            int channels = tensor.shape[3];

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (channels == 1)
                    {
                        float gray = tensor[0, y, x, 0];
                        pixels[y * width + x] = new Color(gray, gray, gray, 1f);
                    }
                    else if (channels == 3)
                    {
                        float r = tensor[0, y, x, 0];
                        float g = tensor[0, y, x, 1];
                        float b = tensor[0, y, x, 2];
                        pixels[y * width + x] = new Color(r, g, b, 1f);
                    }
                    else if (channels == 4)
                    {
                        float r = tensor[0, y, x, 0];
                        float g = tensor[0, y, x, 1];
                        float b = tensor[0, y, x, 2];
                        float a = tensor[0, y, x, 3];
                        pixels[y * width + x] = new Color(r, g, b, a);
                    }
                    else
                        throw new ArgumentException($"Note that input must be of shape (B, H, W, C). Tensor shape received {tensor.shape}");
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        public static string TensorToString(TensorFloat tensor)
        {          
            int rank = tensor.shape.rank;

            if (rank > 4)
                throw new NotImplementedException("Cannot handle tensors with rank higher than 4");

            StringBuilder sb = new();

            int[] shape = TensorShapeTo4DShape(tensor.shape);
            int batch = shape[0];
            int height = shape[1];
            int width = shape[2];
            int channels = shape[3];


            sb.Append($"Tensor({tensor.shape.ToArray().ToCommaSeparatedString()})");

            sb.Append("\n[");
            for (int l = 0; l < batch; l++)
            {
                if (l > 0)
                {
                    sb.Append("\n\n\n");
                    for (int indent = 0; indent < rank - 3; indent++)
                    {
                        sb.Append(" ");
                    }
                }
                if (rank > 3)
                    sb.Append("[");

                for (int h = 0; h < height; h++)
                {
                    if (h > 0)
                    {
                        sb.Append("\n\n");
                        for (int indent = 0; indent < rank - 2; indent++)
                        {
                            sb.Append(" ");
                        }
                    }
                    if (rank > 2)
                        sb.Append("[");

                    for (int w = 0; w < width; w++)
                    {
                        if (w > 0 && rank > 1)
                        {
                            sb.Append("\n");
                            for (int indent = 0; indent < rank - 1; indent++)
                            {
                                sb.Append(" ");
                            }
                        }
                        if (rank > 1)
                            sb.Append("[");

                        for (int c = 0; c < channels; c++)
                        {
                            if (c > 0)
                                sb.Append(", ");

                            sb.Append(tensor[l, h, w, c].ToString());
                        }

                        if (rank > 1)
                            sb.Append("]");
                    }

                    if (rank > 2)
                        sb.Append("]");
                }

                if (rank > 3)
                    sb.Append("]");
            }

            sb.Append("]");

            return sb.ToString();
        }
       
        public static TensorFloat CloneTensor(TensorFloat tensor)
        {
            TensorFloat outp = TensorFloat.AllocZeros(tensor.shape);
            for (int i = 0; i < outp.count; i++)
            {
                outp[i] = tensor[i];
            }
            return tensor;
        }
        public static Texture2D CloneTexture(Texture2D texture)
        {
            // Create a new Texture2D with the same dimensions and format as the source
            Texture2D clonedTexture = new Texture2D(
                texture.width,
                texture.height,
                texture.format,
                texture.mipmapCount > 1);

            // Copy the raw data from the source texture to the new texture
            Graphics.CopyTexture(texture, clonedTexture);

            // Ensure the new texture has the same filter mode, wrap mode, and anisoLevel
            clonedTexture.filterMode = texture.filterMode;
            clonedTexture.wrapMode = texture.wrapMode;
            clonedTexture.anisoLevel = texture.anisoLevel;

            // Apply the changes
            clonedTexture.Apply(false);

            return clonedTexture;
        }
        
        public static int[] TensorShapeTo4DShape(TensorShape shape)
        {
            int[] sp = shape.ToArray();
            int batch = 1;
            int height = 1;
            int width = 1;
            int channels = 1;

            switch (sp.Length)
            {
                case 4:
                    batch = sp[0];
                    height = sp[1];
                    width = sp[2];
                    channels = sp[3];
                    break;
                case 3:
                    height = sp[0];
                    width = sp[1];
                    channels = sp[2];
                    break;
                case 2:
                    width = sp[0];
                    channels = sp[1];
                    break;
                case 1:
                    channels = sp[0];
                    break;
                default:
                    throw new ArgumentException($"Unsupported shape with {sp.Length} dimensions.");
            }
            return new int[] { batch, height, width, channels };
        }
        public static class Vision
        {
            /// <summary>
            /// Returns the hex value of the given r, g & b values in range [0, 255].
            /// </summary>
            public static string HexOf(int r, int g, int b)
            {
                return string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);
            }
            /// <summary>
            /// Returns the hex value of the given r, g & b values in range [0, 1].
            /// </summary>
            public static string HexOf(float r, float g, float b)
            {
                int ri = (int)(r * 255.0f);
                int gi = (int)(g * 255.0f);
                int bi = (int)(b * 255.0f);

                return string.Format("#{0:X2}{1:X2}{2:X2}", ri, gi, bi);
            }
            public static void HWC2CHW(ref TensorFloat image)
            {
                if (image == null)
                {
                    Debug.LogError("Tensor is null. Cannot perform center crop.");
                    return;
                }
                int[] shape = TensorShapeTo4DShape(image.shape);

                int batch = shape[0];
                int height = shape[1];
                int width = shape[2];
                int channels = shape[3];

                TensorShape newShape = new TensorShape(batch, channels, height, width);
                TensorFloat permuted = TensorFloat.AllocZeros(newShape);

                for (int b = 0; b < batch; b++)
                {
                    for (int c = 0; c < channels; c++)
                    {
                        for (int h = 0; h < height; h++)
                        {
                            for (int w = 0; w < width; w++)
                            {
                                permuted[b,c, h, w] = image[b, h, w, c];
                            }
                        }
                    }
                }

                image.Dispose();
                image = permuted;
            }
            public static void CHW2HWC(ref TensorFloat image)
            {
                if (image == null)
                {
                    Debug.LogError("Tensor is null. Cannot perform center crop.");
                    return;
                }
                int[] shape = TensorShapeTo4DShape(image.shape);

                int batch = shape[0];
                int channels = shape[1];
                int height = shape[2];
                int width = shape[3];
                

                TensorShape newShape = new TensorShape(batch, height, width, channels);
                TensorFloat permuted = TensorFloat.AllocZeros(newShape);

                for (int b = 0; b < batch; b++)
                {
                    for (int c = 0; c < channels; c++)
                    {
                        for (int h = 0; h < height; h++)
                        {
                            for (int w = 0; w < width; w++)
                            {
                                permuted[b, h, w, c] = image[b, c, h, w];
                            }
                        }
                    }
                }

                image.Dispose();
                image = permuted;
            }
            public static void CenterCrop(ref TensorFloat image)
            {
                if (image == null)
                {
                    Debug.LogError("Tensor is null. Cannot perform center crop.");
                    return;
                }
                int[] shape = TensorShapeTo4DShape(image.shape);

                int batch = shape[0];
                int height = shape[1];
                int width = shape[2];
                int channels = shape[3];

                if (height == width)
                    return;

                int squareSize = Mathf.Min(height, width);

                int yOffset = (height - squareSize) / 2;
                int xOffset = (width - squareSize) / 2;

                TensorShape newShape = new TensorShape(batch, squareSize, squareSize, channels);
                TensorFloat croppedTensor = TensorFloat.AllocZeros(newShape);

                for (int b = 0; b < batch; b++)
                {
                    for (int y = 0; y < squareSize; y++)
                    {
                        for (int x = 0; x < squareSize; x++)
                        {
                            for (int c = 0; c < channels; c++)
                            {
                                croppedTensor[b, y, x, c] = image[b, y + yOffset, x + xOffset, c];
                            }
                        }
                    }
                }

                image.Dispose();
                image = croppedTensor;
            }
            public static void ToGrayscale(ref TensorFloat image, bool average = false)
            {
                if (image == null)
                {
                    Debug.LogError("Tensor is null. Cannot convert to grayscale.");
                    return;
                }

                if (image.shape[3] < 3)
                {
                    Debug.LogError("Tensor does not have 3 or 4 channels (RGB/RGBA). Cannot convert to grayscale.");
                    return;
                }

                int[] shape = TensorShapeTo4DShape(image.shape);

                int batch = shape[0];
                int height = shape[1];
                int width = shape[2];
                int channels = shape[3];

                TensorShape newShape = new TensorShape(batch, height, width, 1);
                TensorFloat grayscaleTensor = TensorFloat.AllocZeros(newShape);

                for (int l = 0; l < batch; l++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            float r = image[l, y, x, 0];
                            float g = image[l, y, x, 1];
                            float b = image[l, y, x, 2];

                            float grayscale = average ? (r + g + b) / 3f : 0.299f * r + 0.587f * g + 0.114f * b;
                            grayscaleTensor[l, y, x, 0] = grayscale;
                        }
                    }
                }

                image.Dispose();
                image = grayscaleTensor;
            }
            public static void FlipHorizontally(ref TensorFloat image)
            {
                int[] shape = TensorShapeTo4DShape(image.shape);

                int batch = shape[0];
                int height = shape[1];
                int width = shape[2];
                int channels = shape[3];

                TensorFloat flippedTensor = TensorFloat.AllocZeros(image.shape);

                for (int b = 0; b < batch; b++)
                {
                    for (int h = 0; h < height; h++)
                    {
                        for (int w = 0; w < width; w++)
                        {
                            for (int c = 0; c < channels; c++)
                            {
                                int flippedW = width - 1 - w;
                                flippedTensor[b, h, flippedW, c] = image[b, h, w, c];
                            }
                        }
                    }
                }

                image.Dispose();
                image = flippedTensor;
            }
            public static void FlipVertically(ref TensorFloat image)
            {
                int[] shape = TensorShapeTo4DShape(image.shape);

                int batch = shape[0];
                int height = shape[1];
                int width = shape[2];
                int channels = shape[3];

                TensorFloat flippedTensor = TensorFloat.AllocZeros(image.shape);

                for (int b = 0; b < batch; b++)
                {
                    for (int h = 0; h < height; h++)
                    {
                        for (int w = 0; w < width; w++)
                        {
                            for (int c = 0; c < channels; c++)
                            {
                                int flippedH = height - 1 - h;
                                flippedTensor[b, flippedH, w, c] = image[b, h, w, c];
                            }
                        }
                    }
                }

                image.Dispose();
                image = flippedTensor;
            }
            public static void Rescale(ref TensorFloat image, float scale)
            {
                int width = (int)MathF.Floor(image.shape[2] * scale);
                int height = (int)MathF.Floor(image.shape[1] * scale);
                int channels = image.shape[3];
                int batch_size = image.shape[0];

                TensorShape newShape = new TensorShape(batch_size, height, width, channels);
                TensorFloat scaled = TensorFloat.AllocZeros(newShape);

                for (int b = 0; b < batch_size; b++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            float x0 = x / scale;
                            float y0 = y / scale;

                            int x1 = (int)MathF.Floor(x0);
                            int y1 = (int)MathF.Floor(y0);
                            int x2 = (int)MathF.Min(x1 + 1, image.shape[2] - 1);
                            int y2 = (int)MathF.Min(y1 + 1, image.shape[1] - 1);

                            float dx = x0 - x1;
                            float dy = y0 - y1;

                            for (int c = 0; c < channels; c++)
                            {
                                float q11 = image[b, y1, x1, c];
                                float q21 = image[b, y1, x2, c];
                                float q12 = image[b, y2, x1, c];
                                float q22 = image[b, y2, x2, c];

                                float pix = (1 - dx) * (1 - dy) * q11 +
                                            dx * (1 - dy) * q21 +
                                            (1 - dx) * dy * q12 +
                                            dx * dy * q22;

                                scaled[b, y, x, c] = pix;
                            }
                        }
                    }
                }

                image.Dispose();
                image = scaled;
            }
            public static void Resize(ref TensorFloat image, int newWidth, int newHeight)
            {
                if (image == null)
                {
                    Debug.LogError("Tensor is null. Cannot perform resize.");
                    return;
                }

               

                int batch = image.shape[0];
                int oldHeight = image.shape[1];
                int oldWidth = image.shape[2];
                int channels = image.shape[3];

                if (oldWidth == newWidth && oldHeight == newHeight)
                    return;

                // Calculate the scaling factor to preserve aspect ratio
                float scale = Mathf.Min((float)newWidth / oldWidth, (float)newHeight / oldHeight);
                int scaledWidth = (int)(oldWidth * scale);
                int scaledHeight = (int)(oldHeight * scale);

                // Calculate padding
                int padX = (newWidth - scaledWidth) / 2;
                int padY = (newHeight - scaledHeight) / 2;

                TensorShape newShape = new TensorShape(batch, newHeight, newWidth, channels);
                TensorFloat resized = TensorFloat.AllocZeros(newShape);

                float scaleX = (float)(oldWidth - 1) / (scaledWidth - 1);
                float scaleY = (float)(oldHeight - 1) / (scaledHeight - 1);

                for (int b = 0; b < batch; b++)
                {
                    for (int y = 0; y < scaledHeight; y++)
                    {
                        for (int x = 0; x < scaledWidth; x++)
                        {
                            float srcX = x * scaleX;
                            float srcY = y * scaleY;

                            int x1 = (int)MathF.Floor(srcX);
                            int y1 = (int)MathF.Floor(srcY);
                            int x2 = Mathf.Min(x1 + 1, oldWidth - 1);
                            int y2 = Mathf.Min(y1 + 1, oldHeight - 1);

                            float dx = srcX - x1;
                            float dy = srcY - y1;

                            for (int c = 0; c < channels; c++)
                            {
                                float q11 = image[b, y1, x1, c];
                                float q21 = image[b, y1, x2, c];
                                float q12 = image[b, y2, x1, c];
                                float q22 = image[b, y2, x2, c];

                                float pix = (1 - dx) * (1 - dy) * q11 +
                                            dx * (1 - dy) * q21 +
                                            (1 - dx) * dy * q12 +
                                            dx * dy * q22;

                                resized[b, y + padY, x + padX, c] = pix;
                            }
                        }
                    }
                }

                image.Dispose();
                image = resized;
            }
            public static void AffineTransform(TensorFloat tensor, float weight = 1, float bias = 0)
            {
                if (tensor == null)
                {
                    Debug.LogError("Tensor is null. Cannot perform affine transform.");
                    return;
                }
                int totalElements = tensor.count;

                for (int i = 0; i < totalElements; i++)
                {
                    tensor[i] = tensor[i] * weight + bias;
                }
                
            }
            public static void Rotate(ref TensorFloat image, float angleDegrees)
            {
                if (image == null)
                {
                    Debug.LogError("Tensor is null. Cannot perform rotation.");
                    return;
                }
                int batch = image.shape[0];
                int channels = image.shape[3];
                int width = image.shape[2];
                int height = image.shape[1];

                TensorShape newShape = new TensorShape(batch, height, width, channels);
                TensorFloat rotated = TensorFloat.AllocZeros(newShape);

                // Convert angle to radians
                float angleRadians = angleDegrees * Mathf.Deg2Rad;
                float cosTheta = Mathf.Cos(angleRadians);
                float sinTheta = Mathf.Sin(angleRadians);

                // Calculate the center of the image
                float centerX = width / 2f;
                float centerY = height / 2f;

                for (int b = 0; b < batch; b++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            // Translate to origin
                            float xOffset = x - centerX;
                            float yOffset = y - centerY;

                            // Rotate
                            float rotatedX = xOffset * cosTheta - yOffset * sinTheta;
                            float rotatedY = xOffset * sinTheta + yOffset * cosTheta;

                            // Translate back and round to nearest pixel
                            int srcX = Mathf.RoundToInt(rotatedX + centerX);
                            int srcY = Mathf.RoundToInt(rotatedY + centerY);

                            // Check if the source pixel is within bounds
                            if (srcX >= 0 && srcX < width && srcY >= 0 && srcY < height)
                            {
                                for (int c = 0; c < channels; c++)
                                {
                                    rotated[b, y, x, c] = image[b, srcY, srcX, c];
                                }
                            }
                            else
                            {
                                // Set out-of-bounds pixels to a default value (e.g., black)
                                for (int c = 0; c < channels; c++)
                                {
                                    rotated[b, y, x, c] = 0f;
                                }
                            }
                        }
                    }
                }

                image.Dispose();
                image = rotated;
            }

        }



        public static class Math
        {
            public static float Sigmoid(float x)
            {
                return 1f / (1f + Mathf.Exp(-x));
            }
            public static float Tanh(float x)
            {
                float e2x = Mathf.Exp(x);
                return (e2x - 1) / (e2x + 1);
            }
            public static int ArgMax(float[] arr)
            {
                int maxIndex = 0;
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] > arr[maxIndex])
                        maxIndex = i;
                }
                return maxIndex;
            }
            public static float[] SoftMax(float[] arr)
            {
                float[] result = new float[arr.Length];
                float max = arr.Max();

                float sum = 0.0f;
                for (int i = 0; i < arr.Length; i++)
                {
                    result[i] = Mathf.Exp(arr[i] - max);
                    sum += result[i];
                }

                for (int i = 0; i < arr.Length; i++)
                {
                    result[i] /= sum;
                }

                return result;
            }
        }
    }
}