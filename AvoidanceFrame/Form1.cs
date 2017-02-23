using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util;

namespace AvoidanceFrame
{
    public partial class Form1 : Form
    {
        private VideoCapture _capture = null;
        //private bool _captureInProgress;
        private Mat _frame;

        public Form1()
        {
            InitializeComponent();
            CvInvoke.UseOpenCL = false;
            try
            {
                _capture = new VideoCapture();
                _capture.Start();
                _capture.ImageGrabbed += ObstacleOverlay;
            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }
            _frame = new Mat();
        }

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
                CvInvoke.Canny(imgGray2, imgEdge, 100, 10);

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
                        /*
                        else
                        {
                            indicies[1] = 0;
                            edgeArray.Add(indicies);

                        }
                        */
                    }
                }

                // Draw lines between points in ObstacleArray (Horizontal-ish lines)
                /*
                for (int i = 0; i < edgeArray.Count - 1; i++)
                {
                    CvInvoke.Line(_frame, new Point(edgeArray[i][0], edgeArray[i][1]), new Point(edgeArray[i + 1][0], edgeArray[i + 1][1]), new MCvScalar(0.0, 255.0, 0.0));
                }
                //Draw the vertical lines
                for (int i = 0; i < edgeArray.Count; i++)
                {
                    CvInvoke.Line(_frame, new Point(i * stepSize, imgEdge.Height), new Point(edgeArray[i][0], edgeArray[i][1]), new MCvScalar(0.0, 255.0, 0.0));
                }
                */
                imageBox1.Image = _frame;
        
            }
        }
    }
}
