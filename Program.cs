using Emgu.CV;

namespace Webcam
{
    class Program
    {
        static void Main(string[] args)
        {
            // Install-Package Emgu.CV 
            // Install-Package Emgu.CV.runtime.windows
            var filename = "webcam.jpg";
            if (args.Length > 0) filename = args[0];
            using var capture = new VideoCapture(0, VideoCapture.API.DShow); //create a camera capture
            var image = capture.QueryFrame(); //take a picture
            image.Save(filename);
        }
    }
}
