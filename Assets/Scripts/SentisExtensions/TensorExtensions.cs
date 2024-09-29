using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEngine;

namespace SentisExtensions
{
    public static class TensorExtensions
    {
        /// INIT

        /// <summary>
        /// (Read-Only) Get the last 4 dimensions of the tensor. (B, H, W, C).
        /// </summary>
        /// <param name="tensor"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static int[] Shape4D(this Tensor<float> tensor)
        {
            int[] sp = tensor.shape.ToArray();
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
        public static Tensor<float> Zeros(params int[] shape)
        {
            return new Tensor<float>(new TensorShape(shape)); 
        }
        public static Tensor<float> Random01(params int[] shape)
        {
            float[] nums = new float[shape.Aggregate(1, (x, y) => x * y)];
            for (int i = 0; i < nums.Length; i++)
            {
                nums[i] = UnityEngine.Random.value;
            }
            return new Tensor<float>(new TensorShape(shape), nums);
        }
        public static Tensor<float> FromTexture(Texture2D texture, ImageShape retain_in_format, OriginCoord read_like, int channels = 3, bool multithread = true)
        {
            if (retain_in_format == ImageShape.HWC)
                return HWCFromTexture(texture, read_like, channels, multithread);
            else if (retain_in_format == ImageShape.CHW)
                return CHWFromTexture(texture, read_like, channels, multithread);
            else
                throw new NotImplementedException();
        }
        /// <summary>
        /// Output tensor shape (B, H, W, C)
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="read_like"></param>
        /// <param name="channels"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private static Tensor<float> HWCFromTexture(Texture2D texture, OriginCoord read_like, int channels = 3, bool multithread = true)
        {
            if (texture == null)
            {
                UnityEngine.Debug.LogError("Texture is null. Cannot convert to tensor.");
                return null;
            }

            int width = texture.width;
            int height = texture.height;

            TensorShape shape = new TensorShape(1, height, width, channels);
            Tensor<float> tensor =  new Tensor<float>(shape);

            Color[] pixels = texture.GetPixels();

            if (multithread)
            {
                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = read_like == OriginCoord.OpenCV ? (height - 1 - y) * width + x : y * width + x;
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
                });
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = read_like == OriginCoord.OpenCV ? (height - 1 - y) * width + x : y * width + x;
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
            }


