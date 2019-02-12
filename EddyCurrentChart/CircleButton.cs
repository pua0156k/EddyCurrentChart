using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing;

namespace EddyCurrentChart
{
    class CircleButton:Button
    {

        public CircleButton()
        {
           
         
            FlatStyle = FlatStyle.Flat;


        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
           
           
            GraphicsPath grpath = new GraphicsPath();
            /*System.Drawing.RectangleF  rf = new System.Drawing.RectangleF(5, 5, ClientSize.Width,ClientSize.Height);
            grpath.AddRectangle(rf);

            grpath.AddArc(20, 20, 100, 100, 180, 90);*/
            //DrawRoundRect(pevent.ClipRectangle.X, pevent.ClipRectangle.Y, pevent.ClipRectangle.Width, pevent.ClipRectangle.Height, 50);
             this.Region = new System.Drawing.Region(DrawRoundRect(pevent.ClipRectangle.X, pevent.ClipRectangle.Y, pevent.ClipRectangle.Width, pevent.ClipRectangle.Height, 50));
            //base.OnPaint(pevent);


            //Draw(pevent.ClipRectangle, pevent.Graphics, 50);
           
            base.OnPaint(pevent);

        }

        public static GraphicsPath DrawRoundRect(int x, int y, int width, int height, int radius)
        {
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(x, y, radius, radius, 180, 90);
            gp.AddArc(width - radius, y, radius, radius, 270, 90);
            gp.AddArc(width - radius, height - radius, radius, radius, 0, 90);
            gp.AddArc(x, height - radius, radius, radius, 90, 90);
            gp.CloseAllFigures();
            
            return gp;
        }
    }
}
