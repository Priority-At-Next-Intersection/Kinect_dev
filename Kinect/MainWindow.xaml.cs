﻿using System;
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
using Kinect.Properties;

namespace Kinect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        ColorFrameSource colorFrameSource;
        ColorFrameReader colorFrameReader;
        BodyFrameSource bodyFrameSource;
        BodyFrameReader bodyFrameReader;
        DrawingGroup drawingGroup;
        Point[] fruitPoints=new Point[2];
        Point[] rectPoints = new Point[2];
        //Point fruitPoint;
        Vector[] fruitVelocities = new Vector[2];
        Point player_1_handL;
        Point player_1_handR;
        Point player_2_handL;
        Point player_2_handR;

        int fruitNum;
        int fruitSize;
        int headSize;
        int score_1;
        int score_2;
        private Random randomGenerator;

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        //转化bitmap格式的图片为imagesource类型的图片
        public static ImageSource ChangeBitmapToImageSource(System.Drawing.Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            ImageSource wpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (!DeleteObject(hBitmap))
            {
                throw new System.ComponentModel.Win32Exception();
            }
            return wpfBitmap;
        }

        public MainWindow()
        {

            sensor = KinectSensor.GetDefault();
            sensor.Open();
            colorFrameSource = sensor.ColorFrameSource;
            colorFrameReader = sensor.ColorFrameSource.OpenReader();
            colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;
            colorFrameSource = sensor.ColorFrameSource;
            bodyFrameSource = sensor.BodyFrameSource;

            // Open the readers for each of the sources
            colorFrameReader = sensor.ColorFrameSource.OpenReader();
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();

            // Create event handlers for each of the readers
            colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;
            bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

            // Get ready to draw graphics
            drawingGroup = new DrawingGroup();
            fruitNum=2;
            
            // Initialize fruit location, velocity, and size

            fruitPoints[0].X = 0;
            fruitPoints[0].Y = colorFrameSource.FrameDescription.Height;
            fruitVelocities[0].X = 15;
            fruitVelocities[0].Y = 30;

            fruitPoints[1].X = 0;
            fruitPoints[1].Y = colorFrameSource.FrameDescription.Height;
            fruitVelocities[1].X = 15;
            fruitVelocities[1].Y = 30;

            //fruitpoints[2].x = 0;
            //fruitpoints[2].y = colorframesource.framedescription.height;
            //fruitvelocities[2].x = 15;
            //fruitvelocities[2].y = 30;

            //fruitpoints[3].x = 0;
            //fruitpoints[3].y = colorframesource.framedescription.height;
            //fruitvelocities[3].x = 15;
            //fruitvelocities[3].y = 30;

            fruitSize = 70;
            headSize = 0;
            // Initialize a random generator
            randomGenerator = new Random();



            InitializeComponent();
        }

        //添加彩色帧到达后的执行函数
        private void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                // Defensive programming: Just in case the sensor skips a frame, exit the function
                if (colorFrame == null)
                {
                    return;
                }

                // Setup an array that can hold all of the bytes of the image
                var colorFrameDescription = colorFrame.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
                var frameSize = colorFrameDescription.Width * colorFrameDescription.Height * colorFrameDescription.BytesPerPixel;
                var colorData = new byte[frameSize];

                // Fill in the array with the data from the camera
                colorFrame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Bgra);

                // Use the byte array to make an image and put it on the screen
                CameraImage.Source = BitmapSource.Create(
                    colorFrame.ColorFrameSource.FrameDescription.Width,
                    colorFrame.ColorFrameSource.FrameDescription.Height,
                    96, 96, PixelFormats.Bgr32, null, colorData, colorFrameDescription.Width * 4);
            }
        }


        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            using (var canvas = drawingGroup.Open())
            {
                // Defensive programming: Just in case the sensor skips a frame, exit the function
                if (bodyFrame == null)
                {
                    return;
                }

                // Get the updated body states for all of the bodies in the scene
                var bodies = new Body[bodyFrame.BodyCount];
                bodyFrame.GetAndRefreshBodyData(bodies);

                // Set the dimensions
                canvas.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0,
                    colorFrameSource.FrameDescription.Width, colorFrameSource.FrameDescription.Height));

                // For each body
                int personID = 0;
                foreach (var body in bodies)
                {
                    personID++;
                    // That is currently being tracked
                    if (body.IsTracked)
                    {

                        var leftHandPoint = body.Joints[JointType.HandLeft].Position;
                        var rightHandPoint = body.Joints[JointType.HandRight].Position;
                        // Uncomment this line to draw a green dot on each tracked joint:
                        //drawJoints(body, Brushes.Green, canvas);
                        Vector speed_L= new Vector(0,0);
                        Vector speed_R = new Vector(0, 0);
                        // Draw dots on the hand joints
                        if (personID == 1)
                        {
                            speed_L.X = body.Joints[JointType.HandLeft].Position.X - player_1_handL.X;
                            speed_L.Y = body.Joints[JointType.HandLeft].Position.Y - player_1_handL.Y;

                            speed_R.X = body.Joints[JointType.HandRight].Position.X - player_1_handL.X;
                            speed_R.Y = body.Joints[JointType.HandRight].Position.Y - player_1_handL.Y;
                            
                            Point headPoint1 = new Point(0,0);
                            headPoint1.X = body.Joints[JointType.Neck].Position.X - headSize;
                            headPoint1.Y = body.Joints[JointType.Neck].Position.Y - headSize;
                            Rect rect1;
                            
                            rect1 = new Rect(headPoint1, new Size(headSize * 2, headSize * 2));
                            //canvas.DrawEllipse(Brushes.DarkGreen, null, headPoint1, 30, 30);
                            //canvas.DrawImage(ChangeBitmapToImageSource(Properties.Resources.ProfChen), rect1);
                        }
                        if (personID == 2)
                        {
                            speed_L.X = body.Joints[JointType.HandLeft].Position.X - player_2_handL.X;
                            speed_L.Y = body.Joints[JointType.HandLeft].Position.Y - player_2_handL.Y;

                            speed_R.X = body.Joints[JointType.HandRight].Position.X - player_2_handL.X;
                            speed_R.Y = body.Joints[JointType.HandRight].Position.Y - player_2_handL.Y;

                            Point headPoint2 = new Point(0, 0);
                            headPoint2.X = body.Joints[JointType.Neck].Position.X - headSize;
                            headPoint2.Y = body.Joints[JointType.Neck].Position.Y - headSize;
                            Rect rect2;
                            rect2 = new Rect(headPoint2, new Size(headSize * 2, headSize * 2));
                            //canvas.DrawEllipse(Brushes.DarkGreen, null, headPoint2, 30, 30);
                            //canvas.DrawImage(ChangeBitmapToImageSource(Properties.Resources.ProfChen), rect2);
                        }
                        
                        drawCameraPoint(leftHandPoint, Brushes.Blue, 15, canvas);
                        drawCameraPoint(rightHandPoint, Brushes.Red, 15, canvas);
                        for(int i=0;i<fruitNum;i++)
                        {
                            // Left Hand
                            if (checkFruitCollision(leftHandPoint, fruitPoints[i], fruitSize))
                            {
                                
                                //fruitPoints[i] = new Point(-100, -100);
                                fruitVelocities[i].X = speed_L.X*100 ;
                                fruitVelocities[i].Y = speed_L.Y*100 ;
                                // Increase the score
                                if (personID == 1)
                                {
                                    score_1++;
                                    ScoreLabel_1.Content = score_1;
                                }
                                if (personID == 2)
                                {
                                    score_2++;
                                    ScoreLabel_2.Content = score_2;
                                }

                            }

                            //Right Hand
                            if (checkFruitCollision(rightHandPoint, fruitPoints[i], fruitSize))
                            {

                                //fruitPoints[i] = new Point(-100, -100);
                                fruitVelocities[i].X = speed_R.X*100 ;
                                fruitVelocities[i].Y = speed_R.Y*100 ;
                                // Increase the score
                                if (personID == 1)
                                {
                                    score_1++;
                                    ScoreLabel_1.Content = score_1;
                                }
                                if (personID == 2)
                                {
                                    score_2++;
                                    ScoreLabel_2.Content = score_2;
                                }

                            }
                        }
                        if (personID == 1)
                        {
                            player_1_handL.X = body.Joints[JointType.HandLeft].Position.X;
                            player_1_handL.Y = body.Joints[JointType.HandLeft].Position.Y;

                            player_1_handR.X = body.Joints[JointType.HandRight].Position.X;
                            player_1_handR.Y = body.Joints[JointType.HandRight].Position.Y;
                        }
                        if (personID == 2)
                        {
                            player_2_handL.X = body.Joints[JointType.HandLeft].Position.X;
                            player_2_handL.Y = body.Joints[JointType.HandLeft].Position.Y;

                            player_2_handR.X = body.Joints[JointType.HandRight].Position.X;
                            player_2_handR.Y = body.Joints[JointType.HandRight].Position.Y;
                        }
                    }
                }
                for (int i = 0; i < fruitNum; i++)
                {
                    // Move the fruit
                    fruitPoints[i].X += fruitVelocities[i].X;
                    fruitPoints[i].Y += fruitVelocities[i].Y;

                    // Apply gravity to the fruit
                    fruitVelocities[i].Y += 0.6;

                    // Check if the fruit is off the screen
                    if (fruitPoints[i].X > 0 && fruitPoints[i].X < colorFrameSource.FrameDescription.Width
                    && fruitPoints[i].Y > 0 && fruitPoints[i].Y < colorFrameSource.FrameDescription.Height)
                    {
                        // Draw the fruit
                        canvas.DrawEllipse(Brushes.Yellow, null, fruitPoints[i], 1, 1);
                        double p = fruitPoints[i].X - fruitSize;
                        double q = fruitPoints[i].Y - fruitSize;
                        rectPoints[i].X = p;
                        rectPoints[i].Y = q;
                        Rect rect;
                        rect = new Rect(rectPoints[i], new Size(fruitSize * 2, fruitSize * 2));
                        if (i==0)
                        {
                            canvas.DrawImage(ChangeBitmapToImageSource(Properties.Resources.volleyball), rect);

                        }
                        else
                        {
                            canvas.DrawImage(ChangeBitmapToImageSource(Properties.Resources.basketball), rect);
                        }

                    }
                    else
                    {
                        // Reset the fruit location and velocity
                        if (randomGenerator.Next(2) == 1) // Pick a random side to start from
                        {
                            fruitPoints[i] = new Point(colorFrameSource.FrameDescription.Width, colorFrameSource.FrameDescription.Height);
                            fruitVelocities[i] = new Vector(-15, -30);
                        }
                        else
                        {
                            fruitPoints[i] = new Point(0, colorFrameSource.FrameDescription.Height);
                            fruitVelocities[i] = new Vector(15, -30);
                        }
                    }
                }
                // Show the drawing on the screen
                DrawingImage.Source = new DrawingImage(drawingGroup);
            }
        }
        private bool checkFruitCollision(CameraSpacePoint cameraPoint, Point fruitPoint, int fruitSize)
        {
            // Convert the CameraSpacePoint to a 2D point
            var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraPoint);
            var canvasPoint = new Point(colorPoint.X, colorPoint.Y);

            // Get the pythagorean distance between the hand and the fruit
            var dist = Math.Sqrt(Math.Pow(canvasPoint.X - fruitPoint.X, 2) +
                Math.Pow(canvasPoint.Y - fruitPoint.Y, 2));

            // If the distance is less than the radius, then we have a collision
            if (dist < fruitSize)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
         private void drawJoints(Body body, Brush brushColor, DrawingContext canvas)
        {
            foreach (var jointType in body.Joints.Keys)
            {
                // Get the point (in 3D space) for the joint
                var cameraPoint = body.Joints[jointType].Position;

                // Draw it using the helper method below
                drawCameraPoint(cameraPoint, brushColor, 15, canvas);
            }
        }

        private void drawCameraPoint(CameraSpacePoint cameraPoint, Brush brushColor, int radius, DrawingContext canvas)
        {
            // Convert the point into 2D so we can use it on the screen
            var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraPoint);
            var canvasPoint = new Point(colorPoint.X, colorPoint.Y);

            // Check if it's safe to draw at that point
            if (canvasPoint.X > 0 && canvasPoint.X < colorFrameSource.FrameDescription.Width
                && canvasPoint.Y > 0 && canvasPoint.Y < colorFrameSource.FrameDescription.Height)
            {
                // Draw a circle
                canvas.DrawEllipse(brushColor, null, canvasPoint, radius, radius);
            }
        }


    }
}
