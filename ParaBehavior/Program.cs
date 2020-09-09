using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using NationalInstruments.Vision.Acquisition.Imaq;
using NationalInstruments.Vision;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO.Ports;
using System.IO;
using System.Threading.Tasks.Dataflow;


namespace ParaBehavior
{
    class Program
    {
        static void Main(string[] args)
        {
            SerialPort pyboard = new SerialPort("COM3", 115200);
            pyboard.Open();
            var options = new DataflowBlockOptions();
            options.BoundedCapacity = 10;
            MCvScalar gray = new MCvScalar(128, 128, 128);
            string camera_id = "img0"; //this is the ID of the NI-IMAQ board in NI MAX. 
            var _session = new ImaqSession(camera_id);
            String camerawindow = "ParameciumImage";
            CvInvoke.NamedWindow(camerawindow);
            int frameWidth = 1280;
            int frameHeight = 1024;
            uint bufferCount = 3;
            uint buff_out = 0;
            int numchannels = 1;
            System.Drawing.Size framesize = new System.Drawing.Size(frameWidth, frameHeight);
            Mat cvimage = new Mat(framesize, Emgu.CV.CvEnum.DepthType.Cv8U, numchannels);
            Mat modeimage = new Mat(framesize, Emgu.CV.CvEnum.DepthType.Cv8U, numchannels);
            byte[,] data_2D = new byte[frameHeight, frameWidth];
            byte[,] imagemode = new byte[frameHeight, frameWidth];
            ImaqBuffer image = null;
            List<byte[,]> imglist = new List<byte[,]>();
            ImaqBufferCollection buffcollection = _session.CreateBufferCollection((int)bufferCount, ImaqBufferCollectionType.VisionImage);
            _session.RingSetup(buffcollection, 0, false);
            _session.Acquisition.AcquireAsync();

            imglist = GetImageList(_session, 5000, 400);
            imagemode = FindMode(imglist);
            modeimage.SetTo(imagemode);
            imglist.Clear(); 
            CvInvoke.Imshow(camerawindow, modeimage);
            CvInvoke.WaitKey(0);

            while (true)
            {
                image = _session.Acquisition.Extract(j, out buff_out);
                data_2D = image.ToPixelArray().U8;
                cvimage.SetTo(data_2D);
                CvInvoke.Imshow(camerawindow, modeimage);
                CvInvoke.WaitKey(1);
            }
             

        }
        static List<byte[,]> GetImageList(ImaqSession ses, int numframes, int mod)
        {

            int frheight = 1024;
            int frwidth = 1280;
            List<byte[,]> avg_imglist = new List<byte[,]>();
            byte[,] avg_data_2D = new byte[frheight, frwidth];
            uint buff_out = 0;
            ImaqBuffer image = null;
            for (uint i = 0; i < numframes; i++)
            {
                image = ses.Acquisition.Extract((uint)0, out buff_out);
                avg_data_2D = image.ToPixelArray().U8;
                if (i % mod == 0)
                {
                    byte[,] avgimage_2D = new byte[frheight, frwidth];
                    Buffer.BlockCopy(avg_data_2D, 0, avgimage_2D, 0, avg_data_2D.Length);
                    avg_imglist.Add(avgimage_2D);
                }
            }
            return avg_imglist;
        }

        static byte[,] FindMode(List<byte[,]> backgroundimages)
        {
            byte[,] image = backgroundimages[0];
            byte[,] output = new byte[image.GetLength(0), image.GetLength(1)];
            uint[] pixelarray = new uint[backgroundimages.Count];
            for (int rowind = 0; rowind < image.GetLength(0); rowind++)
            {
                for (int colind = 0; colind < image.GetLength(1); colind++)
                {
                    int background_number = 0;
                    foreach (byte[,] background in backgroundimages)
                    {
                        if (rowind == colind && rowind % 100 == 0)
                        {
                            Console.WriteLine(background[rowind, colind]); // This gives pixel vals of a line down the diagonal of the images for all images in modelist. //Values are unique indicating that the copy method is working.  
                        }
                        pixelarray[background_number] = background[rowind, colind];
                        // get mode of this. enter it as the value in output. 
                        background_number++;
                    }
                    uint mode = pixelarray.GroupBy(i => i)
                              .OrderByDescending(g => g.Count())
                              .Select(g => g.Key)
                              .First();
                    output[rowind, colind] = (byte)mode;

                }
            }
            Console.WriteLine("Done");
            return output;
        }
    }
}
