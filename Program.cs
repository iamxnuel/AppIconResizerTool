using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace AppIconTool
{
    class Program
    {
        #region Workflow
        static string outputDir = null;
        static string roundIcon = null;
        static string fgIcon = null;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string fileNameFound = "";
                string mode = "";
                bool nextOneOut = false;
                bool nextOneRound = false;
                bool nextOneFgIcon = false;

                roundIcon = fgIcon = "no";

                foreach (var f in args)
                    if (nextOneOut)
                    {
                        outputDir = f;
                        nextOneOut = false;
                    }
                    else if (nextOneRound)
                    {
                        roundIcon = f;
                        nextOneRound = false;
                    }
                    else if (nextOneFgIcon)
                    {
                        fgIcon = f;
                        nextOneFgIcon = false;
                    }
                    else if (File.Exists(f))
                        fileNameFound = f;
                    else if (f.ToLower() == "--logo")
                        mode = "Logo";
                    else if (f.ToLower() == "--icon")
                        mode = "AppIcon";
                    else if (f.ToLower() == "--out")
                        nextOneOut = true;
                    else if (f.ToLower() == "--round")
                        nextOneRound = true;
                    else if (f.ToLower() == "--fgicon")
                        nextOneFgIcon = true;


                if (!string.IsNullOrEmpty(fileNameFound))
                    if (!string.IsNullOrWhiteSpace(mode))
                    {
                        var img = LoadImage(fileNameFound);

                        if (img != null)
                            if (mode == "AppIcon")
                            {
                                ProcessAppIcon(img);
                                return;
                            }
                            else if (mode == "Logo")
                            {
                                ProcessAppLogo(img);
                                return;
                            }
                    }
                    else
                    {
                        Start(fileNameFound);
                        return;
                    }
            }

            while (true)
            {
                outputDir = fgIcon = roundIcon = null;
                Console.WriteLine("Enter Source Image File:");
                Console.Write("> ");

                var f = Console.ReadLine();

                if (f.ToLower() == "exit" || f.ToLower() == "cancel")
                    return;

                Start(f);
            }
        }

        static void Start(string fileName)
        {
            var srcImage = LoadImage(fileName);

            if (srcImage == null)
                return;

            Console.WriteLine("OK Image loaded ({0}x{1})! What you want does the Image contain? (AppIcon|Logo)", srcImage.Width, srcImage.Height);

        newMode:
            Console.Write("> ");
            var mode = Console.ReadLine();

            if (mode == "AppIcon")
                ProcessAppIcon(srcImage);
            else if (mode == "Logo")
                ProcessAppLogo(srcImage);
            else if (mode.ToLower() == "exit" || mode.ToLower() == "cancel")
            { }
            else
            {
                Console.WriteLine("Unknown Mode! (AppIcon|Logo)");
                goto newMode;
            }

            srcImage.Dispose();
        }
        static void ProcessAppIcon(Image srcImage)
        {
            if (!CheckIconUsable(srcImage))
                return;

            var outdir = DefineOutputDir();

            if (string.IsNullOrWhiteSpace(outdir))
                return;

            Console.WriteLine("Okay, ready for creation process.");
            Console.WriteLine();

            //Generate iOS Icons
            Console.Write("Generating iOS Icons...");

            Stopwatch w = new Stopwatch();
            w.Start();
            var outdir_ios = Path.Combine(outdir, "iOS");

            if (Directory.Exists(outdir_ios))
                Directory.Delete(outdir_ios, true);

            Directory.CreateDirectory(outdir_ios);

            foreach (var size in AppIconSizes_iOS)
                using (var resImage = ResizeImage(srcImage, size, size))
                    resImage.Save($"{Path.Combine(outdir_ios, size + ".png")}", ImageFormat.Png);

            w.Stop();
            Done(w.ElapsedMilliseconds, AppIconSizes_iOS.Length);

            Console.Write("Generating iTunes Artwork...");
            w.Restart();

            foreach (var size in iTunesArtworkSizes)
                using (var resImage = ResizeImage(srcImage, size.Value, size.Value))
                    resImage.Save($"{Path.Combine(outdir, size.Key + ".png")}", ImageFormat.Png);

            w.Stop();
            Done(w.ElapsedMilliseconds, iTunesArtworkSizes.Count);

            Console.Write("Generating Android Icons...");
            w.Restart();

            var outdir_android = Path.Combine(outdir, "Android");

            if (Directory.Exists(outdir_android))
                Directory.Delete(outdir_android, true);

            Directory.CreateDirectory(outdir_android);

            foreach (var size in AppIconSizes_Android)
                using (var resImage = ResizeImage(srcImage, size.Value, size.Value))
                {
                    var subDir = Path.Combine(outdir_android, $"mipmap-{size.Key}");

                    if (!Directory.Exists(subDir))
                        Directory.CreateDirectory(subDir);

                    resImage.Save($"{Path.Combine(subDir, "ic_launcher.png")}", ImageFormat.Png);
                }
            w.Stop();
            Done(w.ElapsedMilliseconds, AppIconSizes_Android.Count);

        roundAgain:

            string roundFile = roundIcon;

            if (string.IsNullOrWhiteSpace(roundFile))
            {
                Console.WriteLine("Do you have an Android Round Launcher Icon? (No|<FileName>)");

                Console.Write("> ");
                roundFile = Console.ReadLine();
            }

            if (roundFile.ToLower() != "no")
            {
                var roundImg = LoadImage(roundFile);

                if (roundImg == null)
                    goto roundAgain;

                if (!CheckIconUsable(roundImg))
                    goto roundAgain;

                Console.Write("Generating Android Round Icons...");
                w.Restart();

                foreach (var size in AppIconSizes_Android)
                    using (var resImage = ResizeImage(srcImage, size.Value, size.Value))
                    {
                        var subDir = Path.Combine(outdir_android, $"mipmap-{size.Key}");

                        if (!Directory.Exists(subDir))
                            Directory.CreateDirectory(subDir);

                        resImage.Save($"{Path.Combine(subDir, "ic_launcher_round.png")}", ImageFormat.Png);
                    }
                w.Stop();
                Done(w.ElapsedMilliseconds, AppIconSizes_Android.Count);
            }

        fgAgain:

            string fgFile = fgIcon;

            if (string.IsNullOrWhiteSpace(fgFile))
            {
                Console.WriteLine("Do you have an Android Foreground Launcher Icon? (No|<FileName>)");
                Console.Write("> ");

               fgFile = Console.ReadLine();
            }

            if (roundFile.ToLower() != "no")
            {
                var fgImg = LoadImage(fgFile);

                if (fgImg == null)
                    goto fgAgain;

                if (!CheckIconUsable(fgImg))
                    goto fgAgain;

                Console.Write("Generating Android Foreground Icons...");
                w.Restart();

                foreach (var size in AppIconSizes_AndroidForeground)
                    using (var resImage = ResizeImage(srcImage, size.Value, size.Value))
                    {
                        var subDir = Path.Combine(outdir_android, $"mipmap-{size.Key}");

                        if (!Directory.Exists(subDir))
                            Directory.CreateDirectory(subDir);

                        resImage.Save($"{Path.Combine(subDir, "ic_launcher_foreground.png")}", ImageFormat.Png);
                    }
                w.Stop();
                Done(w.ElapsedMilliseconds, AppIconSizes_AndroidForeground.Count);
            }

            Console.WriteLine("Creation Process finished successfully.");
        }
        static void ProcessAppLogo(Image srcImage)
        {
            if (!CheckLogoUsable(srcImage))
                return;

            var outdir = DefineOutputDir();

            if (string.IsNullOrWhiteSpace(outdir))
                return;

            Console.WriteLine("Okay, ready for creation process.");
            Console.WriteLine();

            //Generate iOS Icons
            Console.Write("Generating iOS Logo...");

            Stopwatch w = new Stopwatch();
            w.Start();
            var outdir_ios = Path.Combine(outdir, "iOS");

            if (Directory.Exists(outdir_ios))
                Directory.Delete(outdir_ios, true);

            Directory.CreateDirectory(outdir_ios);

            foreach (var size in LogoIconSizes_iOS)
                using (var resImage = ResizeImage(srcImage, size.Value, ScaleDown(srcImage.Width, srcImage.Height, size.Value)))
                    resImage.Save($"{Path.Combine(outdir_ios, size.Key + ".png")}", ImageFormat.Png);

            w.Stop();
            Done(w.ElapsedMilliseconds, LogoIconSizes_iOS.Count);

            Console.Write("Generating Android Logo...");
            w.Restart();

            var outdir_android = Path.Combine(outdir, "Android");

            if (Directory.Exists(outdir_android))
                Directory.Delete(outdir_android, true);

            Directory.CreateDirectory(outdir_android);

            foreach (var size in LogoIconSizes_Android)
                using (var resImage = ResizeImage(srcImage, size.Value, ScaleDown(srcImage.Width, srcImage.Height, size.Value)))
                {
                    var subDir = Path.Combine(outdir_android, $"mipmap-{size.Key}");

                    if (!Directory.Exists(subDir))
                        Directory.CreateDirectory(subDir);

                    resImage.Save($"{Path.Combine(subDir, "ic_logo.png")}", ImageFormat.Png);
                }
            w.Stop();
            Done(w.ElapsedMilliseconds, LogoIconSizes_Android.Count);
        }
        #endregion
        #region Helpers
        static void Done(float ms, int files)
        {
            Console.WriteLine("[DONE] ({1} Files created in {0:0} ms)", ms, files);
        }

        public static bool CheckLogoUsable(Image srcImage)
        {
            if (srcImage.Width < 1500)
            {
                Console.WriteLine("Image cannot be used, its width smaller than 1500px, minimum required width is 1500px!");
                return false;
            }

            return true;
        }
        public static bool CheckIconUsable(Image srcImage)
        {
            if (srcImage.Width != srcImage.Height)
            {
                Console.WriteLine("Image cannot be used, it has not the same height as width, so it's not quadratic. Requirement!");
                return false;
            }

            if (srcImage.Width < 1024)
            {
                Console.WriteLine("Image cannot be used, its smaller than 1024px, minimum required size is 1024x1024!");
                return false;
            }

            return true;
        }

        public static string DefineOutputDir()
        {
            string outDir = outputDir;

            if (string.IsNullOrWhiteSpace(outDir))
            {
                Console.WriteLine("Where you want to output the result? Define the outdir:");
                Console.Write("> ");

                outDir = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(outDir) || outDir.ToLower() == "cancel")
                    return null;
            }

            try
            {
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot use this Directory as outdir. {0}: {1}", ex.GetType().FullName, ex.Message);
                return null;
            }

            return outDir;
        }
        public static int ScaleDown(int originalWidth, int originalHeight, int newWidth)
        {
            return (int)(newWidth / ((float)originalWidth / (float)originalHeight));
        }
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        public static Image LoadImage(string fileName)
        {
            Console.WriteLine("Checking File \"{0}\"", fileName);

            if (!File.Exists(fileName))
            {
                Console.WriteLine("File not found!");
                return null;
            }

            Image srcImage;

            try
            {
                srcImage = Image.FromFile(fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot use File! {0}: {1}", ex.GetType().FullName, ex.Message);
                return null;
            }

            if (srcImage == null)
            {
                Console.WriteLine("Cannot use File! Image is null!");
                return null;
            }

            return srcImage;
        }
        #endregion
        #region Size Definitions
        public static readonly int[] AppIconSizes_iOS = new int[]
        {
            16,
            20,
            29,
            32,
            40,
            48,
            50,
            55,
            57,
            58,
            60,
            64,
            72,
            76,
            80,
            87,
            88,
            100,
            114,
            120,
            128,
            144,
            152,
            167,
            172,
            180,
            196,
            216,
            256,
            512,
            1024
        };
        public static readonly Dictionary<string, int> iTunesArtworkSizes = new Dictionary<string, int>()
        {
            { "iTunesArtwork", 512 },
            { "iTunesArtwork@2x", 1024 }
        };
        public static readonly Dictionary<string, int> AppIconSizes_Android = new Dictionary<string, int>()
        {
            { "mdpi", 48 },
            { "hdpi", 72 },
            { "xhdpi", 96 },
            { "xxdpi", 144 },
            { "xxxdpi", 192 },
        };
        public static readonly Dictionary<string, int> AppIconSizes_AndroidForeground = new Dictionary<string, int>()
        {
            { "mdpi", 108 },
            { "hdpi", 162 },
            { "xhdpi", 216 },
            { "xxdpi", 324 },
            { "xxxdpi", 432 },
        };
        public static readonly Dictionary<string, int> LogoIconSizes_iOS = new Dictionary<string, int>()
        {
            { "Logo@1", 400 },
            { "Logo@2", 800 },
            { "Logo@3", 1200 }
        };
        public static readonly Dictionary<string, int> LogoIconSizes_Android = new Dictionary<string, int>()
        {
            { "mdpi", 350 },
            { "hdpi", 524 },
            { "xhdpi", 700 },
            { "xxdpi", 1049 },
            { "xxxdpi", 1399 },
        };
        #endregion
    }
}
