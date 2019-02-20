using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions; 


namespace Test1._0
{
    public partial class Form1 : Form
    {
        enum TYPE { none = 0, optical = 1, spin = 2, TPS = 3 };
        TYPE Type = TYPE.none;
        string OpenRoute, SaveRoute;//图片路径
        Bitmap Picture2, Picture1;//创建图像对象Bitmap1

        BitmapData Data1;
        byte[] OriginalMessage;//图片原始颜色信息
        byte[] NewMessage;//图片更新后的颜色信息
        int[,] CurrentWave;//当前波形
        int[,] NextWave;//下一时刻波形
        int Bitmap_Width;//图片宽度
        int Bitmap_Height;//图片高度
        int WidthByte;//图片宽度方向上的字节数
        int X0 = -1;//波源或旋转中心
        int Y0 = -1;//波源或旋转中心
        int Direction = 0;//旋转方向，-1代表顺时针，1代表逆时针；畸变类型，-1代表桶型，1代表枕型
        bool Start = false;//是否开始的标志
        bool Changed_TPS = false;
        double[] sin = new double[3601];//正弦函数表，角度精确到1位小数
        double[] cos = new double[3601];//余弦函数表，角度精确到1位小数
        int number = 0;
        int PNumber = 0;
        double[,] OMatrix, TMatrix;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Picture1 = new Bitmap(Resource1.LENA);//将默认图片赋给图片对象Bitmap0
            initialization();
        }

        /// <summary>
        /// 初始化函数，包括窗体的初始化以及图像信息变量的初始化
        /// </summary>
        private void initialization()
        {
            Start = false;
            for (int i = 0; i < 3601; i++)
                sin[i] = (double)Math.Sin((double)i * Math.PI / (10 * 180));
            for (int j = 0; j < 3601; j++)
                cos[j] = (double)Math.Cos((double)j * Math.PI / (10 * 180));
            Picture2 = Picture1.Clone(new Rectangle(0, 0, Picture1.Width, Picture1.Height), PixelFormat.Format24bppRgb);//创建副本图片对象
            pictureBox1.Image = Picture2;//在窗体中插入图片
            X0 = -1;
            Y0 = -1;

            //设置标签和各功能按钮的位置
            

            //初始化变量
            Data1 = Picture2.LockBits(new Rectangle(0, 0, Picture2.Width, Picture2.Height), ImageLockMode.ReadOnly, Picture2.PixelFormat);//指定图像的特性
            OriginalMessage = new byte[Data1.Stride * Data1.Height];//图片原始颜色信息
            NewMessage = new byte[OriginalMessage.Length];//图片更新后的颜色信息
            CurrentWave = new int[Picture2.Width, Picture2.Height];//当前波形
            NextWave = new int[Picture2.Width, Picture2.Height];//下一时刻波形
            Bitmap_Width = Picture2.Width;//图片宽度
            Bitmap_Height = Picture2.Height;//图片高度
            WidthByte = Data1.Stride;//图片宽度方向上的字节数
            Marshal.Copy(Data1.Scan0, OriginalMessage, 0, OriginalMessage.Length);//初始化图片原始信息数组
            Picture2.UnlockBits(Data1);//从系统内解锁Bitmap1
            //comboBox2.SelectedItem = "最邻近元法";
        }

