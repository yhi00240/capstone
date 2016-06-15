using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Capston_Project
{
    public class Write_memo
    {
        Form1 form;
        public Point old_pos;
        public Point new_pos;
        public Point cursor_pos;
        Graphics G;
        public Pen pen;

        public Write_memo(Form1 form)
        {
            this.form = form;          
            form.write_view.Image = Image.FromFile("write/background.png");
            form.ScreenShot.Parent = form.write_view;
            old_pos = new Point(50, 50);
            pen = new Pen(Color.Black, 5);
            
        }

        public void writing(Point point)
        {
            
            //form.write_pen.BringToFront();
            old_pos = new_pos;
            new_pos = point;

            cursor_pos.X = new_pos.X + 400;
            cursor_pos.Y = new_pos.Y + 100;

            Cursor.Position = cursor_pos;

            pen.StartCap = pen.EndCap = LineCap.Round;
            G = form.ScreenShot.CreateGraphics();
            
            G.DrawLine(pen, old_pos.X, old_pos.Y, new_pos.X, new_pos.Y);
            G.Dispose();
            
        }
        public void set_cursor_pos(Point point)
        {
           
        }
        public void removing()
        {
            form.write_view.Refresh();

            form.write_view.Visible = false;
            form.write_view.SendToBack();
        }
    }
}
