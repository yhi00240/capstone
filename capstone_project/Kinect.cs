using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.CSharp;
using Microsoft.Kinect;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace Capston_Project
{
    public enum ImageType
    {
        View = 0,
        Body = 1
    }

    //키넥트 다루기 클래스
    public class Kinect
    {
        public KinectSensor sensor = null;
        public MultiSourceFrameReader frameReader = null;
        public BodyFrameReader bodyFrameReader = null;
        public CoordinateMapper coordinateMapper = null;           //바디->화면 매핑
        public FrameDescription frameDescription = null;
        public int depthWidth;
        public int depthHeight;
        Form1 form;

        public Action action = null;
        Content_init content_init = null;
        Write_memo write_memo = null;
        public Body[] bodies = null;
        public Body body = null;
        public Rectangle rec;
        bool is_view = false;

        int new_function = -1;
        int old_function = -1;

        public bool is_able_grab = false;
        Game game;

        public int game_again_timer = 0;
        int change_count = 0;
        int step = 0;

        //상체에 장기 출력 너비와 높이
        int new_width;
        int new_height;

        //책이나 동영상을 취소한 후 일정시간 term 부여
        public int cancel_timer = 0;


        public Kinect(Form1 form)
        {
            this.form = form;
            action = new Action();
            action.get_kinect(this);
            write_memo = new Write_memo(form);
        }

        //키넥트 initialization
        public void kinect_init()
        {

            // Check whether there are Kinect sensors available and select the default one.
            if (KinectSensor.GetDefault() != null)
            {
                this.sensor = KinectSensor.GetDefault();

                this.coordinateMapper = this.sensor.CoordinateMapper;
                this.frameDescription = this.sensor.DepthFrameSource.FrameDescription;
                this.depthWidth = this.frameDescription.Width;
                this.depthHeight = this.frameDescription.Height;
                game = new Game(form, action, this);

                // Check that the connect was properly retrieved and is connected.
                if (this.sensor != null)
                {
                    if ((this.sensor.KinectCapabilities & KinectCapabilities.Vision) == KinectCapabilities.Vision)
                    {
                        // Open the sensor for use.
                        this.sensor.Open();

                        // Next open the multi-source frame reader.
                        this.frameReader = this.sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color);

                        // Retrieve the frame descriptions for each frame source.
                        FrameDescription colorFrameDescription = this.sensor.ColorFrameSource.FrameDescription;
                        this.frameReader.MultiSourceFrameArrived += frameReader_MultiSourceFrameArrived;

                    }
                }
                bodyFrameReader = sensor.BodyFrameSource.OpenReader();
                if (bodyFrameReader != null)
                {
                    bodyFrameReader.FrameArrived += Reader_FrameArrived;
                }
            }


        }


        //Body 프레임이 도착했을 경우 호출되는 콜백
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {

            bool deataReceived = false;
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (bodies == null)
                    {
                        bodies = new Body[bodyFrame.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(bodies);
                    deataReceived = true;
                }
            }

            if (deataReceived)
            {
                foreach (Body body in bodies)
                {
                    this.body = body;
                    if (body.IsTracked)
                    {
                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
                        Dictionary<JointType, Point_3D> joint_points = new Dictionary<JointType, Point_3D>();

                        CameraSpacePoint pos;
                        DepthSpacePoint depthSpacePt;
                        for (int i = 0; i < 25; i++)
                        {
                            pos = joints[action.JointTypePoint_String[i]].Position;
                            depthSpacePt = this.coordinateMapper.MapCameraPointToDepthSpace(pos);
                            action.points[i] = new Point_3D(depthSpacePt.X * (float)(1920.0 / this.depthWidth), depthSpacePt.Y * (float)(1080.0 / this.depthHeight), pos.Z * 100);
                        }

                        action.find_upbody();
                        action.find_average_z();
                        action.hand_state = body.HandRightState;

                        new_function = action.get_function(form, old_function);
                        if (new_function != old_function)
                        {
                            action.function_count = 0;


                            function_release(old_function);
                            function_start(new_function);

                        }

                        if (cancel() == 1)
                        {
                            function_release(old_function);
                            old_function = new_function = -1;
                        }
                        old_function = new_function;

                        switch (new_function)
                        {
                            case Define.VEDIO:
                                function_vedio_mode();
                                //Console.WriteLine("body");
                                break;

                            case Define.GAME:
                                //Console.WriteLine("game");
                                Console.WriteLine(game_again_timer);
                                if ((game.game_state == Define.GAME_FINNISH) && (body.HandRightState == HandState.Closed) && 
                                    (action.average_z - (int)(action.points[(int)Action.JointTypePoint.HandRight].Z)) > 60)
                                {
                                    //Console.WriteLine(game_again_timer);
                                    if (game_again_timer <= 0)
                                    {
                                        function_release(Define.GAME);
                                        function_start(Define.GAME);
                                        game.init();
                                        game.start();
                                        form.game_Timer(game);
                                        form.game_state.Visible = false;
                                    }
                                }
                                break;


                            case Define.WRITE:
                                //Console.WriteLine("write");
                                function_write_mode();
                                break;
                        }
                    }
                }
            }

            else
                form.sprite_image.Visible = false;
        }

        // 딕테일 Todo
        private void function_vedio_mode()
        {
            new_width = 500 - (int)(action.points[(int)(Action.JointTypePoint.SpineMid)].Z / 100 * 150);
            new_height = 600 - (int)(action.points[(int)(Action.JointTypePoint.SpineMid)].Z / 100 * 150);

            form.sprite_image.Visible = true;
            form.sprite_image.SizeMode = PictureBoxSizeMode.StretchImage;
            form.sprite_image.Location = new System.Drawing.Point((int)(action.points[(int)(Action.JointTypePoint.SpineMid)].X) - (new_width / 2) + 40, (int)action.points[(int)(Action.JointTypePoint.SpineMid)].Y - new_height / 2 + 50);

            form.sprite_image.Width = new_width;
            form.sprite_image.Height = new_height;

            action.setHandMoveStatus();
            action.takingPICTURE(form);
            action.takingOrganACTION(form);
           

            //cancel(Define.CANCEL_AVI);
        }
        public int cancel()
        {
            if (form.show_groupBox.Visible)
            {
                cancel_timer = 50;
                cancel_avi();
                return 2;
            }
            else if(form.show_groupBox_book.Visible)
            {
                cancel_timer = 50;
                cancel_avi();
                return 2;
            }

            else if(cancel_function() && cancel_timer <= 0)
            {
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!");
                cancel_timer = 0;
                return 1;
            }
            return -1;
        }

        public void cancel_avi()
        {
            Point_3D p = action.points[(int)Action.JointTypePoint.HandTipLeft];
            if (body.HandLeftState == Microsoft.Kinect.HandState.Closed && action.average_z - p.Z > 70)
            {
                Console.WriteLine("cancel");
                form.show_groupBox.Visible = false;
                form.show_groupBox_book.Visible = false;
                form.show_detail_avi_avi.Ctlcontrols.stop();
            }
        }
        public bool cancel_function()
        {
            Point_3D p = action.points[(int)Action.JointTypePoint.HandTipLeft];
            if (body.HandLeftState == Microsoft.Kinect.HandState.Closed && action.average_z - p.Z > 70)
            {
                return true;
            }
            return false;
        }

        public bool cancel_write()
        {
            Point_3D p = action.points[(int)Action.JointTypePoint.HandTipLeft];
            if (body.HandLeftState == Microsoft.Kinect.HandState.Closed && action.average_z - p.Z > 70)
            {
                return true;
            }
            return false;
        }
        public void Screen_shout()
        {
            
            string str = null;

            //키넥트 영상 부분의 원본 이미지를 복사
            Bitmap bitmap = (Bitmap)form.kinect_view.Image.Clone();

            //최종적으로 저장될 스크린샷 bitmap
            var target = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
            var graphics = Graphics.FromImage(target);
            graphics.CompositingMode = CompositingMode.SourceOver;  //투명 배경을 가진 장기 이미지들을 합치기 위함.

            //원본 이미지를 먼저 복사
            graphics.DrawImage(bitmap, 0, 0);

            //현재 그려져 있는 장기들은 모두 그리도록 한다.
            //상체 장기의 경우, 크기가 동적으로 계속 변하므로, 마지막 크기였던 new_width와 new_height에 맞게 resize 시켜서 합치도록 한다.
            if(form.sprite_image.Visible == true)
            {
                graphics.DrawImage(form.sprite_image.Image, (int)(action.points[(int)(Action.JointTypePoint.SpineMid)].X) - (new_width / 2) + 60, (int)action.points[(int)(Action.JointTypePoint.SpineMid)].Y - new_height / 2 + 50, new_width, new_height);
            }

            //심장, 간, 폐는 크기가 정적이므로 그대로 그린다.
            if(action.OrganPick_3_heart && form.function_vedio_img_heart.Visible == true)
            {
                graphics.DrawImage(form.function_vedio_img_heart.Image, (int)action.points[(int)Action.JointTypePoint.HandRight].X - Action.HEART_LOCATION_X_OFFSET, (int)action.points[(int)Action.JointTypePoint.HandRight].Y - Action.HEART_LOCATION_Y_OFFSET);
            }

            else if (action.OrganPick_3_lung && form.function_vedio_img_lung.Visible == true)
            {
                graphics.DrawImage(form.function_vedio_img_lung.Image, (int)action.points[(int)Action.JointTypePoint.HandRight].X - Action.LUNG_LOCATION_X_OFFSET, (int)action.points[(int)Action.JointTypePoint.HandRight].Y - Action.LUNG_LOCATION_Y_OFFSET);
            }

            else if (action.OrganPick_3_digest && form.function_vedio_img_digest.Visible == true)
            {
                graphics.DrawImage(form.function_vedio_img_digest.Image, (int)action.points[(int)Action.JointTypePoint.HandRight].X - Action.DIGEST_LOCATION_X_OFFSET, (int)action.points[(int)Action.JointTypePoint.HandRight].Y - Action.DIGSET_LOCATION_Y_OFFSET);
            }

           
            //현재 시간으로 저장해서 스샷 이름이 겹치지 않도록 함, 순서대로 저장 가능
            str = "screenshot_";
            str += System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            str += ".png";
            //bitmap.Save(str, System.Drawing.Imaging.ImageFormat.Png);

            //전체 사진 중, 일부 영역만을 잘라내어 저장한다.
            Rectangle cropRect = new Rectangle(450, 0, 1120, 1000);
            Bitmap cropped = target.Clone(cropRect, bitmap.PixelFormat);

            cropped.Save(str, ImageFormat.Png);
            //target.Save(str, ImageFormat.Png);

            Console.WriteLine("캡쳐 완료");
            Console.WriteLine(str);


            //최근 캡쳐한 이미지명을 적용
            form.lastCapture = str;

            //스크린샷이 찍힐 때마다 write_memo에서 사용될 캔버스의 이미지가 변경된다.
            CaptureMoniter(form);


            //G.Dispose();
            bitmap.Dispose();
            target.Dispose();

        }

        // 캡쳐된 화면 출력하기 pictureBox3 이용
        public void CaptureMoniter(Form1 form)
        {
            form.ScreenShot.Visible = true;
            form.ScreenShot.SizeMode = PictureBoxSizeMode.StretchImage;
            //form.ScreenShot.Location = new System.Drawing.Point(250, 250);
            form.ScreenShot.Location = new System.Drawing.Point(0, 0);
            //form.ScreenShot.Size = new System.Drawing.Size(590, 430);
            //form.ScreenShot.Size = new System.Drawing.Size(650, 500);
            form.ScreenShot.Size = new System.Drawing.Size(1120, 1000);
            
            form.ScreenShot.Image = System.Drawing.Image.FromFile(form.lastCapture);//스샷 작게 보여주는 픽쳐박스
            //form.Drawbox.Image = System.Drawing.Image.FromFile(str);    //그림판의 배경을 스샷찍을거로 바꿈
        }


        private void function_write_mode()
        {
            // point는 정해져있고 기준값으로 차 값만 큼 point에 더해준다.
            int hand_state = action.is_write_memo(body.HandRightState);
            Point_3D p_tip = action.points[(int)Action.JointTypePoint.HandTipRight];
            //Point p = new Point((int)p_tip.X - 500, (int)p_tip.Y + 100);
            //Point p = new Point((int)p_tip.X - 400, (int)p_tip.Y + 300);
            Point p = new Point((int)p_tip.X - 600, (int)p_tip.Y + 100);



            if (hand_state == Define.HAND_CLOSE)
            {
                if(195 < p.X && p.X < 295 && 100 < p.Y && p.Y < 200){
                    write_memo.pen = new Pen(Color.Red, 5);
                }
                else if (340 < p.X && p.X < 440 && 100 < p.Y && p.Y < 200)
                {
                    write_memo.pen = new Pen(Color.Orange, 5);
                }
                else if (495 < p.X && p.X < 595 && 100 < p.Y && p.Y < 200)
                {
                    write_memo.pen = new Pen(Color.Yellow, 5);
                }
                else if (630 < p.X && p.X < 730 && 100 < p.Y && p.Y < 200)
                {
                    write_memo.pen = new Pen(Color.Green, 5);
                }
                else if (795 < p.X && p.X < 895 && 100 < p.Y && p.Y < 200)
                {
                    write_memo.pen = new Pen(Color.Blue, 5);
                }
                Console.WriteLine("글 쓰기가 가능합니다.");
                write_memo.writing(p);
            }
            else if (hand_state == Define.HAND_OPEN)
            {
                Console.WriteLine(p.X + "~~~" + p.Y);
                write_memo.old_pos = p;
                write_memo.new_pos = p;
                Point cursor_pos = p;
                cursor_pos.X = p.X + 400;
                cursor_pos.Y = p.Y + 100;
                Cursor.Position = cursor_pos;
            }
            
            if (cancel_write())
            {
                Console.WriteLine("나가기");
                function_release(Define.WRITE);
                //write_memo.removing();
            }
        }

        private void function_start(int new_function)
        {
            switch (new_function)
            {
                case Define.VEDIO:
                    form.sprite_image.Visible = true;
                    form.sprite_timer.Enabled = true;
                    form.function_vedio_box.Visible = true;
                    form.function_vedio_img_book.Visible = true;        //  책 영역 visible
                    form.function_vedio_img_camera.Visible = true;      //  카메라 영역 visible

                    //TV 위치 지정
                    form.function_vedio_box.Location = new System.Drawing.Point(Action.TV_LOCATION_X, Action.TV_LOCATION_Y);
                    action.TV_rect = new Rectangle(Action.TV_RECT_X, Action.TV_RECT_Y, form.function_vedio_box.Width - 20, form.function_vedio_box.Height - 20);
                    //책 위치 지정
                    form.function_vedio_img_book.Location = new System.Drawing.Point(Action.BOOK_LOCATION_X, Action.BOOK_LOCATION_Y);
                    action.Book_rect = new Rectangle(Action.BOOK_RECT_X, Action.BOOK_RECT_Y, form.function_vedio_img_book.Width - 20, form.function_vedio_img_book.Height - 20);
                    //카메라 위치 지정
                    form.function_vedio_img_camera.Location = new System.Drawing.Point(Action.CAMERA_LOCATION_X, Action.CAMERA_LOCATION_Y);
                    action.Camera_rect = new Rectangle(Action.CAMERA_LOCATION_X + Action.CAMERA_FIX - 100, Action.CAMERA_LOCATION_Y, form.function_vedio_img_camera.Width, form.function_vedio_img_camera.Height);
                    //카메라 카운트다운 위치 지정
                    form.function_vedio_img_cameraCnt.Location = new System.Drawing.Point(Action.CAMERA_LOCATION_X + 130, Action.CAMERA_LOCATION_Y - 70);
                    

                    break;

                case Define.GAME:

                    form.game_bottom.Visible = true;
                    game.init();
                    game.start();
                    form.game_Timer(game);

                    break;


                case Define.WRITE:
                    form.write_view.BringToFront();
                    form.ScreenShot.BringToFront();
                    form.ScreenShot.Visible = true;
                    form.write_view.Visible = true;
                    //form.write_pen.Visible = true;
                    break;
            }

        }
        private void function_release(int old_function)
        {
            switch (old_function)
            {
                case Define.VEDIO:
                    form.sprite_image.Visible = false;
                    form.sprite_timer.Enabled = false;
                    form.function_vedio_box.Visible = false;
                    form.function_vedio_img_heart.Visible = false;
                    form.function_vedio_img_digest.Visible = false;
                    form.function_vedio_img_lung.Visible = false;
                    form.function_vedio_img_grab.Visible = false;
                    form.function_vedio_img_camera.Visible = false;
                    form.function_vedio_img_book.Visible = false;
                    form.function_vedio_img_cameraCnt.Visible = false;
                    form.Camera_countdown_textbox.Visible = false;
                    action.releaseStates();     //  모든 장기체험 상태를 초기화시킨다.

                    break;

                case Define.GAME:
                    for (int i = 0; i < 15; i++)
                    {
                        game.images[i].Visible = false;
                    }
                    form.game_timer.Enabled = false;
                    form.crush_timer.Enabled = false;
                    form.game_state.Visible = false;
                    form.game_bottom.Visible = false;

                    form.game_life_block1.Visible = false;
                    form.game_life_block2.Visible = false;
                    form.game_life_block3.Visible = false;
                    form.game_life_block4.Visible = false;
                    form.game_life_block5.Visible = false;

                    break;


                case Define.WRITE:
                    form.ScreenShot.Visible = false;
                    form.write_view.Visible = false;
                    
                    form.write_view.Refresh();
                    form.ScreenShot.SendToBack();
                    form.write_view.SendToBack();
                    //write_memo.removing();
                    //form.write_pen.Visible = false;
                    break;
            }

        }

        public void show_avi(int mode)
        {

            Console.WriteLine("AA");
            switch (mode)
            {
                case Define.HEART_AVI:
                    //test
                    //form.show_groupBox.Visible = true;
                    //form.show_detail_background.Image = Image.FromFile("show_upper/show_tv.png");
                    form.show_detail_avi_avi.uiMode = "none";
                    form.show_detail_avi_avi.URL = "show_upper/heart_avi.avi";
                    form.show_detail_avi_avi.Ctlcontrols.play();
                    break;
                case Define.LUNG_AVI:
                    //form.show_groupBox.Visible = true;
                    //form.show_detail_background.Image = Image.FromFile("show_upper/show_tv.png");
                    form.show_detail_avi_avi.uiMode = "none";
                    form.show_detail_avi_avi.URL = "show_upper/breath_avi.avi";
                    form.show_detail_avi_avi.Ctlcontrols.play();
                    break;
                case Define.DIGEST_AVI:
                    //form.show_groupBox.Visible = true;
                    //form.show_detail_background.Image = Image.FromFile("show_upper/show_tv.png");
                    form.show_detail_avi_avi.uiMode = "none";
                    form.show_detail_avi_avi.URL = "show_upper/digest_avi.avi";
                    form.show_detail_avi_avi.Ctlcontrols.play();
                    break;
            }

        }

        private void frameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (is_view)
            {
                is_view = false;
                return;
            }
            is_view = true;
            // Try to get the frame from its reference.
            try
            {
                MultiSourceFrame frame = e.FrameReference.AcquireFrame();

                if (frame != null)
                {
                    try
                    {
                        ColorFrameReference colorFrameReference = frame.ColorFrameReference;
                        useRGBAImage(colorFrameReference);
                    }
                    catch (Exception)
                    {
                        // Don't worry about exceptions for this demonstration.
                    }
                }
            }
            catch (Exception)
            {
                // Don't worry about exceptions for this demonstration.
            }
        }

        private void useRGBAImage(ColorFrameReference frameReference)
        {
            // Actually aquire the frame here and check that it was properly aquired, and use it again since it too is disposable.
            ColorFrame frame = frameReference.AcquireFrame();

            if (frame != null)
            {
                Bitmap outputImage = null;
                System.Drawing.Imaging.BitmapData imageData = null;
                // Next get the frame's description and create an output bitmap image.
                FrameDescription description = frame.FrameDescription;

                outputImage = new Bitmap(description.Width, description.Height, PixelFormat.Format32bppArgb);

                // Next, we create the raw data pointer for the bitmap, as well as the size of the image's data.
                imageData = outputImage.LockBits(new Rectangle(0, 0, outputImage.Width, outputImage.Height),
                    ImageLockMode.WriteOnly, outputImage.PixelFormat);
                IntPtr imageDataPtr = imageData.Scan0;
                int size = imageData.Stride * outputImage.Height;

                using (frame)
                {
                    // After this, we copy the image data directly to the buffer.  Note that while this is in BGRA format, it will be flipped due
                    // to the endianness of the data.
                    if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
                    {
                        frame.CopyRawFrameDataToIntPtr(imageDataPtr, (uint)size);
                    }
                    else
                    {
                        frame.CopyConvertedFrameDataToIntPtr(imageDataPtr, (uint)size, ColorImageFormat.Bgra);
                    }
                }
                // Finally, unlock the output image's raw data again and create a new bitmap for the preview picture box.
                outputImage.UnlockBits(imageData);
                form.kinect_view.Image = outputImage;
            }
            else
            {
                Console.WriteLine("Lost frame");
            }
        }
    }
}