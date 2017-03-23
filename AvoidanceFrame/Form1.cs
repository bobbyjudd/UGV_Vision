using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FlyCapture2Managed;


namespace AvoidanceFrame
{
    public partial class Form1 : Form
    {
        private VideoCapture _capture = null;
        private Mat _frame;
        private VectorOfVectorOfPoint maxContour = null;
        private BackgroundWorker m_grabThread;

        
        private ManagedCamera pgCam = null;
        private ManagedBusManager pgBusMgr = null;
        private ManagedImage rawImage = null;
        private ManagedImage convImage = null;
        

        


        public Form1()
        {
            InitializeComponent();
            CvInvoke.UseOpenCL = false;

            
            try
            {
                // Basic webcam stuff
                _capture = new VideoCapture();
                _capture.Start();
                _capture.ImageGrabbed += hsvMethod;
                //Application.Idle += hsvMethod;
                
            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }
            
            _frame = new Mat();
            maxContour = new VectorOfVectorOfPoint();

            /*
            // Point Grey
            pgCam = new ManagedCamera();
            pgBusMgr = new ManagedBusManager();

            pgCam.Connect(pgBusMgr.GetCameraFromIndex(0));

            pgCam.StartCapture();

            Application.Idle += pgFrame;
            rawImage = new ManagedImage();
            convImage = new ManagedImage();

            m_grabThread = new BackgroundWorker();
            m_grabThread.ProgressChanged += new ProgressChangedEventHandler(hsvMethod);
            m_grabThread.DoWork += new DoWorkEventHandler(GrabLoop);
            m_grabThread.WorkerReportsProgress = true;
            m_grabThread.RunWorkerAsync();
            */
        }
                
        private void GrabLoop(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (true)
            {
                try
                {
                    pgCam.RetrieveBuffer(rawImage);
                }
                catch (FC2Exception ex)
                {
                    Debug.WriteLine("Error: " + ex.Message);
                    continue;
                }

                lock (this)
                {
                    rawImage.Convert(PixelFormat.PixelFormatBgr, convImage);
                    _frame = new Image<Bgr, byte>(convImage.bitmap).Mat;
                }
                worker.ReportProgress(0);
            }

            //m_grabThreadExited.Set();
        }

        /* Old  obstacle detection code
        private void ObstacleOverlay(object sender, EventArgs arg)
        {
            if (_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                //Capture frame from video feed
                _capture.Retrieve(_frame, 0);

                int stepSize = 8;
                List<int[]> edgeArray = new List<int[]>();

                // Convert img to grayscale and store result in imgGray
                Mat imgGray = new Mat();
                Mat imgGray2 = new Mat();
                CvInvoke.CvtColor(_frame, imgGray, ColorConversion.Bgr2Gray);

                // Blur the image slightly to remove noise

                CvInvoke.BilateralFilter(imgGray, imgGray2, 9, 30.0, 30.0);
                //Edge detection
                Mat imgEdge = new Mat();
                CvInvoke.Canny(imgGray2, imgEdge, 50, 100);

                //imageBox1.Image = imgEdge;

                
                int[] indicies = new int[2], revInd = new int[2];

                for (int i = 0; i < imgEdge.Width; i += stepSize)
                {
                    indicies[0] = i;
                    revInd[1] = i;
                    for (int j = imgEdge.Height - 5; j >= 0; j--)
                    {
                        indicies[0] = j;
                        revInd[0] = j;
                        //textBox1.Text = imgEdge.GetData(revInd)[0].ToString();
                        if (imgEdge.GetData(revInd)[0] == 255)
                        {
                            CvInvoke.Line(_frame, new Point(i, j), new Point(i + 1, j + 1), new MCvScalar(0.0, 255.0, 0.0), 3);
                            edgeArray.Add(indicies);
                            break;
                        }
                        
                        else
                        {
                            indicies[1] = 0;
                            edgeArray.Add(indicies);
                        }
                        
                    }
                }

                // Draw lines between points in ObstacleArray (Horizontal-ish lines)
                
                for (int i = 0; i < edgeArray.Count - 1; i++)
                {
                    CvInvoke.Line(_frame, new Point(edgeArray[i][0], edgeArray[i][1]), new Point(edgeArray[i + 1][0], edgeArray[i + 1][1]), new MCvScalar(0.0, 255.0, 0.0));
                }
                //Draw the vertical lines
                for (int i = 0; i < edgeArray.Count; i++)
                {
                    CvInvoke.Line(_frame, new Point(i * stepSize, imgEdge.Height), new Point(edgeArray[i][0], edgeArray[i][1]), new MCvScalar(0.0, 255.0, 0.0));
                }
                
                imageBox1.Image = _frame;
        
            }

        }
    */

        void hsvMethod(object sender, EventArgs arg)
        {
            if (_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                _capture.Retrieve(_frame, 0);
                Mat hsv = new Mat();
                Mat mask = new Mat();
                Mat filtered = new Mat();
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                //HSV Threshold limits
                MCvScalar lowerBound = new MCvScalar(0, 50, 30);
                MCvScalar upperBound = new MCvScalar(15, 255, 255);
                
                //Convert BGR to HSV
                CvInvoke.CvtColor(_frame, hsv, ColorConversion.Bgr2Hsv);

                // Isolate Color range of interest, smooth, and convert to b/w
                CvInvoke.InRange(hsv, new ScalarArray(lowerBound), new ScalarArray(upperBound), mask);
                CvInvoke.GaussianBlur(mask, filtered, new Size(25, 25), 0.0);
                CvInvoke.Threshold(filtered, mask, 150.0, 255.0, ThresholdType.Binary);
                
                //Outling all shapes found
                CvInvoke.FindContours(mask, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                //Find largest area contour
                int maxVal = 0;
                for (int i = 0; i < contours.Size; i++)
                {
                    if (contours[i].Size > maxVal)
                    {
                        maxVal = contours.Size;
                        if (maxContour.Size != 0)
                            maxContour.Clear();
                        maxContour.Push(contours[i]);
                    }
                }

                //Draw on frame
                CvInvoke.DrawContours(_frame,maxContour, -1, new MCvScalar(0.0, 255.0, 0.0));

                imageBox1.Image = _frame;
            }
        }
    }
}
