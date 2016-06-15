using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Capston_Project
{
    public class Game
    {
        public PictureBox[] images = new PictureBox[15];
        //List<bug> bugList = new List<bug>();

        //int img_count = 15;
        //int current_index = 0;
        //int totalbBugNum = 1;
        //int showImageCount = 0;
        //int game_catchBugNum = 0;
        //int game_passBugNum = 0;
        List<bug> bugList;

        int img_count = 15;
        int current_index = 0;
        int totalbBugNum = 1;
        int showImageCount = 0;
        public int game_catchBugNum = 0;
        public int game_passBugNum = 0;
        int life = 5;
        public int game_state = -1;

        Bitmap deadVirus = null;   

        Form1 form;
        Action action;
        Kinect kinect;
        public Game(Form1 form, Action action, Kinect kinect)
        {
            this.form = form;
            this.action = action;
            this.kinect = kinect;
            this.deadVirus = new Bitmap("game/dead_virus_img.png"); 
            for (int i = 0; i < img_count; i++)
            {
                string str = "game_image" + i.ToString();
                images[i] = (PictureBox)form.Controls[str];
                images[i].Image = Image.FromFile("game/" + str + ".png");
                images[i].Width = 100;
                images[i].Height = 100;
            }
        }
        public void init()
        {
            bugList = new List<bug>();
            img_count = 15;
            current_index = 0;
            totalbBugNum = 1;
            showImageCount = 0;
            game_catchBugNum = 0;
            game_passBugNum = 0;
            life = 5;
            game_state = Define.GAME_ING;
            form.game_life_block1.Visible = false;
            form.game_life_block2.Visible = false;
            form.game_life_block3.Visible = false;
            form.game_life_block4.Visible = false;
            form.game_life_block5.Visible = false;

            
            for (int i = 0; i < img_count; i++)
            {
                bugList.Add(new bug());
            }
        }

        public void start()
        {
            images[0].Visible = bugList[0].visible = true;
            images[0].Location = bugList[0].loc = new Point(start_x_y(), form.kinect_view.Location.Y - 100);
            images[0].Parent = form.kinect_view;

        }
        public int start_x_y()
        {
            Random random = new Random();
            int ranNum = random.Next(form.kinect_view.Location.X + 500, form.kinect_view.Location.X + 1500); // 범위 변경 가능
            return ranNum; // 시작 점
        }
        public void update()
        {
            Point temp_p;

            int ran_length; // 랜덤으로 변하는 길이가 얼마냐
            int ran_direct; // 랜덤으로 x축 방향 +로 해줄거냐, -로 해줄거냐
            int tmp_x;

            for (int i = 0; i < img_count; i++)
            {
                temp_p = bugList[i].loc;

                // update전 충돌검사했는데 충돌한 것
                if (bugList[i].visible == false && bugList[i].crush == true)
                {
                    images[i].Visible = false;
                    bugList[i].crush = false;
                    game_catchBugNum++;
                    
                    //  Console.WriteLine("잡은 숫자 : " + game_catchBugNum);      
                }
                // update전 충돌도 안했고, 이미지가 화면에 보이는 상태일 때
                else if (bugList[i].visible == true && bugList[i].crush == false)
                {
                    /* 범위검사 해주기 */
                    // 우선 update_p 할 숫자찾기
                    Random random = new Random();
                    //ran_length = random.Next(30, 80); //30에서 80사이로 x,y값 길이 변함
                    ran_length = random.Next(20, 40); //30에서 80사이로 x,y값 길이 변함
                    ran_direct = random.Next(0, 100); //값이 50이하면 -방향, 값이 51이상이면 +방향
                    if (ran_direct < 51)
                    {
                        //여기서 다시 범위 넘는지 확인한 후, 범위 넘으면 방향을 바꿔주면 된다.
                        tmp_x = temp_p.X - ran_length;
                        if (tmp_x < form.kinect_view.Location.X + 500) //범위 넘으면
                        {
                            tmp_x = temp_p.X + ran_length; //반대방향으로 돌린다.
                        }
                    }
                    else
                    {
                        //여기서 다시 범위 넘는지 확인한 후, 범위 넘으면 방향을 바꿔주면 된다.
                        tmp_x = temp_p.X + ran_length;
                        if (tmp_x > form.kinect_view.Location.X + 1500) //범위 넘으면 (여기서 500값은 나중에 조절하면된다.)
                        {
                            tmp_x = temp_p.X - ran_length; //반대방향으로 돌린다.
                        }
                    }

                    // 범위검사 한다음에 temp에다 다시 넣기
                    temp_p.X = tmp_x;
                    temp_p.Y += ran_length; //여기서는 y값 검사만. 넘었으면 못잡은 것임

                    if (temp_p.Y > form.kinect_view.Location.Y + 1100) // 만약 못 잡고 pass한 상태이면 
                    {
                        bugList[i].visible = images[i].Visible = false;
                        game_passBugNum++; // 못잡았으므로 pass한 bug수 늘어난다.
                        //Console.WriteLine(game_passBugNum);
                        switch (game_passBugNum)
                        {
                            case 1:
                                form.game_life_block1.BringToFront();
                                form.game_life_block1.Visible = true;
                                break;
                            case 2:
                                form.game_life_block2.BringToFront();
                                form.game_life_block2.Visible = true;
                                break;
                            case 3:
                                form.game_life_block3.BringToFront();
                                form.game_life_block3.Visible = true;
                                break;
                            case 4:
                                form.game_life_block4.BringToFront();
                                form.game_life_block4.Visible = true;
                                break;
                            case 5:
                                form.game_life_block5.BringToFront();
                                form.game_life_block5.Visible = true;
                                break;
                        }
                    }
                    else
                    {
                        bugList[i].loc = images[i].Location = temp_p;
                    }
                }
                else { } // 생략해도 되고 안해도 되고
            }
            showImageCount++;
            if (showImageCount == 10) // 3초마다 갱신해준다. 난이도에 따라서 다르게 설정할 수 있다.
            {
                showImageCount = 0;
                if (showImageCount < 10 - 1)
                {
                    add_image();
                }
            }

            if (game_catchBugNum > 10)
            {
                Console.WriteLine("win");
                form.game_state.Visible = true;
                form.game_state.Image = Image.FromFile("game/win.png");
                gameOver();
            }
            if (game_passBugNum > 5)
            {
                Console.WriteLine("over");
                form.game_state.Visible = true;
                form.game_state.Image = Image.FromFile("game/over.png");
                gameOver();
            }
        }
        public void crush_check()
        {
            ////////////// 충돌 체크하자!
            //Console.WriteLine("state = " + action.hand_state.ToString());

            for (int i = 0; i < img_count; i++)
            {
                // 손의 네모에 벌레의 위치값이 들어가면 충돌했다. 벌레가 충돌하는 모습을 한자로 읽으면 충충돌(蟲衝突)
                if (bugList[i].visible == true &&
                    Intersect(bugList[i].loc, (int)(action.points[(int)Action.JointTypePoint.HandRight].X),
                                                      (int)(action.points[(int)Action.JointTypePoint.HandRight].Y)) &&
                      //Intersect(bugList[i].loc, (int)(action.points[(int)Action.JointTypePoint.HandTipRight].X),
                      //                                  (int)(action.points[(int)Action.JointTypePoint.HandTipRight].Y)) &&
                    action.hand_state == Microsoft.Kinect.HandState.Closed) // 충돌하면
                {
                    bugList[i].visible = false; // 아직 값 update 되기전이므로 image.visible은 하지 않고 기다린다.
                    bugList[i].crush = true;    // 충돌 했으므로 crush 체크를 해준다.
                }

            }
        }

        public bool Intersect(Point bugP, int hand_x, int hand_y)
        {
            // 손의 네모값에 벌레의 위치값이 들어가면
            //if (hand_x - 50 <= bugP.X + 25 && bugP.X + 25 <= hand_x + 50 && hand_y - 50 <= bugP.Y + 25 && bugP.Y + 25 <= hand_y + 50)
            //if (hand_x - 50 <= bugP.X + 50 && bugP.X + 50 <= hand_x + 50 && hand_y - 50 <= bugP.Y + 50 && bugP.Y + 50 <= hand_y + 50)
            if (bugP.X <= hand_x && hand_x <= bugP.X + 100 && bugP.Y <= hand_y && hand_y <= bugP.Y + 100)
            {
                return true;
            }
            return false;
        }
        public void add_image()
        {
            if (current_index == img_count - 1) // current_index가 만들어놓은 picturebox 개수 넘으면 다시 0으로 바꿔준다.
            {
                current_index = -1;
            }
            current_index++;
            totalbBugNum++;
            // 값 초기화한다.
            bugList[current_index].visible = images[current_index].Visible = true;
            bugList[current_index].loc = images[current_index].Location = new Point(start_x_y(), form.kinect_view.Location.Y - 100);
            bugList[current_index].crush = false;
            images[current_index].Parent = form.kinect_view;
        }
        public void gameOver()
        {
            form.game_timer.Enabled = false;
            form.crush_timer.Enabled = false;
            game_state = Define.GAME_FINNISH;
            kinect.game_again_timer = 60;
            // Console.WriteLine("game_passBugNum" + game_passBugNum);
        }

    }
    class bug
    {
        public Point loc; // 현재 좌표
        public bool visible; // 현재 화면에 나와있냐
        public bool crush; // 벌레 부딪혔냐? (crush함수는 update함수보다 타이머를 더 빠르게 잡아주어야 하기때문에 필요)



        public static int INIT_POS = -8888;     

        public bug()
        {
            loc = new System.Drawing.Point(INIT_POS, INIT_POS);
            visible = false;
            crush = false;


        }
    }
}