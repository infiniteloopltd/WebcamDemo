using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using DirectShowLib;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FFMediaToolkit;
using FFMediaToolkit.Encoding;
using FFMediaToolkit.Graphics;

namespace Webcam
{
    class Program
    {
        private static VideoCapture _captureDevice;
        private static MediaOutput _videoOutput;

        /// <summary>
        /// Record a video for 5 seconds and store it as c:\temp\example.mp4
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Install-Package Emgu.CV 
            // Install-Package Emgu.CV.runtime.windows
            // Install-Package FFMediaToolkit
            // Install-Package DirectShowLib 
            
            var camIndex = SelectCameraIndex();
            _captureDevice = new VideoCapture(camIndex, VideoCapture.API.DShow)
                {FlipVertical = true};
            _captureDevice.ImageGrabbed += CaptureDeviceImageGrabbed;
            var settings = new VideoEncoderSettings(width: 
                _captureDevice.Width
                , height: _captureDevice.Height
                , framerate: 15
                , codec: VideoCodec.H264)
            {
                EncoderPreset = EncoderPreset.Fast,
                CRF = 17 // Constant Rate Factor
            };
            // Download from https://github.com/BtbN/FFmpeg-Builds/releases
            FFmpegLoader.FFmpegPath =
                @"C:\Users\fiach\source\repos\Webcam\FFmpeg\";
            _videoOutput = MediaBuilder.CreateContainer(@"c:\temp\example.mp4").WithVideo(settings).Create();
            _captureDevice.Start();
            Thread.Sleep(TimeSpan.FromSeconds(5));
            _captureDevice.Stop();
            _captureDevice.Dispose();
            _videoOutput.Dispose();
        }

        /// <summary>
        /// If there are more than one webcam attached, then ask the user to select
        /// </summary>
        /// <returns></returns>
        private static int SelectCameraIndex()
        {
            var cameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            if (cameras.Length == 1) return 0;
            foreach (var (camera, index) in WithIndex(cameras))
            {
                Console.WriteLine($"{index}:{camera.Name}");
            }
            Console.WriteLine("Select a camera from the list above:");
            var camIndex = Convert.ToInt32(Console.ReadLine());
            return camIndex;
        }
        
        /// <summary>
        /// Not used, but a simple method to take a picture from the video capture device
        /// and save to disk
        /// </summary>
        /// <param name="filename"></param>
        static void TakePhoto(string filename)
        {
            using var capture = new VideoCapture(0, VideoCapture.API.DShow);
            var image = capture.QueryFrame(); //take a picture
            image.Save(filename);
        }
        
        /// <summary>
        /// When an Image is grabbed from the video capture device, then add it to the
        /// video stream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CaptureDeviceImageGrabbed(object sender, System.EventArgs e)
        {
            var frame = new Mat();
            _captureDevice.Retrieve(frame);
            var buffer = new VectorOfByte();
            var input = frame.ToImage<Bgr, byte>();
            CvInvoke.Imencode(".bmp", input, buffer);
            var bitmapData = buffer.ToArray();
            // 'Pixel buffer size doesn't match size required by this image format.'
            // Remove 54 byte header
            var headerLessData = RedBlueSwap(bitmapData.Skip(54).ToArray());
            var imageData = ImageData.FromArray(headerLessData, ImagePixelFormat.Rgb24, frame.Size);
            _videoOutput.Video.AddFrame(imageData);
        }

        /// <summary>
        /// The Bitmap file format is recorded backwards, so red and green are swapped
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static byte[] RedBlueSwap(byte[] input)
        {
            var output = new byte[input.Length];
            for (var i = 0; i < input.Length - 3; i += 3)
            {
                var r = input[i];
                var g = input[i + 1];
                var b = input[i + 2];
                output[i] = b;
                output[i + 1] = g;
                output[i + 2] = r;
            }
            return output;
        }

        /// <summary>
        /// Handy code from https://thomaslevesque.com/2019/11/18/using-foreach-with-index-in-c/
        /// Gives access to an indexer in a for each loop
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        private static IEnumerable<(T item, int index)> WithIndex<T>(IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }
    }
}
