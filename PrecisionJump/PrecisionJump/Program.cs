using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace PrecisionJump
{
    class Program
    {
        const string ADBPATH = @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe";
        const byte TargetWR = 245, TargetWG = 245, TargetWB = 245;
        const byte PersonR = 49, PersonG = 47, PersonB = 78, PersonTolerance = 20;
        const int PersonSearchWith = 300, PersonSearchHeight = 300;
        const double InverseSpeed = 1.4;

        static string CmdAdb(string arguments)
        {
            string ret = string.Empty;
            using (Process p = new Process())
            {
                p.StartInfo.FileName = ADBPATH;
                p.StartInfo.Arguments = arguments;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;   //重定向标准输入   
                p.StartInfo.RedirectStandardOutput = true;  //重定向标准输出   
                p.StartInfo.RedirectStandardError = true;   //重定向错误输出   
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                ret = p.StandardOutput.ReadToEnd();
                p.Close();
            }
            return ret;
        }

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

        //static void DetectColorWithMarshal(Bitmap image, byte searchedR, byte searchedG, int searchedB, int tolerance)
        //{
        //    BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width,
        //      image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        //    byte[] imageBytes = new byte[Math.Abs(imageData.Stride) * image.Height];
        //    IntPtr scan0 = imageData.Scan0;

        //    Marshal.Copy(scan0, imageBytes, 0, imageBytes.Length);

        //    byte unmatchingValue = 0;
        //    byte matchingValue = 255;
        //    int toleranceSquared = tolerance * tolerance;

        //    for (int i = 0; i < imageBytes.Length; i += 3)
        //    {
        //        byte pixelB = imageBytes[i];
        //        byte pixelR = imageBytes[i + 2];
        //        byte pixelG = imageBytes[i + 1];

        //        int diffR = pixelR - searchedR;
        //        int diffG = pixelG - searchedG;
        //        int diffB = pixelB - searchedB;

        //        int distance = diffR * diffR + diffG * diffG + diffB * diffB;

        //        imageBytes[i] = imageBytes[i + 1] = imageBytes[i + 2] = distance >
        //          toleranceSquared ? unmatchingValue : matchingValue;
        //    }

        //    Marshal.Copy(imageBytes, 0, scan0, imageBytes.Length);

        //    image.UnlockBits(imageData);
        //}

        //static unsafe void DetectColorWithUnsafeParallel(Bitmap image, byte searchedR, byte searchedG, int searchedB, int tolerance)
        //{
        //    BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width,
        //      image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        //    int bytesPerPixel = 3;

        //    byte* scan0 = (byte*)imageData.Scan0.ToPointer();
        //    int stride = imageData.Stride;

        //    byte unmatchingValue = 0;
        //    byte matchingValue = 255;
        //    int toleranceSquared = tolerance * tolerance;

        //    Task[] tasks = new Task[4];
        //    for (int i = 0; i < tasks.Length; i++)
        //    {
        //        int ii = i;
        //        tasks[i] = Task.Factory.StartNew(() =>
        //        {
        //            int minY = ii < 2 ? 0 : imageData.Height / 2;
        //            int maxY = ii < 2 ? imageData.Height / 2 : imageData.Height;

        //            int minX = ii % 2 == 0 ? 0 : imageData.Width / 2;
        //            int maxX = ii % 2 == 0 ? imageData.Width / 2 : imageData.Width;

        //            for (int y = minY; y < maxY; y++)
        //            {
        //                byte* row = scan0 + (y * stride);

        //                for (int x = minX; x < maxX; x++)
        //                {
        //                    int bIndex = x * bytesPerPixel;
        //                    int gIndex = bIndex + 1;
        //                    int rIndex = bIndex + 2;

        //                    byte pixelR = row[rIndex];
        //                    byte pixelG = row[gIndex];
        //                    byte pixelB = row[bIndex];

        //                    int diffR = pixelR - searchedR;
        //                    int diffG = pixelG - searchedG;
        //                    int diffB = pixelB - searchedB;

        //                    int distance = diffR * diffR + diffG * diffG + diffB * diffB;

        //                    row[rIndex] = row[bIndex] = row[gIndex] = distance >
        //                        toleranceSquared ? unmatchingValue : matchingValue;
        //                }
        //            }
        //        });
        //    }

        //    Task.WaitAll(tasks);

        //    image.UnlockBits(imageData);
        //}

        //static unsafe void DetectColorWithUnsafe(Bitmap image, byte searchedR, byte searchedG, int searchedB)
        //{
        //    BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width,
        //      image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        //    int bytesPerPixel = 3;

        //    byte* scan0 = (byte*)imageData.Scan0.ToPointer();
        //    int stride = imageData.Stride;

        //    byte unmatchingValue = 0;
        //    byte matchingValue = 255;

        //    for (int y = 0; y < imageData.Height; y++)
        //    {
        //        byte* row = scan0 + (y * stride);

        //        for (int x = 0; x < imageData.Width; x++)
        //        {
        //            // Watch out for actual order (BGR)!
        //            int bIndex = x * bytesPerPixel;
        //            int gIndex = bIndex + 1;
        //            int rIndex = bIndex + 2;
        //            row[rIndex] = row[bIndex] = row[gIndex] =
        //                ((row[rIndex] != searchedR)
        //                || (row[gIndex] != searchedG)
        //                || (row[bIndex] != searchedB)) ? unmatchingValue : matchingValue;
        //        }
        //    }

        //    image.UnlockBits(imageData);
        //}

        static unsafe void FindTargetWWithUnsafe(Bitmap image, byte searchedR, byte searchedG, int searchedB, out float centerX, out float centerY, out int count)
        {
            centerX = centerY = 0;
            count = 0;

            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int bytesPerPixel = 3;

            byte* scan0 = (byte*)imageData.Scan0.ToPointer();
            int stride = imageData.Stride;

            var ymin = imageData.Height / 3;
            var ymax = imageData.Height / 2;

            for (int y = ymin; y < ymax; y++)
            {
                byte* row = scan0 + (y * stride);

                for (int x = 0; x < imageData.Width; x++)
                {
                    // Watch out for actual order (BGR)!
                    int bIndex = x * bytesPerPixel;
                    int gIndex = bIndex + 1;
                    int rIndex = bIndex + 2;
                    if (row[rIndex] == searchedR && row[gIndex] == searchedG && row[bIndex] == searchedB)
                    {
                        if (count > 100 && (Math.Abs(centerX / count - x) > 50 || Math.Abs(centerY / count - y) > 50))
                        {
                            continue;
                        }
                        centerX += x;
                        centerY += y;
                        count++;
                    }
                }
            }
            if (count > 0)
            {
                centerX /= count;
                centerY /= count;
            }

            image.UnlockBits(imageData);
        }

        static unsafe void FindPersonAroundWithUnsafe(Bitmap image, byte searchedR, byte searchedG, byte searchedB, byte tolerance, int searchWith, int searchHeight, ref float pcx, ref float pcy, out int count)
        {
            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int bytesPerPixel = 3;

            byte* scan0 = (byte*)imageData.Scan0.ToPointer();
            int stride = imageData.Stride;
            int toleranceSquared = tolerance * tolerance;

            var minx = (int)pcx - searchWith / 2;
            var maxx = (int)pcx + searchWith / 2;
            var miny = (int)pcy - searchHeight / 2;
            var maxy = (int)pcy + searchHeight / 2;

            int x1 = maxx + 1, x2 = minx - 1, y1 = maxy + 1, y2 = miny - 1;
            count = 0;
            for (int y = miny; y < maxy; y++)
            {
                byte* row = scan0 + (y * stride);

                for (int x = minx; x < maxx; x++)
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
                    if (distance < toleranceSquared)
                    {
                        count++;
                        if (x < x1)
                        {
                            x1 = x;
                            y1 = y;
                        }
                        if (x > x2)
                        {
                            x2 = x;
                            y2 = y;
                        }
                    }
                }
            }
            image.UnlockBits(imageData);

            pcx = (x1 + x2) / 2.0f;
            pcy = (y1 + y2) / 2.0f;
        }

        static double MeasureDistance(string img)
        {
            using (var bitmap = new Bitmap(img))
            {
                float tcx, tcy, pcx, pcy;
                int count1, count2;
                FindTargetWWithUnsafe(bitmap, TargetWR, TargetWG, TargetWB, out tcx, out tcy, out count1);
                if (tcx < 10 || tcy < 10 || count1 < 600 || count1 > 900)
                {
                    return -1;
                }
                pcx = bitmap.Width + 40 - tcx;
                pcy = bitmap.Height + 40 - tcy;

                FindPersonAroundWithUnsafe(bitmap, PersonR, PersonG, PersonB, PersonTolerance, PersonSearchWith, PersonSearchHeight, ref pcx, ref pcy, out count2);
                if (count2 < 2000)
                {
                    return -1;
                }

                //using (var g = Graphics.FromImage(bitmap))
                //{
                //    g.DrawString($"{count1} {count2}", new Font("Arial", 20), Brushes.Red, new PointF(100, 100));
                //    g.FillEllipse(Brushes.Red, tcx - 2, tcy - 2, 5, 5);
                //    g.FillEllipse(Brushes.Red, pcx - 2, pcy - 2, 5, 5);
                //}
                //bitmap.Save(Path.ChangeExtension(img, ".p.png"), ImageFormat.Png);
                var dx = pcx - tcx;
                var dy = pcy - tcy;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }


        static void Main(string[] args)
        {
            var screenshotspath = Path.Combine(Environment.CurrentDirectory, "screenshots");

            var cmd1 = "shell /system/bin/screencap -p /sdcard/screenshot.png";
            var cmd2Tempate = "pull /sdcard/screenshot.png ";
            var cmd3Tempate = "shell input swipe 250 250 300 300 ";
            var cmdmodel = "shell getprop ro.product.model";

            var test = false;

            if (!test)
            {
                var step = 0;
                while (true)
                {
                    var model = CmdAdb(cmdmodel);
                    if (string.IsNullOrWhiteSpace(model) || model.Contains("no devices"))
                    {
                        Console.WriteLine("No device connected, stopped");
                        break;
                    }

                    CmdAdb(cmd1);
                    var img = Path.Combine(screenshotspath, DateTime.Now.ToString("yyMMddHHmmssfff") + ".png");
                    CmdAdb($"{cmd2Tempate}{img}");

                    var distance = MeasureDistance(img);
                    if (distance > 0)
                    {
                        var time = (int)(distance * InverseSpeed);
                        CmdAdb($"{cmd3Tempate}{time}");
                        Console.WriteLine($"Step:{step}\tDis:{distance}\tTime:{time}");
                        // Thread.Sleep(time);
                    }
                    else
                    {
                        File.Delete(img);
                        Console.WriteLine($"Step:{step}\tCann't measure the distance");
                        //Thread.Sleep(2000);
                        continue;
                    }
                }
            }
            else
            {
                foreach (var file in Directory.EnumerateFiles(screenshotspath).ToList())
                {
                    if (!file.EndsWith(".p.png"))
                    {
                        if (MeasureDistance(file) < 0)
                            Console.WriteLine(file);
                    }
                }
            }
            Console.ReadKey();
        }
    }
}
