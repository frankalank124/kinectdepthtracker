using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Web;
using System.Diagnostics;
using System.Net;
using System.Xml;
using System.IO;

namespace KinectTests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        String service = null;

        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;
        private int frameCount;

        private KinectSensor kinect = null;
        private DepthFrameReader depthFrameReader = null;
        private FrameDescription depthFrameDescription = null;
        //private WriteableBitmap depthBitmap = null;
        private byte[] depthPixels = null;
        private string statusText = null;
        //public httpc

        public MainWindow()
        {
            frameCount = 0;

            // get the kinectSensor object
            this.kinect = KinectSensor.GetDefault();

            // open the reader for the depth frames
            this.depthFrameReader = this.kinect.DepthFrameSource.OpenReader();

            // wire handler for frame arrival
            this.depthFrameReader.FrameArrived += this.Reader_FrameArrived;

            // get FrameDescription from DepthFrameSource
            this.depthFrameDescription = this.kinect.DepthFrameSource.FrameDescription;

            // allocate space to put the pixels being received and converted
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];

            // open the sensor
            this.kinect.Open();

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }


        private unsafe void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance

                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            frameCount += 3;

                            int left_index;
                            int right_index;
                            left_index = 0;
                            right_index = 0;
                            int left_largest = getMin(depthPixels, 'l', &left_index);
                            int right_largest = getMin(depthPixels, 'r', &right_index);
                            int leftXpos = left_index % depthFrameDescription.Width;
                            int leftYpos = left_index / depthFrameDescription.Width;

                            int rightXpos = right_index % depthFrameDescription.Width;
                            int rightYpos = right_index / depthFrameDescription.Width;

                            if (frameCount >= 10)
                            {
                                frameCount = 0;

                                if (left_largest > 0 || true)
                                {
                                    LEFT.Text = left_largest.ToString();
                                    LXpos.Text = leftXpos.ToString();
                                    LYpos.Text = leftYpos.ToString();
                                }
                                if (right_largest > 0 || true)
                                {
                                    RIGHT.Text = right_largest.ToString();
                                    RXpos.Text = rightXpos.ToString();
                                    RYpos.Text = rightYpos.ToString();
                                }
                            }
                            depthFrameProcessed = true;
                        }
                    }
                }
            }
        }

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        private unsafe int getMin(byte[] depthArray, char side, int* index)
        {
            if (frameCount < 10)
            {
                return -1;
            }
            //byte[] unsorted = new byte[depthArray.Length];
            //int count = 0;
            //for (int i = 0; i < depthArray.Length; i++)
            //{
            //    if (depthArray[i] > 0)
            //    {
            //        unsorted[count++] = depthArray[i];
            //    }
            //}
            //byte[] sorted = new byte[count];
            //for (int i = 0; i < count; i++)
            //{
            //    sorted[i] = unsorted[i];
            //}
            //insertion_sort(sorted);
            //quicksort(sorted, 0, count-1);
            //return sorted[5];

            int minimum = 255;
            
            for (int num_iter = 0; num_iter <= 20; num_iter++)
            {

                int lowerLim = 0, upperLim = 512;
                minimum = 255;
                *index = 0;
                if (side == 'l')
                {
                    upperLim = 256;
                }
                else if (side == 'r')
                {
                    lowerLim = 256;
                }
                for (int x = lowerLim; x < upperLim; x++)
                {
                    for (int y = 0; y < depthFrameDescription.Height; y++)
                    {
                        if (depthArray[x + 512 * y] > 10 && depthArray[x + 512 * y] < minimum)
                        {
                            minimum = depthArray[x + 512 * y];
                            *index = x + 512 * y;
                        }
                    }
                }
                depthArray[*index] = 255;
            }


            return minimum;
        }

        public void insertion_sort(byte[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                byte x = a[i];
                int j = i-1;
                while (j >= 0 && a[j]>x)
                {
                    a[j+1] = a[j];
                    j -= 1;
                }
                a[j+1] = x;
            }
        }

        public void quicksort(byte[] arr, int low, int high)
        {
            if (low < high)
            {
                int mid = partition(arr, low, high);
                quicksort(arr, low, mid - 1);
                quicksort(arr, mid + 1, high);
            }
        }
        public int partition(byte[] arr, int l, int h) 
        {
            int pivot = arr[h];
            int i = l;
            for (int j = l; j < h - 1; j++)
            {
                if (arr[j] <= pivot)
                {
                    swap(arr, i, j);
                    i += 1;
                }
            }
            swap(arr, i, h);
            return i;
        }
        public void swap(byte[] arr, int x1, int x2)
        {
            byte temp = arr[x1];
            arr[x1] = arr[x2];
            arr[x2] = temp;
        }

        private void clickExit(object sender, RoutedEventArgs e)
        {
            Label clickedLabel = sender as Label;
            kinect.Close();
            System.Environment.Exit(1);
        }
    }
}
