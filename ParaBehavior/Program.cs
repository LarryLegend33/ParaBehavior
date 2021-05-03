using System;
using System.Linq;
using System.Drawing;
using NationalInstruments.Vision.Acquisition.Imaq;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.IO.Ports;
using System.IO;
using System.Threading.Tasks.Dataflow;
using System.Diagnostics;
using System.Collections.Generic;

namespace ParaBehavior
{
    class Program
    {
        static SerialPort pyboard = new SerialPort("COM4", 115200);
        static SerialPort pyboard2 = new SerialPort("COM5", 115200);
        static Stopwatch experiment_timer = new Stopwatch();
        //static int max_interval = 50000;

        static void Main(string[] args)
        {

            // set up pyboard for shock control
            pyboard.Open();
            pyboard.WriteLine("import para\r");
            pyboard2.Open();
            pyboard2.WriteLine("import shockpara\r");

            // set up random number generator
            var rand = new Random();
            // set up video camera
            var options = new DataflowBlockOptions();
            options.BoundedCapacity = 10;
            MCvScalar gray = new MCvScalar(128, 128, 128);
            string camera_id = "img0"; //this is the ID of the NI-IMAQ board in NI MAX. 
            var _session = new ImaqSession(camera_id);
            var jlist = new List<uint>(); 
            int frameWidth = 1280;
            int frameHeight = 1024;
            uint bufferCount = 3;
            uint buff_out = 0;
            int numchannels = 1;
            Size framesize = new Size(frameWidth, frameHeight);
            Mat cvimage = new Mat(framesize, Emgu.CV.CvEnum.DepthType.Cv8U, numchannels);
            byte[,] data_2D = new byte[frameHeight, frameWidth];
            Console.WriteLine("Please Enter Experiment ID");
            string exp_id = Console.ReadLine();
            DirectoryInfo di = Directory.CreateDirectory("E:/ParaBehaviorData/" + exp_id);
            Console.WriteLine("Please Enter Condition (1=experiment, 2=control)");
            int cond = Convert.ToInt32(Console.ReadLine());

            VideoWriter camvid = new VideoWriter("E:/ParaBehaviorData/" + exp_id + "/" + exp_id + ".AVI", 0, 100, framesize, false);
            string logpath = "E:/ParaBehaviorData/" + exp_id + "/" + exp_id + "_log.txt";
            ImaqBuffer image = null;
            ImaqBufferCollection buffcollection = _session.CreateBufferCollection((int)bufferCount, ImaqBufferCollectionType.VisionImage);
            _session.RingSetup(buffcollection, 0, false);
            _session.Acquisition.AcquireAsync();
            uint j = buff_out;

            // experiment parameters
            int trials = 10;  // number of trials
            int ISI = 2000;    // interstimulus interval (between CS onset and US onset)
            int US_dur = 2000;  // US (shock) duration (msec)
            int CS_dur = 4000;  // CS (tone) duration (msec)
            int CS_intensity = 80; // duty cycle % for CS pulse width
            int min_interval = 7000; // minimum ITI
            double mean_interval = 15000;    // mean interval between CS onsets

            // write experiment parameters to file
            string[] lines = { "ISI: " + Convert.ToString(ISI), "US_dur: " + Convert.ToString(US_dur), "CS_dur: " + Convert.ToString(CS_dur), "CS_intensity: " + Convert.ToString(CS_intensity), "mean_interval: " + Convert.ToString(mean_interval), "min_interval: " + Convert.ToString(min_interval), "cond: " + Convert.ToString(cond), "trials: " + Convert.ToString(trials) };
            string docpath = "E:/ParaBehaviorData/" + exp_id + "/" + exp_id + "_param.txt";
            using (StreamWriter outputFile = new StreamWriter(docpath))
            {
                foreach (string line in lines)
                    outputFile.WriteLine(line);
            }

            int[] CS_times = new int[trials];
            int[] US_times = new int[trials];
            CS_times[0] = ExpRnd(mean_interval,rand) + 1000;
            for (int i = 1; i < trials; i++)
            {
                CS_times[i] = CS_times[i - 1] + ExpRnd(mean_interval, rand) + min_interval;
            }
            int experiment_duration = CS_times[trials-1] + 10000;
            Console.WriteLine(experiment_duration);
            switch (cond)
            {
                case 1:
                    for (int i = 0; i < trials; i++)
                    {
                        US_times[i] = CS_times[i] + ISI;
                    }
                    break;
                case 2:
                    US_times[0] = ExpRnd(mean_interval, rand) + 1000;
                    for(int i = 1; i < trials; i++)
                    {
                        US_times[i] = US_times[i - 1] + ExpRnd(mean_interval, rand) + min_interval;
                    }
                    break;
            }

            // write event times to disk
            File.WriteAllLines("E:/ParaBehaviorData/" + exp_id + "/" + exp_id + "_CS_times.txt", CS_times.Select(tb => tb.ToString()));
            File.WriteAllLines("E:/ParaBehaviorData/" + exp_id + "/" + exp_id + "_US_times.txt", US_times.Select(tb => tb.ToString()));
            
            var tonethread = new Thread(() => PlayCS(CS_dur, CS_intensity, CS_times));
            tonethread.Start();
            var shockthread = new Thread(() => ShockPara(US_dur, US_times));
            shockthread.Start();

            experiment_timer.Start();

            while (true)
            {

                if (experiment_timer.ElapsedMilliseconds > experiment_duration)
                {
                    Console.WriteLine(Convert.ToString(experiment_timer.ElapsedMilliseconds));
                    camvid.Dispose();
                    // Disconnect the camera
                    CvInvoke.DestroyAllWindows();
                    using (StreamWriter logfile = new StreamWriter(logpath))
                    {
                        for (int jind = 0; jind < jlist.Count; jind++)
                        {
                            logfile.WriteLine(jlist[jind].ToString());
                        }
                    }
                    break;
                }
                // write images to file
                image = _session.Acquisition.Extract(j, out buff_out);
                data_2D = image.ToPixelArray().U8;
                cvimage.SetTo(data_2D);
                camvid.Write(cvimage);
                jlist.Add(j);

                j = buff_out + 1;
                
            }
            


        }

        public static int ExpRnd(double m, Random rand)
        {
            // exponential random numbers (rounded) using the inverse transform method
            double y = -Math.Log(1 - rand.NextDouble()) * m;
            return (int)y;
        }

        public static void PlayCS(int CS_dur, int CS_intensity, int[] times)
        {
            int trial = 0;
            while(trial < times.Length)
            {
                if (experiment_timer.ElapsedMilliseconds > times[trial])
                {
                    pyboard.WriteLine("para.motor_buzz(" + Convert.ToString(CS_dur) + "," + Convert.ToString(CS_intensity) +")\r");
                    Console.WriteLine("Trial " + Convert.ToString(trial + 1));
                    trial++;
                }
            }
        }

        public static void ShockPara(int US_dur, int[] times)
        {
            int trial = 0;
            while (trial < times.Length)
            {
                if (experiment_timer.ElapsedMilliseconds > times[trial])
                {
                    pyboard2.WriteLine("shockpara.shock_para(" + Convert.ToString(US_dur) + ")\r");
                    trial++;
                }
            }
        }

    }
}