        /// <summary>
        /// 图像畸变函数
        /// </summary>
        private void optical(int X0, int Y0, double Angle)
        {
            string pattern = (string)comboBox2.SelectedItem;//comboBox2中的文字内容
            double NewX = 0;//偏移后的X精确值
            double NewY = 0;//偏移后的Y精确值
            int IntegerX = 0;//偏移后的X近似整数值
            int IntegerY = 0;//偏移后的Y近似整数值
            double DecimalX, DecimalY;//小数部分
            double Extent = Direction * Angle / 1000000;
            double Distance;//用于计算目标点与畸变中心的距离
            BitmapData Data2 = Picture2.LockBits(new Rectangle(0, 0, Picture2.Width, Picture2.Height), ImageLockMode.ReadWrite, Picture2.PixelFormat);//创建BmpData副本
            Marshal.Copy(Data2.Scan0, NewMessage, 0, NewMessage.Length);//初始化更新后的图片信息数组NewPictureInfo
            switch (pattern)
            {//判断comboBox2中的内容是什么
                case "最近邻插值":
                    {
                        for(int X = 0; X < Bitmap_Width; X++)
                        {
                            for (int Y = 0; Y < Bitmap_Height; Y++)
                            {
                                Distance = Math.Sqrt(((X - X0) * (X - X0) + (Y - Y0) * (Y - Y0)));
                                NewX = (X - X0) / (1 + Extent * Distance * Distance) + (double)X0;
                                NewY = (Y - Y0) / (1 + Extent * Distance * Distance) + (double)Y0;
                                    //获得最邻近点的坐标（整数值）
                                    IntegerY = (int)(NewY + 0.5);
                                    IntegerX = (int)(NewX + 0.5);
                                    int r = 0, g = 0, b = 0;
                                    if (IntegerX < 0 || IntegerX >= Bitmap_Width || IntegerY < 0 || IntegerY >= Bitmap_Height)
                                    {
                                        r = 0;
                                        g = 0;
                                        b = 0;
                                    }
                                    else
                                    {
                                        r = OriginalMessage[IntegerY * WidthByte + IntegerX * 3];
                                        g = OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 1];
                                        b = OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 2];
                                        if (r < 0) r = 0;
                                        else if (r > 255) r = 255;
                                        else r = r;

                                        if (g < 0) g = 0;
                                        else if (g > 255) g = 255;
                                        else g = g;

                                        if (b < 0) b = 0;
                                        else if (b > 255) b = 255;
                                        else b = b;
                                    }                                    
                                    NewMessage[Y * WidthByte + X * 3] = (byte)r;
                                    NewMessage[Y * WidthByte + X * 3 + 1] = (byte)g;
                                    NewMessage[Y * WidthByte + X * 3 + 2] = (byte)b;
                                //}
                            }
                        }

                        break;
                    }
                case "双线性插值":
                    {
                        for (int X = 0; X < Bitmap_Width; X++)
                        {
                            for (int Y = 0; Y < Bitmap_Height; Y++) 
                            {
                                Distance = Math.Sqrt(((X - X0) * (X - X0) + (Y - Y0) * (Y - Y0)));
                                NewX = (X - X0) / (1 + Extent * Distance * Distance) + (double)X0;
                                NewY = (Y - Y0) / (1 + Extent * Distance * Distance) + (double)Y0;
                                    
                                    //取整
                                    IntegerY = (int)(NewY);
                                    IntegerX = (int)(NewX);
                                    //获取小数部分
                                    DecimalX = NewX - IntegerX;
                                    DecimalY = NewY - IntegerY;
                                    double r1=0,r2=0,r3=0,r4=0,g1=0,g2=0,g3=0,g4=0,b1=0,b2=0,b3=0,b4=0;
                                    if (NewX < 0 || (NewX + 1) >= Bitmap_Width || NewY < 0 || (NewY + 1) >= Bitmap_Height)
                                    {
                                        NewMessage[Y * WidthByte + X * 3] = 0;
                                        NewMessage[Y * WidthByte + X * 3 + 1] = 0;
                                        NewMessage[Y * WidthByte + X * 3 + 2] = 0;
                                    }
                                    else
                                    {
                                        r1 = (1 - DecimalX) * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + IntegerX * 3];
                                        g1 = (1 - DecimalX) * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 1];
                                        b1 = (1 - DecimalX) * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 2];
                                        r2 = (1 - DecimalX) * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + IntegerX * 3];
                                        g2 = (1 - DecimalX) * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + IntegerX * 3 + 1];
                                        b2 = (1 - DecimalX) * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + IntegerX * 3 + 2];
                                        r3 = DecimalX * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + (IntegerX + 1) * 3];
                                        g3 = DecimalX * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + (IntegerX + 1) * 3 + 1];
                                        b3 = DecimalX * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + (IntegerX + 1) * 3 + 2];
                                        r4 = DecimalX * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3];
                                        g4 = DecimalX * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 1];
                                        b4 = DecimalX * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 2];
                                        NewMessage[Y * WidthByte + X * 3] = (byte)(r1 + r2 + r3 + r4);
                                        NewMessage[Y * WidthByte + X * 3 + 1] = (byte)(g1 + g2 + g3 + g4);
                                        NewMessage[Y * WidthByte + X * 3 + 2] = (byte)(b1 + b2 + b3 + b4);
                                    }
                            }
                        }
                        break;
                    }
                case "双三次插值":
                    {
                        for (int X = 0; X < Bitmap_Width; X++)
                        {
                            for (int Y = 0; Y < Bitmap_Height; Y++) 
                            {
                                Distance = Math.Sqrt(((X - X0) * (X - X0) + (Y - Y0) * (Y - Y0)));
                                NewX = (X - X0) / (1 + Extent * Distance * Distance) + (double)X0;
                                NewY = (Y - Y0) / (1 + Extent * Distance * Distance) + (double)Y0;
                                                                
                                //取整
                                IntegerY = (int)(NewY);
                                IntegerX = (int)(NewX);
                                //获取小数部分
                                DecimalX = NewX - IntegerX;
                                DecimalY = NewY - IntegerY;
                                double r1 = 0, r2 = 0, r3 = 0, r4 = 0, g1 = 0, g2 = 0, g3 = 0, g4 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0;
                                    if (IntegerX < 1 || (IntegerX + 2) >= Bitmap_Width || IntegerY < 1 || (IntegerY + 2) >= Bitmap_Height)
                                    {
                                        NewMessage[Y * WidthByte + X * 3] = 0;
                                        NewMessage[Y * WidthByte + X * 3 + 1] = 0;
                                        NewMessage[Y * WidthByte + X * 3 + 2] = 0;
                                    }
                                    else
                                    {
                                        r1 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX - 1) * 3]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX) * 3]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 1) * 3]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 2) * 3];

                                        r2 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX - 1) * 3]
                                                   + S(DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX) * 3]
                                                   + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 1) * 3]
                                                   + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 2) * 3];

                                        r3 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX - 1) * 3]
                                                   + S(DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX) * 3]
                                                   + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3]
                                                   + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 2) * 3];

                                        r4 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX - 1) * 3]
                                                   + S(DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX) * 3]
                                                   + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 1) * 3]
                                                   + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 2) * 3];

                                        g1 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX - 1) * 3 + 1]
                                                   + S(DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX) * 3 + 1]
                                                   + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 1) * 3 + 1]
                                                   + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 2) * 3 + 1];

                                        g2 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX - 1) * 3 + 1]
                                                   + S(DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX) * 3 + 1]
                                                   + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 1) * 3 + 1]
                                                   + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 2) * 3 + 1];

                                        g3 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX - 1) * 3 + 1]
                                                   + S(DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX) * 3 + 1]
                                                   + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 1]
                                                   + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 2) * 3 + 1];

                                        g4 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX - 1) * 3 + 1]
                                                   + S(DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX) * 3 + 1]
                                                   + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 1) * 3 + 1]
                                                   + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 2) * 3 + 1];

                                        b1 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX - 1) * 3 + 2]
                                                   + S(DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX) * 3 + 2]
                                                   + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 1) * 3 + 2]
                                                   + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 2) * 3 + 2];

                                        b2 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX - 1) * 3 + 2]
                                                   + S(DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX) * 3 + 2]
                                                   + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 1) * 3 + 2]
                                                   + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 2) * 3 + 2];

                                        b3 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX - 1) * 3 + 2]
                                                   + S(DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX) * 3 + 2]
                                                   + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 2]
                                                   + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 2) * 3 + 2];

                                        b4 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX - 1) * 3 + 2]
                                                   + S(DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX) * 3 + 2]
                                                   + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 1) * 3 + 2]
                                                   + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 2) * 3 + 2];
                                    }
                                double r = r1 * S(1 + DecimalY) + r2 * S(DecimalY) + r3 * S(1 - DecimalY) + r4 * S(2 - DecimalY);
                                double g = g1 * S(1 + DecimalY) + g2 * S(DecimalY) + g3 * S(1 - DecimalY) + g4 * S(2 - DecimalY);
                                double b = b1 * S(1 + DecimalY) + b2 * S(DecimalY) + b3 * S(1 - DecimalY) + b4 * S(2 - DecimalY);

                                if (r < 0) r = 0;
                                else if (r > 255) r = 255;
                                else r = r;

                                if (g < 0) g = 0;
                                else if (g > 255) g = 255;
                                else g = g;

                                if (b < 0) b = 0;
                                else if (b > 255) b = 255;
                                else b = b;

                                NewMessage[Y * WidthByte + X * 3] = (byte)r;
                                NewMessage[Y * WidthByte + X * 3 + 1] = (byte)g;
                                NewMessage[Y * WidthByte + X * 3 + 2] = (byte)b;
                            }
                        }
                        break;
                    }
                default:
                    {
                        Type = TYPE.none;
                        break;
                    }
            }
            Marshal.Copy(NewMessage, 0, Data2.Scan0, NewMessage.Length);//渲染更新后的图片
            Picture2.UnlockBits(Data2);
            pictureBox1.Refresh();
            //交换能量缓存 将新产生的波形 赋值给当前波形的缓存 计算下一帧的波形
            int[,] temp = CurrentWave;
            CurrentWave = NextWave;
            NextWave = temp;
            Thread.Sleep(500);
        }

        /// <summary>
        /// 旋转扭曲函数
        /// </summary>
        private void spin(int X0, int Y0, int Angle, double AreaSize)
        {
            string pattern = (string)comboBox2.SelectedItem;//comboBox2中的文字内容
            double NewX = 0;//偏移后的X精确值
            double NewY = 0;//偏移后的Y精确值
            int IntegerX = 0;//偏移后的X近似整数值
            int IntegerY = 0;//偏移后的Y近似整数值
            double DecimalX, DecimalY;//小数部分
            int Extent = 20 * Angle;
            int Range;
            Range = (int)(Math.Max(Bitmap_Width, Bitmap_Height) * AreaSize);
            double Distance;//用于判断点是否在圆环半径以内
            int StartX = Math.Max(X0 - Range, 0 );//旋转区域位置x轴起点
            int StartY = Math.Max(Y0 - Range, 0 );//旋转区域位置y轴起点
            int EndX = X0 + Range >= Bitmap_Width ? Bitmap_Width - 1 : X0 + Range;//旋转区域x轴矩形长度
            int EndY = Y0 + Range >= Bitmap_Height ? Bitmap_Height - 1 : Y0 + Range;//旋转区域y轴矩形长度
            double Circumcircle_Radius = Math.Min(Math.Min((EndX - X0), (X0 - StartX)), Math.Min((EndY - Y0), (Y0 - StartY)));// X_Radius < Y_Radius ? X_Radius : Y_Radius;
            BitmapData Data2 = Picture2.LockBits(new Rectangle(0, 0, Picture2.Width, Picture2.Height), ImageLockMode.ReadWrite, Picture2.PixelFormat);//创建BmpData副本
            Marshal.Copy(Data2.Scan0, NewMessage, 0, NewMessage.Length);//初始化更新后的图片信息数组NewMessage
            switch (pattern)
            {//判断comboBox2中的内容是什么
                case "最近邻插值":
                    {
                        for (int X = StartX; X < EndX; X++)
                        {
                            for (int Y = StartY; Y < EndY; Y++)
                            {

                                Distance = Math.Sqrt(((X - X0) * (X - X0) + (Y - Y0) * (Y - Y0)));
                                if (Distance <= Circumcircle_Radius)
                                {
                                    if (Direction == 0)
                                    {
                                        NewX = X;
                                        NewY = Y;
                                    }
                                    else
                                    {
                                        NewX = ((double)(X - X0) * cos[(int)(Extent * (1 - Distance / Circumcircle_Radius))] - (double)Direction * (double)(Y - Y0) * sin[(int)(Extent * (1 - Distance / Circumcircle_Radius))]) + (double)X0;
                                        NewY = ((double)Direction * (double)(X - X0) * sin[(int)(Extent * (1 - Distance / Circumcircle_Radius))] + (double)(Y - Y0) * cos[(int)(Extent * (1 - Distance / Circumcircle_Radius))]) + (double)Y0;
                                    }
                                    //获得最邻近点的坐标（整数值）
                                    IntegerY = (int)(NewY + 0.5);
                                    IntegerX = (int)(NewX + 0.5);

                                    if (IntegerX < 0 || IntegerX >= Bitmap_Width || IntegerY < 0 || IntegerY >= Bitmap_Height)
                                    {
                                        continue;
                                    }
                                    int r = OriginalMessage[IntegerY * WidthByte + IntegerX * 3];
                                    int g = OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 1];
                                    int b = OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 2];

                                    if (r < 0) r = 0;
                                    else if (r > 255) r = 255;
                                    else r = r;

                                    if (g < 0) g = 0;
                                    else if (g > 255) g = 255;
                                    else g = g;

                                    if (b < 0) b = 0;
                                    else if (b > 255) b = 255;
                                    else b = b;
                                    
                                    NewMessage[Y * WidthByte + X * 3] = (byte)r;
                                    NewMessage[Y * WidthByte + X * 3 + 1] = (byte)g;
                                    NewMessage[Y * WidthByte + X * 3 + 2] = (byte)b;

                                }


                            }
                        }

                        break;
                    }
                case "双线性插值":
                    {

                        for (int X = StartX; X < EndX; X++)
                        {
                            for (int Y = StartY; Y < EndY; Y++)
                            {

                                Distance = Math.Sqrt(((X - X0) * (X - X0) + (Y - Y0) * (Y - Y0)));
                                if (Distance <= Circumcircle_Radius)
                                {
                                    if (Direction == 0)
                                    {
                                        NewX = X;
                                        NewY = Y;
                                    }
                                    else
                                    {
                                        NewX = ((double)(X - X0) * cos[(int)(Extent * (1 - Distance / Circumcircle_Radius))] - (double)Direction * (double)(Y - Y0) * sin[(int)(Extent * (1 - Distance / Circumcircle_Radius))]) + (double)X0;
                                        NewY = ((double)Direction * (double)(X - X0) * sin[(int)(Extent * (1 - Distance / Circumcircle_Radius))] + (double)(Y - Y0) * cos[(int)(Extent * (1 - Distance / Circumcircle_Radius))]) + (double)Y0;
                                    }

                                    //取整
                                    IntegerY = (int)(NewY);
                                    IntegerX = (int)(NewX);
                                    //获取小数部分
                                    DecimalX = NewX - IntegerX;
                                    DecimalY = NewY - IntegerY;
                                    if (IntegerX < 0 || (IntegerX + 1) >= Bitmap_Width || IntegerY < 0 || (IntegerY + 1) >= Bitmap_Height)
                                    {
                                        continue;
                                    }
                                    double r1 = (1 - DecimalX) * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + IntegerX * 3];
                                    double g1 = (1 - DecimalX) * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 1];
                                    double b1 = (1 - DecimalX) * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 2];
                                    double r2 = (1 - DecimalX) * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + IntegerX * 3];
                                    double g2 = (1 - DecimalX) * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + IntegerX * 3 + 1];
                                    double b2 = (1 - DecimalX) * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + IntegerX * 3 + 2];
                                    double r3 = DecimalX * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + (IntegerX + 1) * 3];
                                    double g3 = DecimalX * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + (IntegerX + 1) * 3 + 1];
                                    double b3 = DecimalX * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + (IntegerX + 1) * 3 + 2];
                                    double r4 = DecimalX * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3];
                                    double g4 = DecimalX * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 1];
                                    double b4 = DecimalX * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 2];
                                    double r = r1 + r2 + r3 + r4;
                                    double g = g1 + g2 + g3 + g4;
                                    double b = b1 + b2 + b3 + b4;

                                    if (r < 0) r = 0;
                                    else if (r > 255) r = 255;
                                    else r = r;

                                    if (g < 0) g = 0;
                                    else if (g > 255) g = 255;
                                    else g = g;

                                    if (b < 0) b = 0;
                                    else if (b > 255) b = 255;
                                    else b = b;
                                    NewMessage[Y * WidthByte + X * 3] = (byte)r;
                                    NewMessage[Y * WidthByte + X * 3 + 1] = (byte)g;
                                    NewMessage[Y * WidthByte + X * 3 + 2] = (byte)b;

                                }


                            }
                        }
                        break;
                    }
                case "双三次插值":
                    {
                        for (int X = StartX; X < EndX; X++)
                        {
                            for (int Y = StartY; Y < EndY; Y++)
                            {

                                Distance = Math.Sqrt(((X - X0) * (X - X0) + (Y - Y0) * (Y - Y0)));
                                if (Distance <= Circumcircle_Radius)
                                {
                                    if (Direction == 0)
                                    {
                                        NewX = X;
                                        NewY = Y;
                                    }
                                    else
                                    {
                                        NewX = ((double)(X - X0) * cos[(int)(Extent * (1 - Distance / Circumcircle_Radius))] - (double)Direction * (double)(Y - Y0) * sin[(int)(Extent * (1 - Distance / Circumcircle_Radius))]) + (double)X0;
                                        NewY = ((double)Direction * (double)(X - X0) * sin[(int)(Extent * (1 - Distance / Circumcircle_Radius))] + (double)(Y - Y0) * cos[(int)(Extent * (1 - Distance / Circumcircle_Radius))]) + (double)Y0;
                                    }

                                    //取整
                                    IntegerY = (int)(NewY);
                                    IntegerX = (int)(NewX);
                                    //获取小数部分
                                    DecimalX = NewX - IntegerX;
                                    DecimalY = NewY - IntegerY;
                                    if (IntegerX - 1 < 0 || (IntegerX + 2) >= Bitmap_Width || IntegerY - 1 < 0 || (IntegerY + 2) >= Bitmap_Height)
                                    {
                                        continue;
                                    }
                                                                                                          
                                    double r1 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX - 1) * 3]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX) * 3]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 1) * 3]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 2) * 3];

                                    double r2 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX - 1) * 3]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX) * 3]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 1) * 3]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 2) * 3];

                                    double r3 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX - 1) * 3]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX) * 3]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 2) * 3];

                                    double r4 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX - 1) * 3]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX) * 3]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 1) * 3]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 2) * 3];

                                    double g1 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX - 1) * 3 + 1]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX) * 3 + 1]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 1) * 3 + 1]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 2) * 3 + 1];

                                    double g2 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX - 1) * 3 + 1]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX) * 3 + 1]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 1) * 3 + 1]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 2) * 3 + 1];

                                    double g3 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX - 1) * 3 + 1]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX) * 3 + 1]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 1]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 2) * 3 + 1];

                                    double g4 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX - 1) * 3 + 1]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX) * 3 + 1]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 1) * 3 + 1]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 2) * 3 + 1];

                                    double b1 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX - 1) * 3 + 2]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX) * 3 + 2]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 1) * 3 + 2]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 2) * 3 + 2];

                                    double b2 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX - 1) * 3 + 2]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX) * 3 + 2]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 1) * 3 + 2]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 2) * 3 + 2];

                                    double b3 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX - 1) * 3 + 2]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX) * 3 + 2]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 2]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 2) * 3 + 2];

                                    double b4 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX - 1) * 3 + 2]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX) * 3 + 2]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 1) * 3 + 2]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 2) * 3 + 2];
                                    double r = r1 * S(1 + DecimalY) + r2 * S(DecimalY) + r3 * S(DecimalY - 1) + r4 * S(DecimalY - 2);
                                    double g = g1 * S(1 + DecimalY) + g2 * S(DecimalY) + g3 * S(DecimalY - 1) + g4 * S(DecimalY - 2);
                                    double b = b1 * S(1 + DecimalY) + b2 * S(DecimalY) + b3 * S(DecimalY - 1) + b4 * S(DecimalY - 2);

                                    if (r < 0) r = 0;
                                    else if (r > 255) r = 255;
                                    else r = r;

                                    if (g < 0) g = 0;
                                    else if (g > 255) g = 255;
                                    else g = g;

                                    if (b < 0) b = 0;
                                    else if (b > 255) b = 255;
                                    else b = b;

                                    NewMessage[Y * WidthByte + X * 3] = (byte)r;
                                    NewMessage[Y * WidthByte + X * 3 + 1] = (byte)g;
                                    NewMessage[Y * WidthByte + X * 3 + 2] = (byte)b;
                                }
                            }
                        }
                        break;
                    }

                default:
                    {
                        Type = TYPE.none;
                        break;
                    }
            }





            Marshal.Copy(NewMessage, 0, Data2.Scan0, NewMessage.Length);//渲染更新后的图片
            Picture2.UnlockBits(Data2);
            pictureBox1.Refresh();
            int[,] temp = CurrentWave;
            CurrentWave = NextWave;
            NextWave = temp;
            Thread.Sleep(500);
        }

        /// <summary>
        /// TPS网格变形函数
        /// </summary>
        private void TPS(double[,] OriginalMatrix, double[,] TargetMatrix)
        { 
            int n = number;
            double[,] W = new double[n + 3, 2];
            double[,] K = new double[n, n];
            double[,] P = new double[n, 3];
            double[,] P_trans = new double[3, n];
            double[,] L = new double[n + 3, n + 3];
            double[,] L_inverse = new double[n + 3, n + 3];
            double[,] Y = new double[n + 3, 2];
            double[,] Y_trans = new double[2, n + 3]; 
            string pattern = (string)comboBox2.SelectedItem;//comboBox2中的文字内容
            double NewX = 0;//偏移后的X精确值
            double NewY = 0;//偏移后的Y精确值
            int IntegerX = 0;//偏移后的X近似整数值
            int IntegerY = 0;//偏移后的Y近似整数值
            double DecimalX, DecimalY;//小数部分
            int i, j;
            double distance;
            for (i = 0; i < n; i++)
            {
                for (j = 0; j < n; j++)
                {
                    if (i == j) K[i, j] = 0;
                    else
                    {
                        distance = (TargetMatrix[0, i] - TargetMatrix[0, j]) * (TargetMatrix[0, i] - TargetMatrix[0, j])
                                   + (TargetMatrix[1, i] - TargetMatrix[1, j]) * (TargetMatrix[1, i] - TargetMatrix[1, j]);
                        K[i, j] = distance * Math.Log10(distance);
                    }
                }
            }
            for (i = 0; i < n; i++)
            {
                P_trans[0, i] = 1;
                P_trans[1, i] = TargetMatrix[0, i];
                P_trans[2, i] = TargetMatrix[1, i];
            }
            for (i = 0; i < n; i++)
            {
                for (j = 0; j < 3; j++)
                {
                    P[i, j] = P_trans[j, i];
                }
            }
            for (i = 0; i < n; i++)
            {
                for (j = 0; j < n; j++)
                {
                    L[i, j] = K[i, j];
                }
                for (j = n; j < n + 3; j++)
                {
                    L[i, j] = P[i, j - n];
                }
            }
            for (i = n; i < n + 3; i++)
            {
                for (j = 0; j < n; j++)
                {
                    L[i, j] = P_trans[i - n, j];
                }
                for (j = n; j < n + 3; j++)
                {
                    L[i, j] = 0;
                }
            }
            for (i = 0; i <= 1; i++)
            {
                for (j = 0; j <= n + 2; j++)
                {
                    if (j < n) Y_trans[i,j] = OriginalMatrix[i,j];
                    else Y_trans[i,j] = 0;
                }
            }
            for (i = 0; i <= n + 2; i++)
            {
                for (j = 0; j <= 1; j++)
                {
                    Y[i, j] = Y_trans[j, i];
                }
            }
            converse(L,L_inverse);
            for (i = 0; i < n + 3; i++)
            {
                for (j = 0; j < n + 3; j++)
                {
                    W[i, 0] = W[i, 0] + L_inverse[i, j] * Y[j, 0];
                    W[i, 1] = W[i, 1] + L_inverse[i, j] * Y[j, 1];
                }
            }
            BitmapData Data2 = Picture2.LockBits(new Rectangle(0, 0, Picture2.Width, Picture2.Height), ImageLockMode.ReadWrite, Picture2.PixelFormat);//创建BmpData副本
            Marshal.Copy(Data2.Scan0, NewMessage, 0, NewMessage.Length);//初始化更新后的图片信息数组NewPictureInfo
            double[,] Distance = new double[1, n];
            double[,] U = new double[1, n + 3];
            switch (pattern)
            {//判断comboBox2中的内容是什么
                case "最近邻插值":
                    {
                        for(int x = 0; x < Bitmap_Width; x++)
                        {
                            for (int y = 0; y < Bitmap_Height; y++)
                            {
                                for (i = 0; i < n; i++)
                                {
                                    Distance[0, i] = (TargetMatrix[0,i] - x) * (TargetMatrix[0, i] - x) + (TargetMatrix[1, i] - y) * (TargetMatrix[1, i] - y);
                                }
                                for (i = 0; i < n; i++)
                                {
                                    U[0, i] = Distance[0, i] * Math.Log10(Distance[0, i]);
                                }
                                U[0, n] = 1;
                                U[0, n + 1] = x;
                                U[0, n + 2] = y;
                                NewX = 0;
                                NewY = 0;
                                for (i = 0; i < n + 3; i++)
                                {
                                    NewX = NewX + U[0, i] * W[i, 0];
                                    NewY = NewY + U[0, i] * W[i, 1];
                                }

                                //获得最邻近点的坐标（整数值）
                                IntegerY = (int)(NewY + 0.5);
                                IntegerX = (int)(NewX + 0.5);
                                int r = 0, g = 0, b = 0;
                                if (IntegerX < 0 || IntegerX >= Bitmap_Width || IntegerY < 0 || IntegerY >= Bitmap_Height)
                                {
                                    r = 0;
                                    g = 0;
                                    b = 0;
                                }
                                else
                                {
                                    r = OriginalMessage[IntegerY * WidthByte + IntegerX * 3];
                                    if (r < 0) r = 0;
                                    else if (r > 255) r = 255;
                                    else r=
                                    g = OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 1];
                                    if (g < 0) g = 0;
                                    else if (g > 255) g = 255;
                                    else g = g;
                                    b = OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 2];
                                    if (b < 0) b = 0;
                                    else if (b > 255) b = 255;
                                    else b = b;
                                }                                    
                                NewMessage[y * WidthByte + x * 3] = (byte)r;
                                NewMessage[y * WidthByte + x * 3 + 1] = (byte)g;
                                NewMessage[y * WidthByte + x * 3 + 2] = (byte)b;
                            }
                        }

                        break;
                    }
                case "双线性插值":
                    {
                        for (int x = 0; x < Bitmap_Width; x++)
                        {
                            for (int y = 0; y < Bitmap_Height; y++)
                            {
                                for (i = 0; i < n; i++)
                                {
                                    Distance[0, i] = (TargetMatrix[0, i] - x) * (TargetMatrix[0, i] - x) + (TargetMatrix[1, i] - y) * (TargetMatrix[1, i] - y);
                                }
                                for (i = 0; i < n; i++)
                                {
                                    U[0, i] = Distance[0, i] * Math.Log10(Distance[0, i]);
                                }
                                U[0, n] = 1;
                                U[0, n + 1] = x;
                                U[0, n + 2] = y;
                                NewX = 0;
                                NewY = 0;
                                for (i = 0; i < n + 3; i++)
                                {
                                    NewX = NewX + U[0, i] * W[i, 0];
                                    NewY = NewY + U[0, i] * W[i, 1];
                                }

                                //取整
                                IntegerY = (int)(NewY);
                                IntegerX = (int)(NewX);
                                //获取小数部分
                                DecimalX = NewX - IntegerX;
                                DecimalY = NewY - IntegerY;
                                double r1 = 0, r2 = 0, r3 = 0, r4 = 0, g1 = 0, g2 = 0, g3 = 0, g4 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0;
                                if ((IntegerX) < 0 || (IntegerX + 1) >= Bitmap_Width || (IntegerY) < 0 || (IntegerY + 1) >= Bitmap_Height)
                                {
                                    NewMessage[y * WidthByte + x * 3] = 0;
                                    NewMessage[y * WidthByte + x * 3 + 1] = 0;
                                    NewMessage[y * WidthByte + x * 3 + 2] = 0;
                                }
                                else
                                {
                                    r1 = (1 - DecimalX) * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + IntegerX * 3];
                                    g1 = (1 - DecimalX) * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 1];
                                    b1 = (1 - DecimalX) * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + IntegerX * 3 + 2];
                                    r2 = (1 - DecimalX) * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + IntegerX * 3];
                                    g2 = (1 - DecimalX) * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + IntegerX * 3 + 1];
                                    b2 = (1 - DecimalX) * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + IntegerX * 3 + 2];
                                    r3 = DecimalX * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + (IntegerX + 1) * 3];
                                    g3 = DecimalX * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + (IntegerX + 1) * 3 + 1];
                                    b3 = DecimalX * (1 - DecimalY) * (double)OriginalMessage[IntegerY * WidthByte + (IntegerX + 1) * 3 + 2];
                                    r4 = DecimalX * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3];
                                    g4 = DecimalX * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 1];
                                    b4 = DecimalX * DecimalY * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 2];
                                    NewMessage[y * WidthByte + x * 3] = (byte)(r1 + r2 + r3 + r4);
                                    NewMessage[y * WidthByte + x * 3 + 1] = (byte)(g1 + g2 + g3 + g4);
                                    NewMessage[y * WidthByte + x * 3 + 2] = (byte)(b1 + b2 + b3 + b4);
                                }

                                //}
                            }
                        }
                        break;
                    }
                case "双三次插值":
                    {
                        for (int x = 0; x < Bitmap_Width; x++)
                        {
                            for (int y = 0; y < Bitmap_Height; y++)
                            {
                                for (i = 0; i < n; i++)
                                {
                                    Distance[0, i] = (TargetMatrix[0, i] - x) * (TargetMatrix[0, i] - x) + (TargetMatrix[1, i] - y) * (TargetMatrix[1, i] - y);
                                }
                                for (i = 0; i < n; i++)
                                {
                                    U[0, i] = Distance[0, i] * Math.Log10(Distance[0, i]);
                                }
                                U[0, n] = 1;
                                U[0, n + 1] = x;
                                U[0, n + 2] = y;
                                NewX = 0;
                                NewY = 0;
                                for (i = 0; i < n + 3; i++)
                                {
                                    NewX = NewX + U[0, i] * W[i, 0];
                                    NewY = NewY + U[0, i] * W[i, 1];
                                }

                                //取整
                                IntegerY = (int)(NewY);
                                IntegerX = (int)(NewX);
                                //获取小数部分
                                DecimalX = NewX - IntegerX;
                                DecimalY = NewY - IntegerY;
                                double r1 = 0, r2 = 0, r3 = 0, r4 = 0, g1 = 0, g2 = 0, g3 = 0, g4 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0;
                                if (IntegerX < 1 || (IntegerX + 2) >= Bitmap_Width || IntegerY < 1 || (IntegerY + 2) >= Bitmap_Height)
                                {
                                    NewMessage[y * WidthByte + x * 3] = 0;
                                    NewMessage[y * WidthByte + x * 3 + 1] = 0;
                                    NewMessage[y * WidthByte + x * 3 + 2] = 0;
                                }
                                else
                                {
                                    r1 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX - 1) * 3]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX) * 3]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 1) * 3]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 2) * 3];

                                    r2 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX - 1) * 3]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX) * 3]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 1) * 3]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 2) * 3];

                                    r3 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX - 1) * 3]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX) * 3]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 2) * 3];

                                    r4 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX - 1) * 3]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX) * 3]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 1) * 3]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 2) * 3];

                                    g1 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX - 1) * 3 + 1]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX) * 3 + 1]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 1) * 3 + 1]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 2) * 3 + 1];

                                    g2 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX - 1) * 3 + 1]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX) * 3 + 1]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 1) * 3 + 1]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 2) * 3 + 1];

                                    g3 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX - 1) * 3 + 1]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX) * 3 + 1]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 1]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 2) * 3 + 1];

                                    g4 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX - 1) * 3 + 1]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX) * 3 + 1]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 1) * 3 + 1]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 2) * 3 + 1];

                                    b1 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX - 1) * 3 + 2]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX) * 3 + 2]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 1) * 3 + 2]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY - 1) * WidthByte + (IntegerX + 2) * 3 + 2];

                                    b2 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX - 1) * 3 + 2]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX) * 3 + 2]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 1) * 3 + 2]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY) * WidthByte + (IntegerX + 2) * 3 + 2];

                                    b3 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX - 1) * 3 + 2]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX) * 3 + 2]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 1) * 3 + 2]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 1) * WidthByte + (IntegerX + 2) * 3 + 2];

                                    b4 = S(1 + DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX - 1) * 3 + 2]
                                               + S(DecimalX) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX) * 3 + 2]
                                               + S(DecimalX - 1) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 1) * 3 + 2]
                                               + S(DecimalX - 2) * (double)OriginalMessage[(IntegerY + 2) * WidthByte + (IntegerX + 2) * 3 + 2];
                                    double r = r1 * S(1 + DecimalY) + r2 * S(DecimalY) + r3 * S(1 - DecimalY) + r4 * S(2 - DecimalY);
                                    double g = g1 * S(1 + DecimalY) + g2 * S(DecimalY) + g3 * S(1 - DecimalY) + g4 * S(2 - DecimalY);
                                    double b = b1 * S(1 + DecimalY) + b2 * S(DecimalY) + b3 * S(1 - DecimalY) + b4 * S(2 - DecimalY);

                                    if (r > 255) r = 255;
                                    else if (r < 0) r = 0;
                                    if (g > 255) g = 255;
                                    else if (g < 0) g = 0;
                                    if (b > 255) b = 255;
                                    else if (b < 0) b = 0;
                                    NewMessage[y * WidthByte + x * 3] = (byte)r;
                                    NewMessage[y * WidthByte + x * 3 + 1] = (byte)g;
                                    NewMessage[y * WidthByte + x * 3 + 2] = (byte)b;
                                }
                            }
                        }
                        break;
                    }
                default:
                    {
                        Type = TYPE.none;
                        break;
                    }
            }
            Marshal.Copy(NewMessage, 0, Data2.Scan0, NewMessage.Length);//渲染更新后的图片
            Picture2.UnlockBits(Data2);
            pictureBox1.Refresh();
            int[,] temp = CurrentWave;
            CurrentWave = NextWave;
            NextWave = temp;
            Thread.Sleep(500);
        }


        /// <summary>
        /// 开始/继续按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked || radioButton4.Checked) Direction = -1;
            else if (radioButton2.Checked || radioButton3.Checked) Direction = 1;
            string str = (string)comboBox1.SelectedItem;//comboBox1中的文字内容
            switch (str)
            {//判断comboBox1中的内容是什么
                case "图像畸变":
                    {
                        ///默认畸变中心为图片中心
                        X0 = Bitmap_Width / 2;
                        Y0 = Bitmap_Height / 2;
                        Type = TYPE.optical;
                        break;
                    }
                case "旋转扭曲":
                    {

                        ///默认旋转中心为图片中心
                        X0 = Bitmap_Width / 2;
                        Y0 = Bitmap_Height / 2;
                        Type = TYPE.spin;
                        break;
                    }
                case "TPS网格变形":
                    {
                        Type = TYPE.TPS;
                        break;
                    }
                default:
                    {
                        Type = TYPE.none;
                        break;
                    }
            }
            Start = true;
            switch (Type)
            {
                case TYPE.optical:
                    {
                        if (X0 >= 0 && Y0 >= 0)
                        {
                            if ((radioButton3.Checked == false) && (radioButton4.Checked == false))
                            {
                                MessageBox.Show("请选择畸变类型");
                            }
                            else if ((double)trackBar3.Value == 0)
                            {
                                MessageBox.Show("请设置畸变程度");
                            }
                            else if ((string)comboBox2.SelectedItem == null)
                            {
                                MessageBox.Show("请选择插值方式");
                            }
                            else
                            {
                                Picture2 = Picture1.Clone(new Rectangle(0, 0, Picture1.Width, Picture1.Height), PixelFormat.Format24bppRgb);//创建副本图片对象
                                pictureBox1.Image = Picture2;//在窗体中插入图片
                            }
                            optical(X0, Y0, (double)trackBar3.Value);
                        }
                        break;
                    }
                case TYPE.TPS:
                    {
                        if (Changed_TPS)
                        {
                            if (number == 0)
                            {
                                MessageBox.Show("请输入控制点对数");
                            }
                            else if ((string)comboBox2.SelectedItem == null)
                            {
                                MessageBox.Show("请选择插值方式");
                            }
                            else if ((OMatrix[0, 0] == 0) && (OMatrix[1, 0] == 0))
                            {
                                MessageBox.Show("请在窗体中选择控制点");
                            }
                            else
                            {
                                Picture2 = Picture1.Clone(new Rectangle(0, 0, Picture1.Width, Picture1.Height), PixelFormat.Format24bppRgb);//创建副本图片对象
                                pictureBox1.Image = Picture2;//在窗体中插入图片
                                TPS(OMatrix, TMatrix);
                            }
                        }
                        break;
                    }
                case TYPE.spin:
                    {
                        if (X0 >= 0 && Y0 >= 0)
                        {
                            if ((radioButton1.Checked == false) && (radioButton2.Checked == false))
                            {
                                MessageBox.Show("请选择旋转方向");
                            }
                            else if ((double)trackBar1.Value == 0)
                            {
                                MessageBox.Show("请设置旋转角度");
                            }
                            else if ((double)trackBar2.Value == 0)
                            {
                                MessageBox.Show("请设置旋转区域大小");
                            }
                            else if ((string)comboBox2.SelectedItem == null)
                            {
                                MessageBox.Show("请选择插值方式");
                            }
                            else
                            {
                                Picture2 = Picture1.Clone(new Rectangle(0, 0, Picture1.Width, Picture1.Height), PixelFormat.Format24bppRgb);//创建副本图片对象
                                pictureBox1.Image = Picture2;//在窗体中插入图片
                            }
                            spin(X0, Y0, trackBar1.Value * 10, (double)trackBar2.Value / 20);
                        }

                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// 恢复原图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            initialization();
            PNumber = 0;
        }
        
        private double S(double x)
        {
            if (x < 0) x = -x;
            if (x >= 0.0 && x < 1)
            {
                return 1.0 - 2.0 * x * x + x * x * x;
            }
            if (x >= 1.0 && x < 2.0)
            {
                return 4.0 - 8.0 * x + 5.0 * x * x - x * x * x;
            }
            else
                return 0.0;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Start = false;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                OpenRoute = openFileDialog1.FileName;//得到图片地址            
                Picture1 = new Bitmap(OpenRoute);//打开一张图片,赋给图片对象Bitmap0
                initialization();
            }
        }

        //求逆  
        void converse(double[,] m,double[,] c)
        { 
            double[,] a = new double[number+3,number+3];
            for (int i = 0; i < number + 3; i++)
            {
                for (int j = 0; j < number + 3; j++)
                {
                    a[i, j] = m[i, j];
                }
            }
            for (int i = 0; i < number + 3; i++)
            {
                for (int j = 0; j < number + 3; j++)
                {
                    if (i == j) 
                    { 
                        c[i, j] = 1;
                    }
                    else 
                    { 
                        c[i, j] = 0; }
                    }
                }
            
            //i表示第几行，j表示第几列
            for (int j = 0; j < number + 3; j++)
            {
                bool flag = false;
                for (int i = j; i < number + 3; i++)
                {
                    if (a[i, j] != 0)
                    {
                        flag = true;
                        double temp;
                        //交换i,j,两行  
                        if (i != j)
                        {
                            for (int k = 0; k < number + 3; k++)
                            {
                                temp = a[j, k];
                                a[j, k] = a[i, k];
                                a[i, k] = temp;
                                
                                temp = c[j, k];
                                c[j, k] = c[i, k];
                                c[i, k] = temp;
                            }
                        }
                        //第j行标准化  
                        double d = a[j, j];
                        for (int k = 0; k < number + 3; k++)
                        {
                            a[j, k] = a[j, k] / d;
                            c[j, k] = c[j, k] / d;
                        }
                        //消去其他行的第j列  
                        d = a[j, j];
                        for (int k = 0; k < number + 3; k++)
                        {
                            if (k != j)
                            {
                                double t = a[k, j];
                                for (int n = 0; n < number + 3; n++)
                                {
                                    a[k, n] -= (t / d) * a[j, n];
                                    c[k, n] -= (t / d) * c[j, n];
                                }
                            }
                        }
                    }
                    }
                }
            }
       
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            MouseEventArgs Now = (MouseEventArgs)e;
            Graphics draw_lines = pictureBox1.CreateGraphics();
            Point MousePosition = pictureBox1.PointToClient(Control.MousePosition);
            Pen Pen1 = new Pen(Color.Coral, 2);
            Pen Pen2 = new Pen(Color.Green, 4);

            //如果选择了TPS变换并且已经选择的点数少于设置的控制点数
            if (Changed_TPS && (PNumber < number * 2))
            {
                //通过已选择的点数的奇偶性判断它是原点还是目标点
                if ((PNumber % 2) == 0)
                {
                    OMatrix[0, PNumber / 2] = Now.Location.X;
                    OMatrix[1, PNumber / 2] = Now.Location.Y;
                }
                if ((PNumber % 2) == 1)
                {
                    TMatrix[0, PNumber / 2] = Now.Location.X;
                    TMatrix[1, PNumber / 2] = Now.Location.Y;
                    draw_lines.DrawLine(Pen1, (int)OMatrix[0, PNumber / 2], (int)OMatrix[1, PNumber / 2], (int)TMatrix[0, PNumber / 2], (int)TMatrix[1, PNumber / 2]);
                    draw_lines.DrawEllipse(Pen2, (int)OMatrix[0, PNumber / 2] - 2, (int)OMatrix[1, PNumber / 2] - 2, 4, 4);
                    draw_lines.DrawEllipse(Pen2, (int)TMatrix[0, PNumber / 2] - 2, (int)TMatrix[1, PNumber / 2] - 2, 4, 4);
                }
            }
            else if (PNumber >= (number * 2))
                MessageBox.Show("请勿选择多于设置值的点数！");
            PNumber++;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string Pattern = comboBox1.Text;
            if (Pattern == "TPS网格变形") Changed_TPS = true;
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                SaveRoute = saveFileDialog1.FileName;//得到图片地址            
                Picture2.Save(SaveRoute);
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            number = Convert.ToInt32(textBox1.Text);
            if (number <= 0)
            {
                MessageBox.Show("请输入正整数！");
                number = 0;
            }
            string Pattern = comboBox1.Text;
            if (Pattern == "TPS网格变形")
            {
                Changed_TPS = true;
            }
            OMatrix = new double[2, number];
            TMatrix = new double[2, number];            
        }

        private void Keypress(object sender, KeyPressEventArgs e)
        {
            //如果输入的是数字或者回车符则允许输入
            if (!(Char.IsNumber(e.KeyChar)) && e.KeyChar != (char)8)
            {
                e.Handled = true;
            }
        }
    }
}
