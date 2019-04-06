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
using Kinect.Properties;

namespace Kinect
{
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        ColorFrameSource colorFrameSource;
        ColorFrameReader colorFrameReader;
        BodyFrameSource bodyFrameSource;
        BodyFrameReader bodyFrameReader;
        DrawingGroup drawingGroup;
        Point[] BallPoints=new Point[2];
        Point[] lastFrameBallPoints = new Point[2];
        Point[] rectPoints = new Point[2];
        //Point BallPoint;
        Vector[] BallVelocities = new Vector[2];
        Point player_1_handL;
        Point player_1_handR;
        Point player_2_handL;
        Point player_2_handR;


        int BallNum;
        int BallSize;
        int headSize;
        int score_1;
        int score_2;
        private Random randomGenerator;

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        //Convert image type from 'bitmap' to 'imagesource'
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
            BallNum=2;
            
            // Initialize Ball location, velocity, and size

            BallPoints[0].X = 0;
            BallPoints[0].Y = colorFrameSource.FrameDescription.Height;
            BallVelocities[0].X = 40;
            BallVelocities[0].Y = 80;

            BallPoints[1].X = 0;
            BallPoints[1].Y = colorFrameSource.FrameDescription.Height;
            BallVelocities[1].X = 45;
            BallVelocities[1].Y = 79;

            //BallPoints[2].X = 0;
            //BallPoints[2].Y = colorFrameSource.FrameDescription.Height;
            //BallVelocities[2].X = 20;
            //BallVelocities[2].Y = 40;

            //Ballpoints[3].x = 0;
            //Ballpoints[3].y = colorframesource.framedescription.height;
            //Ballvelocities[3].x = 15;
            //Ballvelocities[3].y = 30;

            BallSize = 70;
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
                        for(int i=0;i<BallNum;i++)
                        {
                            // Left Hand
                            if (checkBallCollision(leftHandPoint, BallPoints[i], 100,i))
                            {
                                
                                //BallPoints[i] = new Point(-100, -100);
                                BallVelocities[i].X = speed_L.X*100 ;
                                BallVelocities[i].Y = speed_L.Y*100 ;
                                // Increase the score
                                //if (personID == 1)
                                //{
                                //    score_1++;
                                //    ScoreLabel_1.Content = score_1;
                                //}
                                //if (personID == 2)
                                //{
                                //    score_2++;
                                //    ScoreLabel_2.Content = score_2;
                                //}

                            }

                            //Right Hand
                            if (checkBallCollision(rightHandPoint, BallPoints[i], BallSize,i))
                            {

                                //BallPoints[i] = new Point(-100, -100);
                                BallVelocities[i].X = speed_R.X*100 ;
                                BallVelocities[i].Y = speed_R.Y*100 ;
                                // Increase the score
                                //if (personID == 1)
                                //{
                                //    score_1++;
                                //    ScoreLabel_1.Content = score_1;
                                //}
                                //if (personID == 2)
                                //{
                                //    score_2++;
                                //    ScoreLabel_2.Content = score_2;
                                //}

                            }

                            Point basketPoint_1 = new Point(300, 80);
                            Point basketPoint_2 = new Point(1000, 80);
                            if (checkBallCollision(basketPoint_1, BallPoints[i], BallSize,i))
                            {


                                score_2++;
                                ScoreLabel_2.Content = score_2;
                                BallVelocities[i].X = -BallVelocities[i].X;
                                BallVelocities[i].Y = -BallVelocities[i].Y;
                            }
                            if (checkBallCollision(basketPoint_2, BallPoints[i], BallSize,i))
                            {
                                score_1++;
                                ScoreLabel_1.Content = score_1;
                                BallVelocities[i].X = -BallVelocities[i].X;
                                BallVelocities[i].Y = -BallVelocities[i].Y;
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
                for (int i = 0; i < BallNum; i++)
                {
                    lastFrameBallPoints[i].X = BallPoints[i].X;
                    lastFrameBallPoints[i].Y = BallPoints[i].Y;
                    // Move the Ball
                    BallPoints[i].X += BallVelocities[i].X;
                    BallPoints[i].Y += BallVelocities[i].Y;

                    // Apply gravity to the Ball
                    BallVelocities[i].Y += 0.9;

                    // Check if the Ball is off the screen
                    if (BallPoints[i].X > 0 && BallPoints[i].X < colorFrameSource.FrameDescription.Width
                    && BallPoints[i].Y > 0 && BallPoints[i].Y < colorFrameSource.FrameDescription.Height)
                    {
                        // Draw the Ball
                        canvas.DrawEllipse(Brushes.Yellow, null, BallPoints[i], 1, 1);
                        double p = BallPoints[i].X - BallSize;
                        double q = BallPoints[i].Y - BallSize;
                        rectPoints[i].X = p;
                        rectPoints[i].Y = q;
                        Rect rect;
                        rect = new Rect(rectPoints[i], new Size(BallSize * 2, BallSize * 2));
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
                        // Reset the Ball location and velocity
                        if (randomGenerator.Next(2) == 1) // Pick a random side to start from
                        {
                            BallPoints[i] = new Point(colorFrameSource.FrameDescription.Width, colorFrameSource.FrameDescription.Height);
                            BallVelocities[i] = new Vector(-20, -40);
                        }
                        else
                        {
                            BallPoints[i] = new Point(0, colorFrameSource.FrameDescription.Height);
                            BallVelocities[i] = new Vector(20, -40);
                        }
                    }
                }
                // Show the drawing on the screen
                DrawingImage.Source = new DrawingImage(drawingGroup);
            }
        }

        private bool checkBallCollision(Point basketPoint, Point BallPoint, int size, int i)
        {
            BallPoint.X = BallPoint.X + BallPoints[i].X - lastFrameBallPoints[i].X;
            BallPoint.Y = BallPoint.Y + BallPoints[i].Y - lastFrameBallPoints[i].Y;
            var dist = Math.Sqrt(Math.Pow(basketPoint.X - BallPoint.X, 2) +
                Math.Pow(basketPoint.Y - BallPoint.Y, 2));

            // If the distance is less than the radius, then we have a collision
            if (dist < size)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool checkBallCollision(CameraSpacePoint cameraPoint, Point BallPoint, int BallSize, int i)
        {
            BallPoint.X = BallPoint.X + BallPoints[i].X - lastFrameBallPoints[i].X;
            BallPoint.Y = BallPoint.Y + BallPoints[i].Y - lastFrameBallPoints[i].Y;
            // Convert the CameraSpacePoint to a 2D point
            var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraPoint);
            var canvasPoint = new Point(colorPoint.X, colorPoint.Y);

            // Get the pythagorean distance between the hand and the Ball
            var dist = Math.Sqrt(Math.Pow(canvasPoint.X - BallPoint.X, 2) +
                Math.Pow(canvasPoint.Y - BallPoint.Y, 2));

            // If the distance is less than the radius, then we have a collision
            if (dist < BallSize*1.2)
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
