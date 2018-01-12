using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PrecisionJump
{
    class Others
    {
        static unsafe void DetectColorWithUnsafe(Bitmap image, byte searchedR, byte searchedG, int searchedB, int tolerance)
        {
            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width,
              image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int bytesPerPixel = 3;

            byte* scan0 = (byte*)imageData.Scan0.ToPointer();
            int stride = imageData.Stride;

            byte unmatchingValue = 0;
            byte matchingValue = 255;
            int toleranceSquared = tolerance * tolerance;

            for (int y = 0; y < imageData.Height; y++)
            {
                byte* row = scan0 + (y * stride);

                for (int x = 0; x < imageData.Width; x++)
                {
                    // Watch out for actual order (BGR)!
                    int bIndex = x * bytesPerPixel;
                    int gIndex = bIndex + 1;
                    int rIndex = bIndex + 2;

                    byte pixelR = row[rIndex];
                    byte pixelG = row[gIndex];
                    byte pixelB = row[bIndex];

                    int diffR = pixelR - searchedR;
                    int diffG = pixelG - searchedG;
                    int diffB = pixelB - searchedB;

                    int distance = diffR * diffR + diffG * diffG + diffB * diffB;

                    row[rIndex] = row[bIndex] = row[gIndex] = distance >
                      toleranceSquared ? unmatchingValue : matchingValue;
                }
            }

            image.UnlockBits(imageData);
        }

        static void DetectColorWithMarshal(Bitmap image, byte searchedR, byte searchedG, int searchedB, int tolerance)
        {
            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width,
              image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            byte[] imageBytes = new byte[Math.Abs(imageData.Stride) * image.Height];
            IntPtr scan0 = imageData.Scan0;

            Marshal.Copy(scan0, imageBytes, 0, imageBytes.Length);

            byte unmatchingValue = 0;
            byte matchingValue = 255;
            int toleranceSquared = tolerance * tolerance;

            for (int i = 0; i < imageBytes.Length; i += 3)
            {
                byte pixelB = imageBytes[i];
                byte pixelR = imageBytes[i + 2];
                byte pixelG = imageBytes[i + 1];

                int diffR = pixelR - searchedR;
                int diffG = pixelG - searchedG;
                int diffB = pixelB - searchedB;

                int distance = diffR * diffR + diffG * diffG + diffB * diffB;

                imageBytes[i] = imageBytes[i + 1] = imageBytes[i + 2] = distance >
                  toleranceSquared ? unmatchingValue : matchingValue;
            }

            Marshal.Copy(imageBytes, 0, scan0, imageBytes.Length);

            image.UnlockBits(imageData);
        }

        static unsafe void DetectColorWithUnsafeParallel(Bitmap image, byte searchedR, byte searchedG, int searchedB, int tolerance)
        {
            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width,
              image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int bytesPerPixel = 3;

            byte* scan0 = (byte*)imageData.Scan0.ToPointer();
            int stride = imageData.Stride;

            byte unmatchingValue = 0;
            byte matchingValue = 255;
            int toleranceSquared = tolerance * tolerance;

            Task[] tasks = new Task[4];
            for (int i = 0; i < tasks.Length; i++)
            {
                int ii = i;
                tasks[i] = Task.Factory.StartNew(() =>
                {
                    int minY = ii < 2 ? 0 : imageData.Height / 2;
                    int maxY = ii < 2 ? imageData.Height / 2 : imageData.Height;

                    int minX = ii % 2 == 0 ? 0 : imageData.Width / 2;
                    int maxX = ii % 2 == 0 ? imageData.Width / 2 : imageData.Width;

                    for (int y = minY; y < maxY; y++)
                    {
                        byte* row = scan0 + (y * stride);

                        for (int x = minX; x < maxX; x++)
                        {
                            int bIndex = x * bytesPerPixel;
                            int gIndex = bIndex + 1;
                            int rIndex = bIndex + 2;

                            byte pixelR = row[rIndex];
                            byte pixelG = row[gIndex];
                            byte pixelB = row[bIndex];

                            int diffR = pixelR - searchedR;
                            int diffG = pixelG - searchedG;
                            int diffB = pixelB - searchedB;

                            int distance = diffR * diffR + diffG * diffG + diffB * diffB;

                            row[rIndex] = row[bIndex] = row[gIndex] = distance >
                                toleranceSquared ? unmatchingValue : matchingValue;
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            image.UnlockBits(imageData);
        }

        static unsafe void DetectColorWithUnsafe(Bitmap image, byte searchedR, byte searchedG, int searchedB)
        {
            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width,
              image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int bytesPerPixel = 3;

            byte* scan0 = (byte*)imageData.Scan0.ToPointer();
            int stride = imageData.Stride;

            byte unmatchingValue = 0;
            byte matchingValue = 255;

            for (int y = 0; y < imageData.Height; y++)
            {
                byte* row = scan0 + (y * stride);

                for (int x = 0; x < imageData.Width; x++)
                {
                    // Watch out for actual order (BGR)!
                    int bIndex = x * bytesPerPixel;
                    int gIndex = bIndex + 1;
                    int rIndex = bIndex + 2;
                    row[rIndex] = row[bIndex] = row[gIndex] =
                        ((row[rIndex] != searchedR)
                        || (row[gIndex] != searchedG)
                        || (row[bIndex] != searchedB)) ? unmatchingValue : matchingValue;
                }
            }

            image.UnlockBits(imageData);
        }

    }
}
