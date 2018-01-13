// Weiqing Chen
// kevincwq@gmail.com
// 2018-01-12

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
        // const string ADBPATH = @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe";
        const string ADBPATH = @".\adb.exe";
        const byte TargetWR = 245, TargetWG = 245, TargetWB = 245, TargetWMaxWidthMin = 35;
        const byte PersonR = 49, PersonG = 47, PersonB = 78, PersonTolerance = 18;
        const int PersonSearchWith = 300, PersonSearchHeight = 300;

        // xiaomi, note3
        const double InvSpeedLeft = 203.84436, OffsetLeft = 39, InvSpeedRight = 202.823440, OffsetRight = 41.38;
        const double Inch2Cm = 2.54, dpiX = 391.885, dpiY = 381.0;

        // adb shell dumpsys display
        // 1080 x 1920, 60.0 fps, density 3.0, 391.885 x 381.0 dpi

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
                int firstx = 0, lastx = 0;
                for (int x = 0; x < imageData.Width - TargetWMaxWidthMin; x++)
                {
                    // Watch out for actual order (BGR)!
                    int bIndex = x * bytesPerPixel;
                    if (row[bIndex + 2] == searchedR && row[bIndex + 1] == searchedG && row[bIndex] == searchedB)
                    {
                        row[bIndex + 2] = row[bIndex + 1] = row[bIndex] = 0;
                        firstx = x;
                        lastx = x + 1;
                        while (lastx < imageData.Width)
                        {
                            bIndex = lastx * bytesPerPixel;
                            if (row[bIndex + 2] != searchedR || row[bIndex + 1] != searchedG && row[bIndex] != searchedB)
                            {
                                break;
                            }
                            row[bIndex + 2] = row[bIndex + 1] = row[bIndex] = 0;
                            lastx++;
                        }
                        if (lastx - firstx < TargetWMaxWidthMin)
                        {
                            x = lastx;
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                var width = lastx - firstx;
                if (width > TargetWMaxWidthMin && width > count)
                {
                    count = width;
                    centerX = (lastx + firstx - 1) / 2;
                    centerY = y;
                }
                if (count > TargetWMaxWidthMin && width < count)
                {
                    break;
                }
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
                    int diffR = row[bIndex + 2] - searchedR;
                    int diffG = row[bIndex + 1] - searchedG;
                    int diffB = row[bIndex] - searchedB;

                    int distance = diffR * diffR + diffG * diffG + diffB * diffB;
                    if (distance < toleranceSquared)
                    {
                        row[bIndex + 2] = row[bIndex + 1] = row[bIndex] = 0;
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

        static double MeasureDistance(string img, out bool direction)
        {
            direction = false;
            using (var bitmap = new Bitmap(img))
            {
                float tcx, tcy, pcx, pcy;
                int count1, count2;
                FindTargetWWithUnsafe(bitmap, TargetWR, TargetWG, TargetWB, out tcx, out tcy, out count1);
                if (tcx < 10 || tcy < 10 || count1 < TargetWMaxWidthMin || count1 > 39)
                {
                    Debug.WriteLine($"-1:{img}");
                    return -1;
                }
                pcx = bitmap.Width + 40 - tcx;
                pcy = bitmap.Height + 40 - tcy;

                FindPersonAroundWithUnsafe(bitmap, PersonR, PersonG, PersonB, PersonTolerance, PersonSearchWith, PersonSearchHeight, ref pcx, ref pcy, out count2);
                if (count2 < 2000)
                {
                    Debug.WriteLine($"-2:{img}");
                    return -2;
                }

                tcy += 2;
                pcy += 7;

                using (var g = Graphics.FromImage(bitmap))
                {
                    g.DrawString($"{count1} {count2}", new Font("Arial", 20), Brushes.Red, new PointF(100, 100));
                    g.FillEllipse(Brushes.Red, tcx - 2, tcy - 2, 5, 5);
                    g.FillEllipse(Brushes.Red, pcx - 2, pcy - 2, 5, 5);
                }
                bitmap.Save(Path.ChangeExtension(img, ".p.png"), ImageFormat.Png);
                var dx = (pcx - tcx) / dpiX * Inch2Cm;
                var dy = (pcy - tcy) / dpiY * Inch2Cm;
                direction = dx < 0;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        static void Main(string[] args)
        {
            var screenshotspath = Path.Combine(Environment.CurrentDirectory, "screenshots");

            var cmd1 = "shell /system/bin/screencap -p /sdcard/screenshot.png";
            var cmd2Tempate = "pull /sdcard/screenshot.png";
            var cmd3Tempate = "shell input swipe";
            var cmdmodel = "shell getprop ro.product.model";

            var random = new Random(Environment.TickCount);

            var test = false;
            var start = DateTime.Now;
            if (!test)
            {
                var step = 0;
                long[] tss = new long[5];
                while (true)
                {
                    Console.Title = $" PrecisionJump {(DateTime.Now - start).TotalMinutes:F2}min";
                    var model = CmdAdb(cmdmodel);
                    if (string.IsNullOrWhiteSpace(model) || model.Contains("no devices"))
                    {
                        Console.WriteLine("No device connected, stopped");
                        break;
                    }

                    tss[0] = DateTime.Now.Ticks;
                    CmdAdb(cmd1);

                    tss[1] = DateTime.Now.Ticks;
                    var img = Path.Combine(screenshotspath, DateTime.Now.ToString("yyMMddHHmmssfff") + ".png");
                    CmdAdb($"{cmd2Tempate} {img}");

                    tss[2] = DateTime.Now.Ticks;
                    bool direction;
                    var distance = MeasureDistance(img, out direction);

                    tss[3] = DateTime.Now.Ticks;
                    if (distance > 0)
                    {
                        step++;
                        var time = direction ? (int)(distance * InvSpeedRight + OffsetRight) : (int)(distance * InvSpeedLeft + OffsetLeft);

                        var startx = 800 - random.Next(100, 300);
                        var starty = 1800 - random.Next(0, 400);
                        var endx = startx + random.Next(20, 130);
                        var endy = starty + random.Next(50, 100) - 50;

                        CmdAdb($"{cmd3Tempate} {startx} {starty} {endx} {endy} {time}");
                        tss[4] = DateTime.Now.Ticks;

                        Console.WriteLine($"Step:{step}\tDis:{distance:F3}\tTime:{time}\tCMD1:{(tss[1] - tss[0]) / 1E7:F3}\tCMD2:{(tss[2] - tss[1]) / 1E7:F3}\tDIS:{(tss[3] - tss[2]) / 1E7:F3}\tCMD3:{(tss[4] - tss[3]) / 1E7:F3}");
                        Thread.Sleep(700 + random.Next(300, 800));
                    }
                    else
                    {
                        Console.WriteLine($"Step:{step}\tCann't measure the distance");
                        if (distance == -1)
                        {
                            File.Delete(img);
                            Thread.Sleep(2000);
                        }
                        else if (distance == -2)
                        {
                            File.Move(img, Path.ChangeExtension(img, ".2.png"));
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            // Thread.Sleep(100);
                        }
                        continue;
                    }
                }
            }
            else
            {
                foreach (var file in Directory.EnumerateFiles(screenshotspath).ToList())
                {
                    if (!file.EndsWith(".p.png"))
                    //if (file.EndsWith("180113012303257.png"))
                    {
                        bool direction;
                        if (MeasureDistance(file, out direction) < 0)
                            Console.WriteLine(file);
                    }
                }
            }
            Console.ReadKey();
        }
    }
}
