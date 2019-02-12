using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EddyCurrentChart
{
    public partial class Form1 : Form
    {
        int SrcWidth = 800, SrcHeight = 800;
        private bool sending;
        Graphics g;
        Graphics gs;
        List<Point> arrLines = new List<Point>();
        Region smallRegion = new Region(new Rectangle(350, 500, 300, 300));
        Stopwatch st = new Stopwatch();
        Bitmap bmp = new Bitmap(800, 871);
        Random rand = new Random();
        int k = 0,j=0,ak=0;
        private static IntPtr hdcDest;
        private static IntPtr hdcSrc;
        private static int w,h;
        private static Bitmap _Bitmap;
        private static IntPtr hOldObject;
        private static BITMAPINFO info;
        private static IntPtr lpvbit;
        private static Graphics grSrc;
        private IntPtr bmpPtr;
        Pen mypen = new Pen(Brushes.DeepSkyBlue, 1);






        //Bitblt
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(
              IntPtr hdcDest, //目标设备的句柄
              int nXDest,     // 目标对象的左上角的X坐标
              int nYDest,     // 目标对象的左上角的X坐标
              int nWidth,     // 目标对象的矩形的宽度
              int nHeight,    // 目标对象的矩形的长度
              IntPtr hdcSrc,  // 源设备的句柄
              int nXSrc,      // 源对象的左上角的X坐标
              int nYSrc,      // 源对象的左上角的X坐标
              int dwRop       // 光栅的操作值
              );
        public const int SRCCOPY = 0xCC0020;
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdcPtr);
        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern IntPtr SelectObject(IntPtr hdcPtr, IntPtr hObject);
        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern bool DeleteDC(IntPtr hdcPtr);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);


        [DllImport("gdi32")]
        private extern static int SetDIBitsToDevice(HandleRef hDC, int xDest, int yDest, int dwWidth, int dwHeight, int XSrc, int YSrc, int uStartScan, int cScanLines, IntPtr lpvBits, ref BITMAPINFO lpbmi, uint fuColorUse);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public int bihSize;
            public int bihWidth;
            public int bihHeight;
            public short bihPlanes;
            public short bihBitCount;
            public int bihCompression;
            public int bihSizeImage;
            public double bihXPelsPerMeter;
            public double bihClrUsed;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER biHeader;
            public int biColors;
        }

        private BITMAPINFO bitmapInfo = new BITMAPINFO
        {
            biHeader =
            {
                bihBitCount = 32,
                bihPlanes = 1,
                bihSize = 40,
                bihWidth = 1,
                bihHeight = 1,
                bihSizeImage = 1
            }
        };

        private Size size = new Size(1, 1);
        private int[] array = new int[1];
        private Task task = null;
        private bool IsTerminate = false;
        private HandleRef hRef;
        private Graphics graphics;
        private IntPtr hdc;
       int _width;
       int _height;
       int[] _pArray;
        GCHandle _gcHandle;
        BITMAPINFO _BI;
        int height = 418, width = 820;

        //
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bmp = new Bitmap(SrcWidth, SrcHeight);
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true); //默認啟動雙緩衝
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除
            g = Graphics.FromImage(bmp);
            gs = this.CreateGraphics();

            g.Clear(Color.White);
          
        }
        private SerialPort CreateComport(SerialPort port)
        {

            serialPort1 = new SerialPort("COM7", 9600, Parity.Even, 7, StopBits.One);

            return port;
        }
        private Boolean OpenComport(SerialPort port)
        {
            try
            {
                if ((port != null) && (!port.IsOpen))
                {
                    port.Open();
                    /*      this.Invoke(new Action(() => {
                              barStaticItem_PlcConnect.Caption = rm.GetString("PlcConnectionSuccess");
                              barStaticItem_PlcConnect.ItemAppearance.Normal.ForeColor = Color.Blue;
                          }));*/
                    System.Diagnostics.Debug.Print("序列埠開啟成功!");
                }
                return true;
            }
            catch (Exception ex)
            {
                /*    AddMsg(String.Format("Plc連線出現例外:{0}", ex.ToString()));
                    this.Invoke(new Action(() => {
                        barStaticItem_PlcConnect.Caption = rm.GetString("PlcConnectionFail");
                        barStaticItem_PlcConnect.ItemAppearance.Normal.ForeColor = Color.Red;
                    }));*/

                System.Diagnostics.Debug.Print("序列埠開啟失敗!\n" + ex.ToString());
                return false;
            }
        }
        bool SetupPlc()
        {
            try
            {

                serialPort1.BaudRate = 9600;
                serialPort1.Parity = Parity.None;
                serialPort1.DataBits = 8;
                serialPort1.StopBits = StopBits.One;
                serialPort1.PortName = "COM10";
                serialPort1.Open();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("連接埠設置失敗!");
                return false;
            }
        }
        //關閉序列埠
        private void CloseComport(SerialPort port)
        {
            try
            {
                if ((port != null) && (port.IsOpen) && (!sending))
                {
                    port.Close();
                    //      AddMsg("PLC序列埠已關閉");
                }
            }
            catch (Exception ex)
            {
                //這邊你可以自訂發生例外的處理程序
                //AddMsg(String.Format("Plc序列埠關閉例外:{0}", ex.ToString()));
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {

            ThreadPool.QueueUserWorkItem(o => {

                int j = 0;
                int i = 0;
                Random rand = new Random();
                while (true)
                {
                    i++;
                    /*this.Invoke(new Action(() => {
                        chartControl1.Series[0].Points.AddPoint(i * 2, rand.Next(20, 40));
                       if (chartControl1.Series[0].Points.Count > 100)
                        {
                            chartControl1.Series[0].Points.RemoveAt(0);
                            GC.Collect();

                        }
                    }));*/
                }




            });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            bool da = SetupPlc();
            Debug.Print(da.ToString());

        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string str = serialPort1.ReadTo("**");
            st.Stop();
            Debug.Print(st.Elapsed.TotalMilliseconds.ToString());
            st.Restart();
      
           // strChange(str);
          /*  byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
             for (int i = 0; i < bytes.Length; i++)
             {

                 str16 += Convert.ToString(bytes[i], 16);
             }
            str16 = Convert.ToString(bytes[1], 16) + Convert.ToString(bytes[0], 16) + Convert.ToString(bytes[3], 16) + Convert.ToString(bytes[2], 16);
            string aa = Convert.ToString(bytes[1], 16) + Convert.ToString(bytes[0], 16);
            string ab = Convert.ToString(bytes[3], 16) + Convert.ToString(bytes[2], 16);

            int ac = Convert.ToInt32(aa,16);
            int ad = Convert.ToInt32(ab,16);*/
           //this.Invoke(new Action(() => {
               
                /* listBox1.Items.Add(ac);
                listBox1.Items.Add(ad);
                listBox1.Items.Add("-----");
                if(ac <1000||ad<10000)
                {
                    listBox2.Items.Add(aa);
                    listBox2.Items.Add(ab);
                    listBox2.Items.Add("-----");
                }*/
          //  }));
            // str16 += Convert.ToString(Convert.ToInt32(bytes[i]), 16);

            //Debug.Print(Convert.ToInt32(str, 16).ToString());
           // System.Diagnostics.Debug.Print(str);


        }
        /*  public unsafe static void DrawImageHighSpeedtoDevice()
          {
              SetDIBitsToDevice(hdcDest, 0, 0, (uint)w, (uint)h, 0, 0, 0,
              (uint)h, data_ptr, ref info, DIB_RGB_COLORS);
          }*/
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            serialPort1.Close();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {

            /* g = this.CreateGraphics();
            //创建两个点
            Point n1 = new Point(20, 20);
            Point n2 = new Point(100, 100);

            //创建画笔
            Pen p = new Pen(Brushes.Black);
            g.DrawLine(p, n1, n2);*/

           
            Image image = new Bitmap(820, 418);//定义图象大小
             g = this.CreateGraphics();
            //Graphics.FromImage(image); //定义gdi绘图对象
            Pen mypen = new Pen(Brushes.DeepSkyBlue, 1);
            int height = 418, width = 820;
            g.Clear(Color.White);//清除整个绘图面并以指定背景色填充。 
                                 //g.FillRectangle(Brushes.WhiteSmoke, 0, 0, width, height);//填充由一对坐标、一个宽度和一个高度指定的矩形的内部。 
                                 //画边框
            g.DrawRectangle(mypen, 0, 0, width - 9, height +390);//绘制由坐标对、宽度和高度指定的矩形。 
            g.DrawLine(new Pen(Brushes.Black, 1), 40, height +370, width - 40, height +370);//x轴
            g.DrawLine(new Pen(Brushes.Black, 1), width - 50, height +360, width - 40, height +370);//箭头
            g.DrawLine(new Pen(Brushes.Black, 1), width - 50, height +360, width - 40, height +370);
            g.DrawLine(new Pen(Brushes.Black, 1), 40, height + 370, 40, 40);//y轴
            g.DrawLine(new Pen(Brushes.Black, 1), 30, 50, 40, 40);
            g.DrawLine(new Pen(Brushes.Black, 1), 50, 50, 40, 40);
            g.DrawString("頻率", new Font("微軟正黑體", 16), Brushes.Green, new PointF(300, 20));
            g.DrawString("電壓值", new Font("Arial", 16), Brushes.Aqua, new PointF(50, 40));
            g.DrawString("頻率", new Font("微軟正黑體", 16), Brushes.Aqua, new PointF(width - 90, height +370));
            g.FillEllipse(Brushes.Black, 500, 500, 10, 10);



        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Invalidate();
            //g.FillEllipse(Brushes.Black, Convert.ToInt32(textBox1.Text),Convert.ToInt32(textBox2.Text), 10, 10);
            st.Restart();
            strChange(":4xV");
       
            
        }
         void strChange(string str)
        {


            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
            if(bytes.Length<4)
                return;
            
          //  string aa = Convert.ToString(bytes[1], 16) + Convert.ToString(bytes[0], 16);
          //  string ab = Convert.ToString(bytes[3], 16) + Convert.ToString(bytes[2], 16);
            int ax = Convert.ToInt32(Convert.ToString(bytes[1], 16) + Convert.ToString(bytes[0], 16), 16);
            int ay = Convert.ToInt32(Convert.ToString(bytes[3], 16) + Convert.ToString(bytes[2], 16), 16);
            // Debug.Print("X : " + ax + "  Y : " + ay);
            if (ax < 10000 || ay < 10000)
                return;
           // Point point = new Point(ax/30, ay/30);
            Point point = new Point((k*5+3),(j*5+3));
            if (k < 100)
            { k++;

            }
            else
            { k = 0;
                j++;
            }
            arrLines.Add(point);
            if (arrLines.Count > 100)
            {
                arrLines.RemoveAt(0);

                bmp = new Bitmap(SrcWidth, SrcHeight);
                using (Graphics g = Graphics.FromImage(bmp))
                {//g.Clear(Color.White);
                 //redraw(g);
                    SetDlbDraw(g);
                    bmp.Dispose();
                }
                //g.Dispose();
                //if(_gcHandle.IsAllocated)
                //_gcHandle.Free();


            }
            else
            {
                SetDlbDraw(g);
                // redraw(g);
            }
            /*   foreach(var item in arrLines)
               {
                   g.FillEllipse(Brushes.Black, item.X,item.Y, 5, 5);

               }
               Stopwatch st = new Stopwatch();
               st.Restart();
               pictureBox1.CreateGraphics().DrawImage(bmp, 0, 0);
                     st.Stop();
                   Debug.Print(st.Elapsed.TotalMilliseconds.ToString());*/
        }
        private void SetDlbDraw(Graphics g)
        {
            //initSetDlb();
            // Stopwatch st = new Stopwatch();
            // st.Restart();

            int minX = 999, maxX = 0;
            int minY = 999, maxY = 0;
            int X_Width=0, Y_Height = 0;
            
           foreach (var item in arrLines)
           {
               g.FillEllipse(Brushes.Red, item.X, item.Y, 5, 5);
                if (minX > item.X)
                    minX = item.X;
                if (maxX < item.X)
                    maxX = item.X;
                if (minY > item.Y)
                    minY = item.Y;
                if (maxY < item.Y)
                    maxY = item.Y;
           }
            X_Width = maxX - minX;
            Y_Height = maxY - minY;
           g.DrawRectangle(mypen, 0, 0, width - 9, height + 390);//绘制由坐标对、宽度和高度指定的矩形。 
           g.DrawLine(mypen, 40, height + 370, width - 40, height + 370);//x轴
           g.DrawLine(mypen, width - 50, height + 360, width - 40, height + 370);//箭头
           g.DrawLine(mypen, width - 50, height + 360, width - 40, height + 370);
           g.DrawLine(mypen, 40, height + 370, 40, 40);//y轴
           g.DrawLine(mypen, 30, 50, 40, 40);
           g.DrawLine(mypen, 50, 50, 40, 40);
           g.DrawString("頻率", new Font("微軟正黑體", 16), Brushes.Green, new PointF(300, 20));
           g.DrawString("電壓值", new Font("Arial", 16), Brushes.Aqua, new PointF(50, 40));
           g.DrawString("頻率", new Font("微軟正黑體", 16), Brushes.Aqua, new PointF(width - 90, height + 350));
            
            BitmapData BD = bmp.LockBits(new Rectangle(minX-3,minY-3, X_Width+3,Y_Height+3),
                                                           ImageLockMode.ReadOnly,
                                                           PixelFormat.Format32bppArgb) ;
            
           // unsafe
           // {

           //    Stopwatch st = new Stopwatch();
           //     st.Restart();
                // Marshal.Copy(BD.Scan0, _pArray, 0, _width * _height);
                // SetDIBitsToDevice(hRef, 0, 0, _width, _height, 0, 0, 0, _height, ref _pArray[0], ref _BI, 0);
                SetDIBitsToDevice(hRef, minX-3, minY-3, X_Width,Y_Height+10, 0, 0, 0,Y_Height+10, (IntPtr)BD.Scan0, ref _BI, 0);
           // st.Stop();
           // Debug.Print(st.Elapsed.TotalMilliseconds.ToString());
            bmp.UnlockBits(BD);
              

           // }

          // st.Stop();
          // Debug.Print(st.Elapsed.TotalMilliseconds.ToString());

        }
        private void redraw(Graphics g)
        { 
 
            Pen mypen = new Pen(Brushes.DeepSkyBlue, 1);
            int height = 418, width = 820;
            foreach (var item in arrLines)
            {
                g.FillEllipse(Brushes.Black, item.X, item.Y, 5, 5);

            }
            g.DrawRectangle(mypen, 0, 0, width - 9, height + 390);//绘制由坐标对、宽度和高度指定的矩形。 
            g.DrawLine(new Pen(Brushes.Black, 1), 40, height + 370, width - 40, height + 370);//x轴
            g.DrawLine(new Pen(Brushes.Black, 1), width - 50, height + 360, width - 40, height + 370);//箭头
            g.DrawLine(new Pen(Brushes.Black, 1), width - 50, height + 360, width - 40, height + 370);
            g.DrawLine(new Pen(Brushes.Black, 1), 40, height + 370, 40, 40);//y轴
            g.DrawLine(new Pen(Brushes.Black, 1), 30, 50, 40, 40);
            g.DrawLine(new Pen(Brushes.Black, 1), 50, 50, 40, 40);
            g.DrawString("頻率", new Font("微軟正黑體", 16), Brushes.Green, new PointF(300, 20));
            g.DrawString("電壓值", new Font("Arial", 16), Brushes.Aqua, new PointF(50, 40));
            g.DrawString("頻率", new Font("微軟正黑體", 16), Brushes.Aqua, new PointF(width - 90, height + 370));
           
            // g.FillEllipse(Brushes.Black, 500, 500, 10, 10);
            Graphics clientDC = this.CreateGraphics();
            IntPtr  hdcPtr = clientDC.GetHdc();
            IntPtr  memdcPtr = CreateCompatibleDC(hdcPtr);   // 创建兼容DC
             bmpPtr = bmp.GetHbitmap();
            SelectObject(memdcPtr, bmpPtr);

            BitBlt(hdcPtr, 0, 0, bmp.Width, bmp.Height, memdcPtr, 0, 0, SRCCOPY);

           DeleteDC(memdcPtr);             // 释放内存
            DeleteObject(bmpPtr);           // 释放内存
            clientDC.ReleaseHdc(hdcPtr);    // 释放内存
           
          
            clientDC.Dispose();
            
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void button5_Click(object sender, EventArgs e)
        {
            int j = 0;
            Task.Run(() =>
            {
                Stopwatch st = new Stopwatch();
                st.Restart();
                /*  Bitmap bmp = new Bitmap(800, 871);
                  g = Graphics.FromImage(bmp);
                  st.Stop();
                  Debug.Print(st.Elapsed.TotalMilliseconds.ToString());
                  st.Restart();
                  for (int i =0;i<=100;i++)
                  {


                      g.FillEllipse(Brushes.Black, i, (i/768)*10, 10, 10);



                  }*/
                string str = ":VxV";
                st.Restart();
                for (int i = 0; i < 100000; i++)
                {
                    j++;
                    strChange(str);
                    //redraw(g);


                    //  MessageBox.Show(st.Elapsed.TotalMilliseconds.ToString());
                }
                st.Stop();
                //Debug.Print(st.Elapsed.TotalMilliseconds.ToString());
            });

            Thread.Sleep(1000);
            MessageBox.Show(j.ToString());
            // pictureBox1.CreateGraphics().DrawImage(bmp, 0, 0);


        }

        private void button6_Click(object sender, EventArgs e)
        {
            this.array = new int[SrcWidth*SrcHeight];
            this.bitmapInfo.biHeader.bihWidth = SrcWidth;
            this.bitmapInfo.biHeader.bihHeight = SrcHeight;
            this.bitmapInfo.biHeader.bihSizeImage = SrcWidth*SrcHeight;
            Stopwatch st = new Stopwatch();
           

                

            this.task = Task.Factory.StartNew(() =>
            {
                using (Graphics graphics = this.CreateGraphics())
                {
                    IntPtr hdc = IntPtr.Zero;
                    try
                    {
                        hdc = graphics.GetHdc();
                        HandleRef handleRef = new HandleRef(graphics, hdc);
                        int k = 0, j = 0;
                        while (true)
                        {


                            st.Restart();
                            Array.Clear(this.array, 0, this.array.Length);
                          
                  
                            /*     for (int i = 100 - 1; i >= 0; --i)
                                 {
                                     particle = SimpleParticlesWorld.Particles[i];
                                     //foreach (SimpleParticle particle in SimpleParticlesWorld.Particles)
                                     //{
                                     pointBase = (this.size.Height - (int)particle.y) * this.size.Width + (int)particle.x;
                                     if (0 <= pointBase && pointBase < this.array.Length)
                                     {
                                         this.array[pointBase] = particle.c;
                                     }
                                 }*/
                            int pointBase;
                            
                            for (int i=100-1;i>=0;--i)
                            {
                                pointBase = (SrcWidth - (int)(j*5)+3) * SrcHeight + (int)(k*5)+i;
                                if (0 <= pointBase && pointBase < this.array.Length)
                                {
                                    this.array[pointBase] = rand.Next();
                                   // this.array[pointBase+1] = rand.Next();
                                  //  this.array[pointBase+2] = rand.Next();
                                  //  this.array[pointBase+3] = rand.Next();

                                }
                            }
                            if (k > 100)
                            {
                                k = 0;
                                j++;
                            }
                            else
                                k++;
                           
                           // SetDIBitsToDevice(handleRef, 0, 0, SrcWidth, SrcHeight, 0, 0, 0, SrcHeight, ref this.array[0], ref this.bitmapInfo, 0);
                            st.Stop();
                            Debug.Print(st.Elapsed.TotalMilliseconds.ToString());
                        }
                    }
                    finally
                    {
                        if (hdc != IntPtr.Zero)
                        {
                            graphics.ReleaseHdc(hdc);
                        }
                    }
                }
            });
        }

        private void button8_Click(object sender, EventArgs e)
        {

            Task.Run(() => { 
                int x = 1;
                int y = 2;
                if (checkBox1.Checked)
                {
                    Stopwatch st = new Stopwatch();
                    st.Restart();
                    this.BeginInvoke(new Action<int[]>((int[] value) =>
                    {
                        MessageBox.Show(value[1].ToString());
                    }), new int[] { x, y });

                }
            });
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            gs.Clear(Color.Black);
            gs.DrawRectangle(mypen, 0, 0, width - 9, height + 390);//绘制由坐标对、宽度和高度指定的矩形。 
            gs.DrawLine(mypen, 40, height + 370, width - 40, height + 370);//x轴
            gs.DrawLine(mypen, width - 50, height + 360, width - 40, height + 370);//箭头
            gs.DrawLine(mypen, width - 50, height + 360, width - 40, height + 370);
            gs.DrawLine(mypen, 40, height + 370, 40, 40);//y轴
            gs.DrawLine(mypen, 30, 50, 40, 40);
            gs.DrawLine(mypen, 50, 50, 40, 40);
            gs.DrawString("頻率", new Font("微軟正黑體", 16), Brushes.Green, new PointF(300, 20));
            gs.DrawString("電壓值", new Font("Arial", 16), Brushes.Aqua, new PointF(50, 40));
            gs.DrawString("頻率", new Font("微軟正黑體", 16), Brushes.Aqua, new PointF(width - 90, height + 350));
        }

        private void button7_Click(object sender, EventArgs e)
        {
            initSetDlb();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            //gs.FillEllipse(Brushes.Black, 100, 100, 50, 50);
          
            // gs.FillEllipse(Brushes.Black, 100, 100, 50, 50);
           

           // DrawImageHighSpeedtoDevice(gs);
         
        
        }

        void initSetDlb()
        {
             graphics = this.CreateGraphics();

            hdc = graphics.GetHdc();
            hRef = new HandleRef(g, hdc);
   
            
            Bitmap bbp = new Bitmap(SrcHeight, SrcWidth);


            _width = SrcWidth;
            _height = SrcHeight;

            _pArray = new int[_width * _height];
            _gcHandle = GCHandle.Alloc(_pArray, GCHandleType.Pinned);
            _BI = new BITMAPINFO
            {
                biHeader =
                {
                    bihBitCount = 32,
                    bihPlanes = 1,
                    bihSize = 40,
                    bihWidth = _width,
                    bihHeight = -_height,
                    bihSizeImage = (_width * _height) << 2
                }
            };
        }
        private void button4_Click_1(object sender, EventArgs e)
        {
            //Task.Run(() => {
                using (Graphics graphics = this.CreateGraphics())
                {
                    IntPtr hdc = IntPtr.Zero;
               
                        hdc = graphics.GetHdc();
                        HandleRef hRef = new HandleRef(graphics, hdc);
                        int _width;
                        int _height;
                        int[] _pArray;
                        GCHandle _gcHandle;
                        BITMAPINFO _BI;
                        Bitmap bbp = new Bitmap(SrcWidth, SrcHeight);
                       
                       
                        _width = SrcWidth;
                        _height = SrcHeight;

                        _pArray = new int[_width * _height];
                        _gcHandle = GCHandle.Alloc(_pArray, GCHandleType.Pinned);
                        _BI = new BITMAPINFO
                        {
                            biHeader =
                {
                    bihBitCount = 32,
                    bihPlanes = 1,
                    bihSize = 40,
                    bihWidth = _width,
                    bihHeight = -_height,
                    bihSizeImage = (_width * _height) << 2
                }
                        };
                    int k = 0,j=0;
                        Stopwatch st = new Stopwatch();
                        st.Restart();
                        for (int i = 0; i <= 10000; i++)
                    {
                        Graphics ggc = Graphics.FromImage(bbp);
                        if (k > 300)
                        {
                            k = 0;
                            j++;
                        }
                        else
                            k++;

                        ggc.FillEllipse(Brushes.Red, (k*5)+3, (j*5)+3, 5, 5);

                            BitmapData BD = bbp.LockBits(new Rectangle(0, 0, bbp.Width, bbp.Height),
                                                            ImageLockMode.ReadOnly,
                                                            PixelFormat.Format32bppArgb);
                            Marshal.Copy(BD.Scan0, _pArray, 0, _width * _height);
                       //     SetDIBitsToDevice(hRef, 0, 0, _width, _height, 0, 0, 0, _height, ref _pArray[0], ref _BI, 0);
                        bbp.UnlockBits(BD);
                        st.Stop();
                            Debug.Print(st.Elapsed.TotalMilliseconds.ToString());
                        }
              
                }
           // });
        }
        }

        /* void SetupSetDlb()
{
    //創建BitmapInfo，這是SetDIBitsToDevice函數所必需的
    BITMAPINFO lpbmi;
    lpbmi.biSize = 40;
    lpbmi.biWidth = m_ImageWidth;
    lpbmi.biHeight = -m_ImageHeight;
    lpbmi.biPlanes = 1;
    lpbmi.biBitCount = 24;
    lpbmi.biCompression =（uint）biCompression.BI_RGB;
    lpbmi.biSizeImage =（uint）（m_ImageHeight* PadLineBytes（m_ImageWidth* lpbmi.biBitCount / 8））;
    lpbmi.biXPelsPerMeter = 0;
    lpbmi.biYPelsPerMeter = 0;
    lpbmi.biClrUsed = 0;
    lpbmi.biClrImportant = 0;
    lpbmi.cols = null;
    if（lpbmi.biBitCount == 8）
    {
        for（int i = 0; i < 256; i++）
        {
            lpbmi.cols[i] =（uint）i;
        }
    }

    IntPtr lpvBits = Marshal.UnsafeAddrOfPinnedArrayElement（imageData，0）;
    圖形graphicsOfPicBox = m_PicView.CreateGraphics（）;
    IntPtr hdc = graphicsOfPicBox.GetHdc（）;

    int iRet = SetDIBitsToDevice（hdc，0,0，（uint）m_ImageWidth，（uint）m_ImageHeight，0,0,0，（uint）m_ImageHeight，lpvBits，
                        ref lpbmi，DIBColorTable.DIB_RGB_COLORS）;

    graphicsOfPicBox.ReleaseHdc（HDC）;
}*/
        /*public unsafe static void initHighSpeed(ref Graphics _grDest, int width,
int height)
        {
             w = width;
             h = height;
             _Bitmap = new Bitmap(width, height);
            grSrc = Graphics.FromImage(_Bitmap);
          //  g = Graphics.FromImage(_Bitmap);
            // _grDest.FillEllipse(Brushes.Black, 100, 100, 50, 50);
            // grSrc.FillEllipse(Brushes.Black, 100, 100, 50, 50);
            Graphics grDest = _grDest;

            hdcDest = grDest.GetHdc();
            hdcSrc = grSrc.GetHdc();

            IntPtr hBitmap = _Bitmap.GetHbitmap();
           // hOldObject = SelectObject(hdcSrc, hBitmap);

            DeleteDC(hdcSrc);             // 释放内存
            DeleteObject(hBitmap);           // 释放内存
            _grDest.ReleaseHdc(hdcDest);    // 释放内存
            grSrc.ReleaseHdc(hdcSrc);
            grSrc.Dispose();
           // _grDest.ReleaseHdc()

            info = new BITMAPINFO();
            info.biSize = (uint)Marshal.SizeOf(info);
            info.biWidth = w;
            info.biHeight = h;
            info.biPlanes = 1;
            info.biBitCount = 32;
            info.biCompression = (uint)BitmapCompressionMode.BI_RGB;
            info.biSizeImage = (uint)(w * h * 4);
            byte[] lpvbits = new byte[4];
            lpvbit = Marshal.UnsafeAddrOfPinnedArrayElement(lpvbits, 0); //取的緩衝區
           /*  fixed (uint* dptr = data)
             {
                 data_ptr = (IntPtr)dptr;
             }*/
        //}
        /*
        public unsafe static void DrawImageHighSpeedtoDevice(Graphics g)
        {
            //grSrc.FillEllipse(Brushes.Black, 100, 100, 50, 50);
            // IntPtr hBitmap = _Bitmap.GetHbitmap();
            //  hOldObject = SelectObject(hdcSrc, hBitmap);
           
            grSrc = Graphics.FromImage(_Bitmap);
            grSrc.FillEllipse(Brushes.Black, 100, 100, 50, 50);
            hdcDest = g.GetHdc();
            //lpvbit = _Bitmap.GetHbitmap();
            SetDIBitsToDevice(hdcDest, 0, 0, (uint)w, (uint)h, 0, 0, 0,
           (uint)h, lpvbit, ref info, 0);
            g.ReleaseHdc(hdcDest);
          //  DeleteDC(hdcSrc);             // 释放内存
          //  DeleteObject(hBitmap);           // 释放内存
           
        }
        */
        /*public static void DrawImage(ref Graphics grDest, ref Bitmap grSrcBitmap)
        {
            grSrc = Graphics.FromImage(grSrcBitmap);
            hdcDest = grDest.GetHdc();
            hdcSrc = grSrc.GetHdc();
            hBitmap = grSrcBitmap.GetHbitmap();
            hOldObject = SelectObject(hdcSrc, hBitmap);
            BitBlt(hdcDest, 0, 0, grSrcBitmap.Width, grSrcBitmap.Height,
        hdcSrc, 0, 0, 0x00CC0020U);
            if (hOldObject != IntPtr.Zero) SelectObject(hdcSrc, hOldObject);
            if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
            if (hdcDest != IntPtr.Zero) grDest.ReleaseHdc(hdcDest);
            if (hdcSrc != IntPtr.Zero) grSrc.ReleaseHdc(hdcSrc);
        }
        public unsafe static void DrawImageHighSpeedtoDevice()
        {
            SetDIBitsToDevice(hdcDest, 0, 0, (uint)w, (uint)h, 0, 0, 0,
           (uint)h, data_ptr, ref info, DIB_RGB_COLORS);
        }*/

    }

