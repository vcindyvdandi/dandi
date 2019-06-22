


namespace Microsoft.Samples.Kinect.BodyBasics
{
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
        using System.ComponentModel;
        using System.Diagnostics;
        using System.Globalization;
        using System.IO;
        using Microsoft.Kinect;
    /// <summary>
    /// TrainingPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TrainingPage : Page
    {

        /*---------------------------화면에 나오는 기호들(원,네모,사각형..)----------------------*/
        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;
        /* ---------------------------그리기 기호 끝----------------------------------*/

        /*-----------좌표 가져오기--------------------*/
        /// <summary>
        /// Active Kinect sensor 키넥트 센서
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames 바디 프레임 리더기
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies 배열
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones  정의한 뼈들을 배열로 집어넣기 위함
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space) 깊이
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space) 높이
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        int frameNumber = 0;
        int record_time = 0; //record
        int train_time = 0;

       

        bool record_btn_on = false;
        bool train_btn_on = false;
        bool ChooseTrain_btn_on = false;
        double[] match_rate = new double[70];

        double[,] temp_xyData = new double[100, 10];
        double[,] temp_xzData = new double[100, 10];



        public TrainingPage()
        {

            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault(); //키넥스 센서 초기화

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription; //키넥트 센서의 depthframesource로 부터 프레임정의를 가져옴

            // get size of joint space
            this.displayWidth = frameDescription.Width; //키넥트 센서로부터 가져온 프레임 정의에서 깊이를 가져옴
            this.displayHeight = frameDescription.Height; //키넥트 센서로부터 가져온 프레임 정의에서 높이를 가져옴

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader(); //키넥트 센서로부터 바디프레임 리더를 가져온다.

            // a bone defined as a line between two joints //관절과 관절 사이를 연결하여 뼈 정의하기 (이 때 뼈들은 배열로 선언)
            this.bones = new List<Tuple<JointType, JointType>>();

            /*------------관절을 정하고 그 관절에 맞는 뼈를 정의하여 리스트에 집어넣는다-------------*/
            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            /*------------뼈 정의 끝-------------*/

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open(); //키넥트 센서 오픈

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText; //isAailable이 가능하면 statustext가 돌아가게 하고 가능하지 않으면 돌아가지 않게 함

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup(); //기호를 그리기위한 도구 그룹을 연다.

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup); //그리기 도구로부터 우리가 현재 인식된 몸의 구조를 비주얼화 한 화면을 이미지 소스로 만듬

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }
    
    /// <summary>
    /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets the bitmap to display
    /// </summary>
    public ImageSource ImageSource
    {
        get
        {
            return this.imageSource;
        }
    }

    /// <summary>
    /// Gets or sets the current status text to display
    /// </summary>
    public string StatusText
    {
        get
        {
            return this.statusText;
        }

        set
        {
            if (this.statusText != value)
            {
                this.statusText = value;

                // notify any bound elements that the text has changed
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                }
            }
        }
    }

    /// <summary>
    /// Execute start up tasks
    /// </summary>
    /// <param name="sender">object sending the event</param>
    /// <param name="e">event arguments</param>
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (this.bodyFrameReader != null)
        {
            this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
        }
    }

    /// <summary>
    /// Execute shutdown tasks
    /// </summary>
    /// <param name="sender">object sending the event</param>
    /// <param name="e">event arguments</param>
    private void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        if (this.bodyFrameReader != null)
        {
            // BodyFrameReader is IDisposable
            this.bodyFrameReader.Dispose();
            this.bodyFrameReader = null;
        }

        if (this.kinectSensor != null)
        {
            this.kinectSensor.Close();
            this.kinectSensor = null;
        }
    }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {

            bool dataReceived = false;


            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    dataReceived = true;




                }
            }

            if (dataReceived)
            {

                using (DrawingContext dc = this.drawingGroup.Open())
                {

                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Pink, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[4];

                        if (body.IsTracked)
                        {
                            frameNumber++;
                            frametext.Text = frameNumber.ToString();
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints; //framereader로부터 받아온 body데이터 값 복사

                            //-------------------------------------------------------------------------------------Train Data 

                            double Head_x = (double)joints[JointType.Head].Position.X;
                            double Head_y = (double)joints[JointType.Head].Position.Y;
                            double Head_z = (double)joints[JointType.Head].Position.Z; //머리

                            double Neck_x = (double)joints[JointType.Neck].Position.X;
                            double Neck_y = (double)joints[JointType.Neck].Position.Y;
                            double Neck_z = (double)joints[JointType.Neck].Position.Z; //목

                            double ShoulderLeft_x = (double)joints[JointType.ShoulderLeft].Position.X;
                            double ShoulderLeft_y = (double)joints[JointType.ShoulderLeft].Position.Y;
                            double ShoulderLeft_z = (double)joints[JointType.ShoulderLeft].Position.Z;//왼쪽 어깨

                            double ShoulderRight_x = (double)joints[JointType.ShoulderRight].Position.X;
                            double ShoulderRight_y = (double)joints[JointType.ShoulderRight].Position.Y;
                            double ShoulderRight_z = (double)joints[JointType.ShoulderRight].Position.Z; //오른쪽 어깨

                            double ElbowLeft_x = (double)joints[JointType.ElbowLeft].Position.X;
                            double ElbowLeft_y = (double)joints[JointType.ElbowLeft].Position.Y;
                            double ElbowLeft_z = (double)joints[JointType.ElbowLeft].Position.Z; //왼쪽 팔꿈치

                            double ElbowRight_x = (double)joints[JointType.ElbowRight].Position.X;
                            double ElbowRight_y = (double)joints[JointType.ElbowRight].Position.Y;
                            double ElbowRight_z = (double)joints[JointType.ElbowRight].Position.Z; //오른쪽 팔꿈치

                            double WristLeft_x = (double)joints[JointType.WristLeft].Position.X;
                            double WristLeft_y = (double)joints[JointType.WristLeft].Position.Y;
                            double WristLeft_z = (double)joints[JointType.WristLeft].Position.Z; //왼쪽 손목

                            double WristRight_x = (double)joints[JointType.WristRight].Position.X;
                            double WristRight_y = (double)joints[JointType.WristRight].Position.Y;
                            double WristRight_z = (double)joints[JointType.WristRight].Position.Z; //오른쪽 손목

                            double SpineBase_x = (double)joints[JointType.SpineBase].Position.X;
                            double SpineBase_y = (double)joints[JointType.SpineBase].Position.Y;
                            double SpineBase_z = (double)joints[JointType.SpineBase].Position.Z; //골반쪽 척추

                            double KneeLeft_x = (double)joints[JointType.KneeLeft].Position.X;
                            double KneeLeft_y = (double)joints[JointType.KneeLeft].Position.Y;
                            double KneeLeft_z = (double)joints[JointType.KneeLeft].Position.Z; //왼쪽 무릎

                            double KneeRight_x = (double)joints[JointType.KneeRight].Position.X;
                            double KneeRight_y = (double)joints[JointType.KneeRight].Position.Y;
                            double KneeRight_z = (double)joints[JointType.KneeRight].Position.Z; //오른쪽 무릎

                            double AnkleLeft_x = (double)joints[JointType.AnkleLeft].Position.X;
                            double AnkleLeft_y = (double)joints[JointType.AnkleLeft].Position.Y;
                            double AnkleLeft_z = (double)joints[JointType.AnkleLeft].Position.Z; //왼쪽 발목

                            double AnkleRight_x = (double)joints[JointType.AnkleRight].Position.X;
                            double AnkleRight_y = (double)joints[JointType.AnkleRight].Position.Y;
                            double AnkleRight_z = (double)joints[JointType.AnkleRight].Position.Z; //오른쪽 발목

                            double Head_Neck_xy = Math.Atan2(Head_y - Neck_y, Head_x - Neck_x) * (180.0 / Math.PI);
                            double Head_Neck_xz = -Math.Atan2(Head_z - Neck_z, Head_x - Neck_x) * (180.0 / Math.PI); //1번 뼈
                            if (Head_Neck_xy < 0)
                            {
                                Head_Neck_xy += 360;
                            }
                            if (Head_Neck_xz < 0)
                            {
                                Head_Neck_xz += 360;
                            }
                            double ElbowLeft_ShoulderLeft_xy = Math.Atan2(ElbowLeft_y - ShoulderLeft_y, ElbowLeft_x - ShoulderLeft_x) * (180.0 / Math.PI);
                            double ElbowLeft_ShoulderLeft_xz = -Math.Atan2(ElbowLeft_z - ShoulderLeft_z, ElbowLeft_x - ShoulderLeft_x) * (180.0 / Math.PI); //2번 뼈                            
                            if (ElbowLeft_ShoulderLeft_xy < 0)
                            {
                                ElbowLeft_ShoulderLeft_xy += 360;
                            }
                            if (ElbowLeft_ShoulderLeft_xz < 0)
                            {
                                ElbowLeft_ShoulderLeft_xz += 360;
                            }

                            double ShoulderRight_ElbowRight_xy = Math.Atan2(ElbowRight_y - ShoulderRight_y, ElbowRight_x - ShoulderRight_x) * (180.0 / Math.PI);
                            double ShoulderRight_ElbowRight_xz = -Math.Atan2(ElbowRight_z - ShoulderRight_z, ElbowRight_x - ShoulderRight_x) * (180.0 / Math.PI); //3번 뼈
                            if (ShoulderRight_ElbowRight_xy < 0)
                            {
                                ShoulderRight_ElbowRight_xy += 360;
                            }
                            if (ShoulderRight_ElbowRight_xz < 0)
                            {
                                ShoulderRight_ElbowRight_xz += 360;
                            }


                            double ElbowLeft_WristLeft_xy = Math.Atan2(WristLeft_y - ElbowLeft_y, WristLeft_x - ElbowLeft_x) * (180.0 / Math.PI);
                            double ElbowLeft_WristLeft_xz = -Math.Atan2(WristLeft_z - ElbowLeft_z, WristLeft_x - ElbowLeft_x) * (180.0 / Math.PI); //4번 뼈
                            if (ElbowLeft_WristLeft_xy < 0)
                            {
                                ElbowLeft_WristLeft_xy += 360;
                            }
                            if (ElbowLeft_WristLeft_xz < 0)
                            {
                                ElbowLeft_WristLeft_xz += 360;
                            }

                            //ElbowRight-WristRight
                            double ElbowRight_WristRight_xy = Math.Atan2(WristRight_y - ElbowRight_y, WristRight_x - ElbowRight_x) * (180.0 / Math.PI);
                            double ElbowRight_WristRight_xz = -Math.Atan2(WristRight_z - ElbowRight_z, WristRight_x - ElbowRight_x) * (180.0 / Math.PI); //5번 뼈
                            if (ElbowRight_WristRight_xy < 0)
                            {
                                ElbowRight_WristRight_xy += 360;
                            }
                            if (ElbowRight_WristRight_xz < 0)
                            {
                                ElbowRight_WristRight_xz += 360;
                            }

                            //Neck-SpineBase
                            double Neck_SpineBase_xy = Math.Atan2(Neck_y - SpineBase_y, Neck_x - SpineBase_x) * (180.0 / Math.PI);
                            double Neck_SpineBase_xz = -Math.Atan2(Neck_z - SpineBase_z, Neck_x - SpineBase_x) * (180.0 / Math.PI); //6번 뼈
                            if (Neck_SpineBase_xy < 0)
                            {
                                Neck_SpineBase_xy += 360;
                            }
                            if (Neck_SpineBase_xz < 0)
                            {
                                Neck_SpineBase_xz += 360;
                            }

                            //SpineBase-KneeLeft
                            double SpineBase_KneeLeft_xy = Math.Atan2(KneeLeft_y - SpineBase_y, KneeLeft_x - SpineBase_x) * (180.0 / Math.PI);
                            double SpineBase_KneeLeft_xz = -Math.Atan2(KneeLeft_z - SpineBase_z, KneeLeft_x - SpineBase_x) * (180.0 / Math.PI); //7번 뼈
                            if (SpineBase_KneeLeft_xy < 0)
                            {
                                SpineBase_KneeLeft_xy += 360;
                            }
                            if (SpineBase_KneeLeft_xz < 0)
                            {
                                SpineBase_KneeLeft_xz += 360;
                            }

                            //SpineBase-KneeRight
                            double SpineBase_KneeRight_xy = Math.Atan2(KneeRight_y - SpineBase_y, KneeRight_x - SpineBase_x) * (180.0 / Math.PI);
                            double SpineBase_KneeRight_xz = -Math.Atan2(KneeRight_z - SpineBase_z, KneeRight_x - SpineBase_x) * (180.0 / Math.PI); //8번 뼈
                            if (SpineBase_KneeRight_xy < 0)
                            {
                                SpineBase_KneeRight_xy += 360;
                            }
                            if (SpineBase_KneeRight_xz < 0)
                            {
                                SpineBase_KneeRight_xz += 360;
                            }

                            //KneeLeft-AnkleLeft
                            double KneeLeft_AnkleLeft_xy = Math.Atan2(AnkleLeft_y - KneeLeft_y, AnkleLeft_x - KneeLeft_x) * (180.0 / Math.PI);
                            double KneeLeft_AnkleLeft_xz = -Math.Atan2(AnkleLeft_z - KneeLeft_z, AnkleLeft_x - KneeLeft_x) * (180.0 / Math.PI); //9번 뼈
                            if (KneeLeft_AnkleLeft_xy < 0)
                            {
                                KneeLeft_AnkleLeft_xy += 360;
                            }
                            if (KneeLeft_AnkleLeft_xz < 0)
                            {
                                KneeLeft_AnkleLeft_xz += 360;
                            }

                            //KneeRight-AnkleRight
                            double KneeRight_AnkleRight_xy = Math.Atan2(AnkleRight_y - KneeRight_y, AnkleRight_x - KneeRight_x) * (180.0 / Math.PI);
                            double KneeRight_AnkleRight_xz = -Math.Atan2(AnkleRight_z - KneeRight_z, AnkleRight_x - KneeRight_x) * (180.0 / Math.PI); //10번 뼈
                            if (KneeRight_AnkleRight_xy < 0)
                            {
                                KneeRight_AnkleRight_xy += 360;
                            }
                            if (KneeRight_AnkleRight_xz < 0)
                            {
                                KneeRight_AnkleRight_xz += 360;
                            }

                            angle.Text = Convert.ToString(Convert.ToInt32(ElbowRight_WristRight_xy));
                            angle2.Text = Convert.ToString(Convert.ToInt32(ElbowRight_WristRight_xz));
                            //-------------------------------------------------------------------------------------Train Data 
                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();//2d값을 넣을 배열

                            foreach (JointType jointType in joints.Keys) //각 관절마다 2d로 변환
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position; //각 관절 3d 측정값 넣어주기
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }
                                //여기서 좌표값 조정한다.
                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position); //3d값 2d로 변환
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y); //2d로 변환한 값 배열로 넣어줌
                            }

                            if (record_btn_on)
                            {
                                record_time++;
                                record_text.Text = record_time.ToString();
                                if (record_time % 10 == 0) //10프레임마다 각 뼈의 기울기 데이터 입력
                                {

                                    // get_trainData(joints, record_time / 10);
                                    string insertQuery_xy = "INSERT INTO xy_table VALUES(" + Head_Neck_xy + ","
                                                                  + ElbowLeft_ShoulderLeft_xy + ","
                                                                  + ShoulderRight_ElbowRight_xy + ","
                                                                  + ElbowLeft_WristLeft_xy + ","
                                                                  + ElbowRight_WristRight_xy + ","
                                                                  + Neck_SpineBase_xy + ","
                                                                  + SpineBase_KneeLeft_xy + ","
                                                                  + SpineBase_KneeRight_xy + ","
                                                                  + KneeLeft_AnkleLeft_xy + ","
                                                                  + KneeRight_AnkleRight_xy + ")";

                                    string insertQuery_xz = "INSERT INTO xz_table VALUES(" + Head_Neck_xz + ","
                                                                 + ElbowLeft_ShoulderLeft_xz + ","
                                                                 + ShoulderRight_ElbowRight_xz + ","
                                                                 + ElbowLeft_WristLeft_xz + ","
                                                                 + ElbowRight_WristRight_xz + ","
                                                                 + Neck_SpineBase_xz + ","
                                                                 + SpineBase_KneeLeft_xz + ","
                                                                 + SpineBase_KneeRight_xz + ","
                                                                 + KneeLeft_AnkleLeft_xz + ","
                                                                 + KneeRight_AnkleRight_xz + ")";

                                 

                                }
                                if (record_time == 300)
                                {
                                  
                                   
                                  
                                    record_btn_on = false;
                                }

                            }

                            if (train_btn_on)
                            {
                                train_time++;
                                if (train_time % 10 == 0) //10프레임마다 각 뼈의 기울기 데이터 차이 계산
                                {
                                    int train_frame = (train_time / 10) - 1;

                                    double diff_Head_Neck_xy = (1 - (Math.Abs(Head_Neck_xy - temp_xyData[train_frame, 0]) / 360)) * 100;
                                    double diff_ElbowLeft_ShoulderLeft_xy = (1 - (Math.Abs(ElbowLeft_ShoulderLeft_xy - temp_xyData[train_frame, 1]) / 360)) * 100;
                                    double diff_ShoulderRight_ElbowRight_xy = (1 - (Math.Abs(ShoulderRight_ElbowRight_xy - temp_xyData[train_frame, 2]) / 360)) * 100;
                                    double diff_ElbowLeft_WristLeft_xy = (1 - (Math.Abs(ElbowLeft_WristLeft_xy - temp_xyData[train_frame, 3]) / 360)) * 100;
                                    double diff_ElbowRight_WristRight_xy = (1 - (Math.Abs(ElbowRight_WristRight_xy - temp_xyData[train_frame, 4]) / 360)) * 100;

                                    double diff_Neck_SpineBase_xy = (1 - (Math.Abs(Neck_SpineBase_xy - temp_xyData[train_frame, 5]) / 360)) * 100;
                                    double diff_SpineBase_KneeLeft_xy = (1 - (Math.Abs(SpineBase_KneeLeft_xy - temp_xyData[train_frame, 6]) / 360)) * 100;
                                    double diff_SpineBase_KneeRight_xy = (1 - (Math.Abs(SpineBase_KneeRight_xy - temp_xyData[train_frame, 7]) / 360)) * 100;
                                    double diff_KneeLeft_AnkleLeft_xy = (1 - (Math.Abs(KneeLeft_AnkleLeft_xy - temp_xyData[train_frame, 8]) / 360)) * 100;
                                    double diff_KneeRight_AnkleRight_xy = (1 - (Math.Abs(KneeRight_AnkleRight_xy - temp_xyData[train_frame, 9]) / 360)) * 100;


                                    string insertQuery_xy = "INSERT INTO diff_xy_table VALUES(" + diff_Head_Neck_xy + ","
                                                                  + diff_ElbowLeft_ShoulderLeft_xy + ","
                                                                  + diff_ShoulderRight_ElbowRight_xy + ","
                                                                  + diff_ElbowLeft_WristLeft_xy + ",";
                                    
                                }
                                if (train_time > 300)
                                {
                                    train_btn_on = false;
                                }
                            }


                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones) //이 뼈는 측정된 뼈가 아니라 그냥 앞에서 정의한 보편적인 뼈임
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }


        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1]; //보편적인 뼈 데이터로부터 실제 측정된 뼈 데이터를 가져옴(3d 데이터임)

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }



            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// Handles the user clicking on the data_record button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            record_btn_on = true;
        }
        /// <summary>
        /// Handles the user clicking on the ChooseTrain button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
      
        /*
        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            train_media.Play();
        }

        private void TrainButton_Click(object sender, RoutedEventArgs e)
        {
            train_btn_on = true;
        }

        /*
                private void trainButton_Click(object sender, RoutedEventArgs e)
                {
                    train_btn_on = true;
                    MyMedia.Play();
                }
                */

    }
}

