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
        Point player_1_LastLHand;
        Point player_1_LastRHand;
        Point player_2_LastLHand;
        Point player_2_LastRHand;
        Point basketPoint_1 = new Point(300, 80);
        Point basketPoint_2 = new Point(1000, 80);

        int BallNum;
        int BallSize;
        int headSize;
        int handSize;
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

            BallSize = 70;
            headSize = 250;
            handSize = 125;

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

                /*********     For each person    ******** */
                int personID = 0;
                foreach (var body in bodies)
                {
                    personID++;
                    // That is currently being tracked
                    if (body.IsTracked)
                    {
                        
                        var leftHandPoint = body.Joints[JointType.HandLeft].Position;
                        var rightHandPoint = body.Joints[JointType.HandRight].Position;
                        Vector speed_LHand= new Vector(0,0);
                        Vector speed_RHand = new Vector(0, 0);

                        if (personID == 1)
                        {
                            var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[JointType.HandLeft].Position);
                            var CurrentHand = new Point(colorPoint.X, colorPoint.Y);

                            speed_LHand.X = CurrentHand.X - player_1_LastLHand.X;
                            speed_LHand.Y = CurrentHand.Y - player_1_LastLHand.Y;

                            speed_RHand.X = CurrentHand.X - player_1_LastLHand.X;
                            speed_RHand.Y = CurrentHand.Y - player_1_LastLHand.Y;

                            var headPoint1 = body.Joints[JointType.Head].Position;//Still Camera coordinate.
                            drawHeadPoint(headPoint1, canvas);//this func will change camera coordinate into canvas one.

                        }
                        if (personID == 2)
                        {
                            var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[JointType.HandLeft].Position);
                            var canvasPoint = new Point(colorPoint.X, colorPoint.Y);

                            speed_LHand.X = canvasPoint.X - player_2_LastLHand.X;
                            speed_LHand.Y = canvasPoint.Y - player_2_LastLHand.Y;

                            speed_RHand.X = canvasPoint.X - player_2_LastLHand.X;
                            speed_RHand.Y = canvasPoint.Y - player_2_LastLHand.Y;

                            var headPoint2 = body.Joints[JointType.Head].Position;
                            drawHeadPoint(headPoint2, canvas);
                        }
                        
                        drawHandPoint(leftHandPoint,canvas);
                        drawHandPoint(rightHandPoint, canvas);

                        for(int i=0;i<BallNum;i++)
                        {
                            // Left Hand
                            if (checkBallCollision(leftHandPoint, BallPoints[i], 100,i))
                            {
                                BallVelocities[i].X = speed_LHand.X;
                                BallVelocities[i].Y = speed_LHand.Y;
                            }

                            //Right Hand
                            if (checkBallCollision(rightHandPoint, BallPoints[i], BallSize,i))
                            {
                                BallVelocities[i].X = speed_RHand.X;
                                BallVelocities[i].Y = speed_RHand.Y;
                            }
                        }
                        if (personID == 1)
                        {
                            var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[JointType.HandLeft].Position);
                            var canvasPoint = new Point(colorPoint.X, colorPoint.Y);

                            player_1_LastLHand.X = canvasPoint.X;
                            player_1_LastLHand.Y = canvasPoint.Y;
                            

                            player_1_LastRHand.X = canvasPoint.X;
                            player_1_LastRHand.Y = canvasPoint.Y;
                        }
                        if (personID == 2)
                        {
                            var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[JointType.HandLeft].Position);
                            var canvasPoint = new Point(colorPoint.X, colorPoint.Y);

                            player_2_LastLHand.X = canvasPoint.X;
                            player_2_LastLHand.Y = canvasPoint.Y;

                            player_2_LastRHand.X = canvasPoint.X;
                            player_2_LastRHand.Y = canvasPoint.Y;
                        }
                    }
                }

                /*********     For each ball    ******** */
                for (int i = 0; i < BallNum; i++)
                {
                    lastFrameBallPoints[i].X = BallPoints[i].X;
                    lastFrameBallPoints[i].Y = BallPoints[i].Y;

                    BallPoints[i].X += BallVelocities[i].X;
                    BallPoints[i].Y += BallVelocities[i].Y;
                    if (checkBallCollision(basketPoint_1, BallPoints[i], BallSize, i))
                    {
                        score_2++;
                        ScoreLabel_2.Content = score_2;
                        BallVelocities[i].X = -BallVelocities[i].X;
                        BallVelocities[i].Y = -BallVelocities[i].Y;
                    }
                    if (checkBallCollision(basketPoint_2, BallPoints[i], BallSize, i))
                    {
                        score_1++;
                        ScoreLabel_1.Content = score_1;
                        BallVelocities[i].X = -BallVelocities[i].X;
                        BallVelocities[i].Y = -BallVelocities[i].Y;
                    }
                    BallVelocities[i].Y += 0.9;

                    // Check if the Ball is in the screen
                    if (BallPoints[i].X > 0 && BallPoints[i].X < colorFrameSource.FrameDescription.Width
                    && BallPoints[i].Y > 0 && BallPoints[i].Y < colorFrameSource.FrameDescription.Height)
                    {
                        // Draw the Ball
                        rectPoints[i].X = BallPoints[i].X - BallSize;
                        rectPoints[i].Y = BallPoints[i].Y - BallSize;
                        Rect rect;
                        rect = new Rect(rectPoints[i], new Size(BallSize * 2, BallSize * 2));
                        if (i==0)
                        {
                            // Ball A
                            canvas.DrawImage(ChangeBitmapToImageSource(Properties.Resources.volleyball), rect);
                        }
                        else
                        {
                            // Ball B
                            canvas.DrawImage(ChangeBitmapToImageSource(Properties.Resources.basketball), rect);
                        }
                    }
                    else
                    {
                        // if the Ball is off the screen then Reset the Ball location and velocity
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

        private void drawHeadPoint(CameraSpacePoint cameraPoint,DrawingContext canvas)
        {
            // Convert the point into 2D so we can use it on the screen
            var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraPoint);
            var canvasPoint = new Point(colorPoint.X, colorPoint.Y);

            // Check if it's safe to draw at that point
            if (canvasPoint.X > 0 && canvasPoint.X < colorFrameSource.FrameDescription.Width
                && canvasPoint.Y > 0 && canvasPoint.Y < colorFrameSource.FrameDescription.Height)
            {
                // Draw a circle
                Rect rect;
                canvasPoint.X = canvasPoint.X - headSize/2.0f;
                canvasPoint.Y = canvasPoint.Y - headSize/2.0f;
                rect = new Rect(canvasPoint, new Size(headSize, headSize));
                canvas.DrawImage(ChangeBitmapToImageSource(Properties.Resources.cat), rect);
                //canvas.DrawEllipse(Brushes.Yellow, null, canvasPoint, 12, 12);
            }
        }

        private void drawHandPoint(CameraSpacePoint cameraPoint, DrawingContext canvas)
        {
            // Convert the point into 2D so we can use it on the screen
            var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraPoint);
            var canvasPoint = new Point(colorPoint.X, colorPoint.Y);

            // Check if it's safe to draw at that point
            if (canvasPoint.X > 0 && canvasPoint.X < colorFrameSource.FrameDescription.Width
                && canvasPoint.Y > 0 && canvasPoint.Y < colorFrameSource.FrameDescription.Height)
            {
                // Draw a circle
                Rect rect;
                canvasPoint.X = canvasPoint.X - handSize / 2.0f;
                canvasPoint.Y = canvasPoint.Y - handSize / 2.0f;
                rect = new Rect(canvasPoint, new Size(handSize, handSize));
                canvas.DrawImage(ChangeBitmapToImageSource(Properties.Resources.paw), rect);
                //canvas.DrawEllipse(Brushes.Yellow, null, canvasPoint, 12, 12);
            }
        }
    }
}