            return tensor;
        }
        /// <summary>
        /// Output tensor shape (B, C, H, W)
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="read_like"></param>
        /// <param name="channels"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private static Tensor<float> CHWFromTexture(Texture2D texture, OriginCoord read_like, int channels = 3, bool multithread = true)
        {
            if (texture == null)
            {
                UnityEngine.Debug.LogError("Texture is null. Cannot convert to tensor.");
                return null;
            }

            int width = texture.width;
            int height = texture.height;

            TensorShape shape = new TensorShape(1, channels, height, width);
            Tensor<float> tensor = new Tensor<float>(shape);

            Color[] pixels = texture.GetPixels();

            if (multithread)
            {
                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = read_like == OriginCoord.OpenCV ? (height - 1 - y) * width + x : y * width + x;
                        Color pixel = pixels[index];

                        if (channels == 1)
                            tensor[0, 0, y, x] = (pixel.r + pixel.b + pixel.g) / 3f;
                        else if (channels == 3)
                        {
                            tensor[0, 0, y, x] = pixel.r;
                            tensor[0, 1, y, x] = pixel.g;
                            tensor[0, 2, y, x] = pixel.b;
                        }
                        else if (channels == 4)
                        {
                            tensor[0, 0, y, x] = pixel.r;
                            tensor[0, 1, y, x] = pixel.g;
                            tensor[0, 2, y, x] = pixel.b;
                            tensor[0, 3, y, x] = pixel.a;
                        }
                        else
                        {
                            throw new System.NotImplementedException($"Unhandled number of channels ({channels})");
                        }
                    }
                });
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = read_like == OriginCoord.OpenCV ? (height - 1 - y) * width + x : y * width + x;
                        Color pixel = pixels[index];

                        if (channels == 1)
                            tensor[0, 0, y, x] = (pixel.r + pixel.b + pixel.g) / 3f;
                        else if (channels == 3)
                        {
                            tensor[0, 0, y, x] = pixel.r;
                            tensor[0, 1, y, x] = pixel.g;
                            tensor[0, 2, y, x] = pixel.b;
                        }
                        else if (channels == 4)
                        {
                            tensor[0, 0, y, x] = pixel.r;
                            tensor[0, 1, y, x] = pixel.g;
                            tensor[0, 2, y, x] = pixel.b;
                            tensor[0, 3, y, x] = pixel.a;
                        }
                        else
                        {
                            throw new System.NotImplementedException($"Unhandled number of channels ({channels})");
                        }
                    }
                }
            }


            return tensor;
        }

        /// OPERATIONS + AUTODISPOSE

        /// <summary>
        /// Rotates an image of shape (B, H, W, C).
        /// </summary>
        /// <param name="image"></param>
        /// <param name="angleDegrees"></param>
        public static Tensor<float> RotateBHWC(this Tensor<float> image, float angleDegrees, bool self_dispose = true)
        {
            if (image == null)
            {
                UnityEngine.Debug.LogError("Tensor is null. Cannot perform rotation.");
                return null;
            }
            int batch = image.shape[0];
            int channels = image.shape[3];
            int width = image.shape[2];
            int height = image.shape[1];

            TensorShape newShape = new TensorShape(batch, height, width, channels);
            Tensor<float> rotated = new Tensor<float>(newShape);

            // Convert angle to radians
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            float cosTheta = MathF.Cos(angleRadians);
            float sinTheta = MathF.Sin(angleRadians);

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

            if(self_dispose)
                image.Dispose();
            return rotated;
        }
        /// <summary>
        /// Image shape: any
        /// </summary>
        /// <param name="image"></param>
        public static Tensor<float> AffineTransform(this Tensor<float> image, float weight = 1, float bias = 0, bool multithread = true, bool self_dispose = true)
        {
            if (image == null)
            {
                UnityEngine.Debug.LogError("Tensor is null. Cannot perform affine transform.");
                throw new Exception("Tensor is null. Cannot perform affine transform.");
            }
            int totalElements = image.count;

            Tensor<float> affined = new Tensor<float>(image.shape);

            if (multithread)
            {
                Parallel.For(0, totalElements, i =>
                {
                    affined[i] = image[i] * weight + bias;
                });
            }
            else
            {
                for (int i = 0; i < totalElements; i++)
                {
                    affined[i] = image[i] * weight + bias;
                }
            }

            if (self_dispose)
                image.Dispose();
            return affined;

        }
        /// <summary>
        /// Image shape (B, H, W, C)
        /// </summary>
        /// <param name="image"></param>
        public static Tensor<float> ResizeBHWC(this Tensor<float> image, int newWidth, int newHeight, bool multithread = true, bool self_dispose = true)
        {
            if (image == null)
            {
                UnityEngine.Debug.LogError("Tensor is null. Cannot perform resize.");
                throw new Exception("Tensor is null. Cannot perform resize.");
            }

            int batch = image.shape[0];
            int oldHeight = image.shape[1];
            int oldWidth = image.shape[2];
            int channels = image.shape[3];

            if (oldWidth == newWidth && oldHeight == newHeight)
                throw new Exception();

            // Calculate the scaling factor to preserve aspect ratio
            float scale = Mathf.Min((float)newWidth / oldWidth, (float)newHeight / oldHeight);
            int scaledWidth = (int)(oldWidth * scale);
            int scaledHeight = (int)(oldHeight * scale);

            // Calculate padding
            int padX = (newWidth - scaledWidth) / 2;
            int padY = (newHeight - scaledHeight) / 2;

            TensorShape newShape = new TensorShape(batch, newHeight, newWidth, channels);
            Tensor<float> resized = new Tensor<float>(newShape);

            float scaleX = (float)(oldWidth - 1) / (scaledWidth - 1);
            float scaleY = (float)(oldHeight - 1) / (scaledHeight - 1);

            if (multithread)
            {
                Tensor<float> refImage = image;

                Parallel.For(0, scaledHeight, y =>
                {
                    for (int b = 0; b < batch; b++)
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
                                float q11 = refImage[b, y1, x1, c];
                                float q21 = refImage[b, y1, x2, c];
                                float q12 = refImage[b, y2, x1, c];
                                float q22 = refImage[b, y2, x2, c];

                                float pix = (1f - dx) * (1f - dy) * q11 +
                                            dx * (1f - dy) * q21 +
                                            (1f - dx) * dy * q12 +
                                            dx * dy * q22;

                                resized[b, y + padY, x + padX, c] = pix;
                            }
                        }
                    }
                });


            }
            else
            {
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

            }

            if(self_dispose)
            image.Dispose();
            return resized;
        }
        /// <summary>
        /// Image shape (B, H, W, C)
        /// </summary>
        /// <param name="image"></param>
        public static Tensor<float> CenterCropBHWC(this Tensor<float> image, bool multithread = true, bool self_dispose = true)
        {
            if (image == null)
            {
                UnityEngine.Debug.LogError("Tensor is null. Cannot perform center crop.");
                throw new Exception("Tensor is null. Cannot perform center crop.");
            }
            int[] shape = image.Shape4D();

            int batch = shape[0];
            int height = shape[1];
            int width = shape[2];
            int channels = shape[3];

            if (height == width)
                throw new Exception("Tensor is null. Cannot perform center crop.");

            int squareSize = Mathf.Min(height, width);

            int yOffset = (height - squareSize) / 2;
            int xOffset = (width - squareSize) / 2;

            TensorShape newShape = new TensorShape(batch, squareSize, squareSize, channels);
            Tensor<float> croppedTensor = new Tensor<float>(newShape);

            if (multithread)
            {
                Tensor<float> imageRef = image;

                Parallel.For(0, squareSize, y =>
                {
                    for (int b = 0; b < batch; b++)
                    {
                        for (int x = 0; x < squareSize; x++)
                        {
                            for (int c = 0; c < channels; c++)
                            {
                                croppedTensor[b, y, x, c] = imageRef[b, y + yOffset, x + xOffset, c];
                            }
                        }
                    }
                });

            }
            else
            {
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
            }

            if(self_dispose)
                image.Dispose();
            return croppedTensor;
        }
        /// <summary>
        /// Image shape (B, H, W, C) to (B, H, W, 1).
        /// If average = true, the avg of all 3 channels is taken (r+g+b)/3, other wise is weighted like: 0.299f * r + 0.587f * g + 0.114f * b;.
        /// </summary>
        /// <param name="image"></param>
        public static Tensor<float> ToGrayscaleBHWC(this Tensor<float> image, bool average = false, bool self_dispose = true)
        {
            if (image == null)
            {
                UnityEngine.Debug.LogError("Tensor is null. Cannot convert to grayscale.");
                throw new Exception("Tensor is null. Cannot convert to grayscale.");
            }

            if (image.shape[3] < 3)
            {
                UnityEngine.Debug.LogError("Tensor does not have 3 or 4 channels (RGB/RGBA). Cannot convert to grayscale.");
                throw new Exception("Tensor does not have 3 or 4 channels (RGB/RGBA). Cannot convert to grayscale.");
            }

            int[] shape = image.Shape4D();

            int batch = shape[0];
            int height = shape[1];
            int width = shape[2];
            int channels = shape[3];

            TensorShape newShape = new TensorShape(batch, height, width, 1);
            Tensor<float> grayscaleTensor = new Tensor<float>(newShape);

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
            if(self_dispose)
                image.Dispose();
            return grayscaleTensor;
        }
        /// <summary>
        /// Image shape (B, H, W, C)
        /// </summary>
        /// <param name="image"></param>
        public static Tensor<float> FlipHorizontallyBHWC(this Tensor<float> image, bool self_dispose = true)
        {
            int[] shape = image.Shape4D();

            int batch = shape[0];
            int height = shape[1];
            int width = shape[2];
            int channels = shape[3];

            Tensor<float> flippedTensor = new Tensor<float>(image.shape);

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
            if(self_dispose)
            image.Dispose();
            return flippedTensor;
        }
        /// <summary>
        /// Image shape (B, H, W, C)
        /// </summary>
        /// <param name="image"></param>
        public static Tensor<float> FlipVerticallyBHWC(this Tensor<float> image, bool self_dispose = true)
        {
            int[] shape = image.Shape4D();

            int batch = shape[0];
            int height = shape[1];
            int width = shape[2];
            int channels = shape[3];

            Tensor<float> flippedTensor = new Tensor<float>(image.shape);

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
            if(self_dispose)
            image.Dispose();
            return flippedTensor;
        }
        /// <summary>
        /// Image shape (B, H, W, C)
        /// </summary>
        /// <param name="image"></param>
        public static Tensor<float> RescaleBHWC(this Tensor<float> image, float scale, bool self_dispose = true)
        {
            int width = (int)MathF.Floor(image.shape[2] * scale);
            int height = (int)MathF.Floor(image.shape[1] * scale);
            int channels = image.shape[3];
            int batch_size = image.shape[0];

            TensorShape newShape = new TensorShape(batch_size, height, width, channels);
            Tensor<float> scaled = new Tensor<float>(newShape);

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
            if(self_dispose)
            image.Dispose();
            return scaled;
        }
        public static Tensor<float> HWC2CHW(this Tensor<float> image, bool multithread = true, bool self_dispose = true)
        {
            if (image == null)
            {
                UnityEngine.Debug.LogError("Tensor is null. Cannot perform center crop.");
                throw new Exception("Tensor is null. Cannot perform center crop.");
            }
            int[] shape = image.Shape4D();

            int batch = shape[0];
            int height = shape[1];
            int width = shape[2];
            int channels = shape[3];

            TensorShape newShape = new TensorShape(batch, channels, height, width);
            Tensor<float> permuted = new Tensor<float>(newShape);

            if (multithread)
            {
                Tensor<float> refImage = image;

                Parallel.For(0, height, h =>
                {
                    for (int b = 0; b < batch; b++)
                    {
                        for (int c = 0; c < channels; c++)
                        {
                            for (int w = 0; w < width; w++)
                            {
                                permuted[b, c, h, w] = refImage[b, h, w, c];
                            }
                        }
                    }
                });
            }
            else
            {
                for (int b = 0; b < batch; b++)
                {
                    for (int c = 0; c < channels; c++)
                    {
                        for (int h = 0; h < height; h++)
                        {
                            for (int w = 0; w < width; w++)
                            {
                                permuted[b, c, h, w] = image[b, h, w, c];
                            }
                        }
                    }
                }
            }

            if(self_dispose)
            image.Dispose();
            return permuted;
        }
        public static Tensor<float> CHW2HWC(this Tensor<float> image, bool multithread = true, bool self_dispose = true)
        {
            if (image == null)
            {
                UnityEngine.Debug.LogError("Tensor is null. Cannot perform center crop.");
                throw new Exception("Tensor is null. Cannot perform center crop.");
            }
            int[] shape = image.Shape4D();

            int batch = shape[0];
            int channels = shape[1];
            int height = shape[2];
            int width = shape[3];


            TensorShape newShape = new TensorShape(batch, height, width, channels);
            Tensor<float> permuted = new Tensor<float>(newShape);

            if (multithread)
            {
                Tensor<float> refImage = image;
                Parallel.For(0, height, h =>
                {
                    for (int b = 0; b < batch; b++)
                    {
                        for (int c = 0; c < channels; c++)
                        {
                            for (int w = 0; w < width; w++)
                            {
                                permuted[b, h, w, c] = refImage[b, c, h, w];
                            }
                        }
                    }
                });
            }
            else
            {
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
            }

            if (self_dispose)
                image.Dispose();
            return permuted;
        }


        /// IN PLACE


        [Obsolete("You might use the non-inplace autodisposable form.")]
        public static void AffineTransform_(this Tensor<float> image, float weight = 1, float bias = 0, bool multithread = true)
        {
            if (image == null)
            {
                UnityEngine.Debug.LogError("Tensor is null. Cannot perform affine transform.");
                return;
            }
            int totalElements = image.count;

            if (multithread)
            {
                Parallel.For(0, totalElements, i =>
                {
                    image[i] = image[i] * weight + bias;
                });
            }
            else
            {
                for (int i = 0; i < totalElements; i++)
                {
                    image[i] = image[i] * weight + bias;
                }
            }

        }


        /// OTHER

        /// <summary>
        /// Tensor shape (1, H, W, C).
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Texture2D ToTexture(this Tensor<float> image, ImageShape tensor_format, bool multithread = true)
        {
            if (tensor_format == ImageShape.HWC)
                return FromHWC2Texture(image, multithread);
            else if (tensor_format == ImageShape.CHW)
                return FromCHW2Texture(image, multithread);
            else
                throw new NotImplementedException();
        }   
        private static Texture2D FromHWC2Texture(this Tensor<float> tensorHWC, bool multithread = true)
        {
            if (tensorHWC == null)
            {
                UnityEngine.Debug.LogError("Tensor is null. Cannot convert to texture.");
                return null;
            }

            int[] shapeIn4D = tensorHWC.Shape4D();

            if (shapeIn4D[0] > 1)
                throw new ArgumentException($"Allowed only 1 batch size, not {shapeIn4D[0]}.");


            int height = shapeIn4D[1];
            int width = shapeIn4D[2];
            int channels = shapeIn4D[3];

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color[] pixels = new Color[width * height];

            if (multithread)
            {
                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (channels == 1)
                        {
                            float gray = tensorHWC[0, y, x, 0];
                            pixels[y * width + x] = new Color(gray, gray, gray, 1f);
                        }
                        else if (channels == 3)
                        {
                            float r = tensorHWC[0, y, x, 0];
                            float g = tensorHWC[0, y, x, 1];
                            float b = tensorHWC[0, y, x, 2];
                            pixels[y * width + x] = new Color(r, g, b, 1f);
                        }
                        else if (channels == 4)
                        {
                            float r = tensorHWC[0, y, x, 0];
                            float g = tensorHWC[0, y, x, 1];
                            float b = tensorHWC[0, y, x, 2];
                            float a = tensorHWC[0, y, x, 3];
                            pixels[y * width + x] = new Color(r, g, b, a);
                        }
                        else
                            throw new ArgumentException($"Note that input must be of shape (B, H, W, C). Tensor shape received {tensorHWC.shape}");
                    }
                });
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (channels == 1)
                        {
                            float gray = tensorHWC[0, y, x, 0];
                            pixels[y * width + x] = new Color(gray, gray, gray, 1f);
                        }
                        else if (channels == 3)
                        {
                            float r = tensorHWC[0, y, x, 0];
                            float g = tensorHWC[0, y, x, 1];
                            float b = tensorHWC[0, y, x, 2];
                            pixels[y * width + x] = new Color(r, g, b, 1f);
                        }
                        else if (channels == 4)
                        {
                            float r = tensorHWC[0, y, x, 0];
                            float g = tensorHWC[0, y, x, 1];
                            float b = tensorHWC[0, y, x, 2];
                            float a = tensorHWC[0, y, x, 3];
                            pixels[y * width + x] = new Color(r, g, b, a);
                        }
                        else
                            throw new ArgumentException($"Note that input must be of shape (B, H, W, C). Tensor shape received {tensorHWC.shape}");
                    }
                }
            }


            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        private static Texture2D FromCHW2Texture(this Tensor<float> tensorCHW, bool multithread = true)
        {
            if (tensorCHW == null)
            {
                UnityEngine.Debug.LogError("Tensor is null. Cannot convert to texture.");
                return null;
            }

            int[] shapeIn4D = tensorCHW.Shape4D();

            if (shapeIn4D[0] > 1)
                throw new ArgumentException($"Allowed only 1 batch size, not {shapeIn4D[0]}.");


            int height = shapeIn4D[3];
            int width = shapeIn4D[2];
            int channels = shapeIn4D[1];

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color[] pixels = new Color[width * height];

            if (multithread)
            {
                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (channels == 1)
                        {
                            float gray = tensorCHW[0, 0, y, x];
                            pixels[y * width + x] = new Color(gray, gray, gray, 1f);
                        }
                        else if (channels == 3)
                        {
                            float r = tensorCHW[0, 0, y, x];
                            float g = tensorCHW[0, 1, y, x];
                            float b = tensorCHW[0, 2, y, x];
                            pixels[y * width + x] = new Color(r, g, b, 1f);
                        }
                        else if (channels == 4)
                        {
                            float r = tensorCHW[0, 0, y, x];
                            float g = tensorCHW[0, 1, y, x];
                            float b = tensorCHW[0, 2, y, x];
                            float a = tensorCHW[0, 3, y, x];
                            pixels[y * width + x] = new Color(r, g, b, a);
                        }
                        else
                            throw new ArgumentException($"Note that input must be of shape (B, H, W, C). Tensor shape received {tensorCHW.shape}");
                    }
                });
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (channels == 1)
                        {
                            float gray = tensorCHW[0, 0, y, x];
                            pixels[y * width + x] = new Color(gray, gray, gray, 1f);
                        }
                        else if (channels == 3)
                        {
                            float r = tensorCHW[0, 0, y, x];
                            float g = tensorCHW[0, 1, y, x];
                            float b = tensorCHW[0, 2, y, x];
                            pixels[y * width + x] = new Color(r, g, b, 1f);
                        }
                        else if (channels == 4)
                        {
                            float r = tensorCHW[0, 0, y, x];
                            float g = tensorCHW[0, 1, y, x];
                            float b = tensorCHW[0, 2, y, x];
                            float a = tensorCHW[0, 3, y, x];
                            pixels[y * width + x] = new Color(r, g, b, a);
                        }
                        else
                            throw new ArgumentException($"Note that input must be of shape (B, C, H, W). Tensor shape received {tensorCHW.shape}");
                    }
                }
            }


            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        /// <summary>
        /// Converts the current tensor to string. Note: only the last 4 dimensions are displayed.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static string ToFullString(this Tensor<float> image)
        {
            int rank = image.shape.rank;

            if (rank > 4)
                throw new NotImplementedException("Cannot handle tensors with rank higher than 4");

            StringBuilder sb = new();

            int[] shape = image.Shape4D();
            int batch = shape[0];
            int height = shape[1];
            int width = shape[2];
            int channels = shape[3];


            sb.Append($"Tensor({image.shape.ToArray().ToCommaSeparatedString()})");

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

                            sb.Append(image[l, h, w, c].ToString());
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
        /// <summary>
        /// Compares the shape and the elements.
        /// </summary>
        /// <param name="tensor1"></param>
        /// <param name="tensor2"></param>
        /// <returns></returns>
        public static bool ReallyEquals(this Tensor<float> tensor, Tensor<float> other)
        {
            if (tensor.shape != other.shape)
                return false;
            if (tensor.DownloadToArray() != other.DownloadToArray())
                return false;

            return true;
        }
        /// <summary>
        /// Clones the current tensor.
        /// </summary>
        /// <param name="tensor"></param>
        /// <returns></returns>
        public static Tensor<float> Clone(this Tensor<float> tensor)
        {
            Tensor<float> outp = new Tensor<float>(tensor.shape);
            for (int i = 0; i < outp.count; i++)
            {
                outp[i] = tensor[i];
            }
            return tensor;
        }
    }
}

