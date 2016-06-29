using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RotateVideoDisplay
{
    public partial class Form1 : Form
    {
        public class star
        {
            public int [] x_Points, y_Points;
            public int X0=0, Y0=0,Len;

            public star(int x0,int y0,int length,int lines)
            {
                x_Points = new int[length* lines]; //lines= count display in 1 rotation
                y_Points = new int[length* lines];
                X0 = x0;
                Y0 = y0;
                Len = length;

                double angle = 180;
                for (int i = 0; i < lines; i++)
                {
                    clac_Coordinate_branch(angle,i);
                    angle = angle - ((double)(360.0/(double)lines));//1.2;
                } 
            }            

            private void clac_Coordinate_branch(double angle,int index)
            {
                for (int i = 0; i < Len; i++)
                {
                    x_Points[index*Len+i] = (int)(Math.Sin((Math.PI / 180) * angle) * i + X0);
                    y_Points[index*Len+i] = (int)(Math.Cos((Math.PI / 180) * angle) * i + Y0);    
                }
            }
        }
        int Count_LEDs;
        public Form1()
        {
            InitializeComponent();
        }

        public void create_file(string path,byte [] data)
        {
            try
            {

                // Delete the file if it exists. 
                if (File.Exists(path))
                {
                    // Note that no lock is put on the 
                    // file and the possibility exists 
                    // that another process could do 
                    // something with it between 
                    // the calls to Exists and Delete.
                    File.Delete(path);
                }

                // Create the file. 
                /*using (FileStream fs = File.Create(path))
                {
                    //Byte[] info = new UTF8Encoding(true).GetBytes("This is some text in the file.");                   

                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }*/

                using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
                {                   
                    writer.Write(data);
                }

                // Open the stream and read it back. 
                /*using (StreamReader sr = File.OpenText(path))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        Console.WriteLine(s);
                    }
                }*/
            }

            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.ToString());
            }
        }

        private void draw_line(double angle, int x0, int y0, int length)
        {
            System.Drawing.Pen myPen;
            myPen = new System.Drawing.Pen(System.Drawing.Color.Red);
            System.Drawing.Graphics formGraphics = this.CreateGraphics();

            //y=mx+b            
            double m,b;
            int x1, y1;
            //try
            {
                m = Math.Tan((Math.PI / 180) * angle);
                //y0 = m * x0 + b;
                b = y0 - (m * x0);
                x1 = (int)(Math.Sin((Math.PI / 180) * angle) * length + x0);
                y1 = (int)(Math.Cos((Math.PI / 180) * angle) * length + y0);
            }
            /*catch
            {
                //x=x0;
                x1 = x0;
                if(angle==270)
                {
                    y1 = -1*length + y0;
                }
                else
                {
                    y1 = length + y0;
                }                
            }*/
            formGraphics.DrawLine(myPen, x0, y0, x1, y1);
            myPen.Dispose();
            formGraphics.Dispose();
        }

        private void button2_Click(object sender, EventArgs e)
        {            
            openFileDialog1.FileName = "";
            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileName != "")
            {
                pictureBox1.Visible = true;
                pictureBox2.Visible = false;
                FileStream fs = new System.IO.FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read);
                pictureBox1.Image =ResizeImage(Image.FromStream(fs), pictureBox1.Width, pictureBox1.Height);
                BtnSave.Enabled = true;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            check_leds_in_a_branch();
            pictureBox2.Visible = false;
        }

        void check_leds_in_a_branch()
        {
            if (txtLEDsInABranch.Text != "")
            {
                Count_LEDs = Int32.Parse(txtLEDsInABranch.Text);
                Count_LEDs /= 2;
                pictureBox1.Height = Count_LEDs * 2;
                pictureBox1.Width = Count_LEDs * 2;

                pictureBox2.Height = Count_LEDs * 2;
                pictureBox2.Width = Count_LEDs * 2;
            }
            else
            {
                MessageBox.Show("لطفا مقداری را وارد کنيد");
                txtLEDsInABranch.Focus();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            check_leds_in_a_branch();
            pictureBox2.Visible = false;
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public class LED
        {
            public LED(int id)
            {           
                Id = id;
            }

            public int Id { get; set; }
                        
            public byte Red { get; set; }
            public byte Green { get; set; }
            public byte Blue { get; set; }
        }

        public class Branch
        {
            public Branch(int id)
            {                
                Id = id;
            }

            public int Id { get; set; }

            public virtual List<LED> LEDs { get; set; } = new List<LED>();            
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {                       

            int lines = int.Parse(txt_lines.Text);                    
            star s=new  star(Count_LEDs,Count_LEDs,Count_LEDs,lines);
            byte[] data = new byte[Count_LEDs * 3 * lines*2]; //3=R,G,B  300=count display in 1 rotation
            
            List<Branch> Br = new List<Branch>();                                   

            Bitmap p1 = new Bitmap(pictureBox1.Image);
            Bitmap p2 = new Bitmap(pictureBox2.Width,pictureBox2.Height);
            Color  c;
            long m=0;
            /*for (int i = 0; i <Count_LEDs* lines; i++)
			{
                c=p1.GetPixel(s.x_Points[i],s.y_Points[i]);
                p2.SetPixel(s.x_Points[i], s.y_Points[i], c);
                //data[m++] = c.R;
                //data[m++] = c.G;
                //data[m++] = c.B;                
			}*/

            m = 0;
            int p = -1;
            bool change_direction = true;
            int k = 0;
            int j = (lines / 2);          
            j *= Count_LEDs;
            int Index1 = -1;
            int Index2 = (lines/2)-1;
            int s1=0, s2=0;
            for (int i = 0; i < Count_LEDs * lines; i++)
            {                
                if ((i % Count_LEDs) == 0)
                {
                    p += Count_LEDs;
                    if (change_direction)
                    {                        
                        change_direction = false;
                        Index1++;
                        Br.Add(new Branch(Index1));
                        s1 = 0;
                    }
                    else
                    {
                        change_direction = true;
                        Index2++;
                        Br.Add(new Branch(Index2));
                        s2 = 0;
                    }                                        
                }
                if(change_direction==true)
                {
                    c = p1.GetPixel(s.x_Points[j], s.y_Points[j]);
                    Br.FirstOrDefault(CU => CU.Id == Index2).LEDs.Add(new LED(s2++) { Red = c.R, Green = c.G, Blue = c.B });
                    j++;
                }
                else
                {
                    c = p1.GetPixel(s.x_Points[p - k], s.y_Points[p - k]);
                    Br.FirstOrDefault(CU => CU.Id == Index1).LEDs.Add(new LED(s1++) { Red = c.R, Green = c.G, Blue = c.B });
                    k++;
                }
                /*data[m++] = c.R;
                data[m++] = c.G;
                data[m++] = c.B;  */              
            }

            lines /= 2;

            if (radioBtn_CW.Checked == true)
            {
                for (int i = 0; i < lines; i++)
                {
                    var b = Br.FirstOrDefault(CU => CU.Id == i).LEDs;
                    for (j = 0; j < b.Count; j++)
                    {
                        data[m++] = b[j].Blue;
                        data[m++] = b[j].Green;
                        data[m++] = b[j].Red;
                    }
                    int ll = i + lines;
                    b = Br.FirstOrDefault(CU => CU.Id == ll).LEDs;
                    for (j = 0; j < b.Count; j++)
                    {
                        data[m++] = b[j].Blue;
                        data[m++] = b[j].Green;
                        data[m++] = b[j].Red;
                    }
                }

                for (int i = 0; i < lines; i++)
                {
                    var b = Br.FirstOrDefault(CU => CU.Id == i + lines).LEDs;
                    for (j = b.Count - 1; j >= 0; j--)
                    {
                        data[m++] = b[j].Blue;
                        data[m++] = b[j].Green;
                        data[m++] = b[j].Red;
                    }
                    b = Br.FirstOrDefault(CU => CU.Id == i).LEDs;
                    for (j = b.Count - 1; j >= 0; j--)
                    {
                        data[m++] = b[j].Blue;
                        data[m++] = b[j].Green;
                        data[m++] = b[j].Red;
                    }
                }
            }
            else
            {
                for (int i = (lines*2)-1; i >= lines; i--)
                {
                    var b = Br.FirstOrDefault(CU => CU.Id == i).LEDs;
                    for (j = b.Count - 1; j >= 0; j--) //for (j = 0; j < b.Count; j++)
                    {
                        data[m++] = b[j].Blue;
                        data[m++] = b[j].Green;
                        data[m++] = b[j].Red;
                    }

                    int ll = i - lines;
                    b = Br.FirstOrDefault(CU => CU.Id == ll).LEDs;
                    for (j = b.Count - 1; j >= 0; j--) //for (j = 0; j < b.Count; j++)
                    {
                        data[m++] = b[j].Blue;
                        data[m++] = b[j].Green;
                        data[m++] = b[j].Red;
                    }
                    
                }

                for (int i = (lines * 2) - 1; i >= lines; i--)
                {
                    var b = Br.FirstOrDefault(CU => CU.Id == i - lines).LEDs;
                    for (j = 0; j < b.Count; j++) //for (j = b.Count - 1; j >= 0; j--)
                    {
                        data[m++] = b[j].Blue;
                        data[m++] = b[j].Green;
                        data[m++] = b[j].Red;
                    }

                    b = Br.FirstOrDefault(CU => CU.Id == i).LEDs;
                    for (j = 0; j < b.Count; j++) //for (j = b.Count - 1; j >= 0; j--)
                    {
                        data[m++] = b[j].Blue;
                        data[m++] = b[j].Green;
                        data[m++] = b[j].Red;
                    }                    
                }
            }

            for (int i = 0; i < lines*2; i++)
            {
                var b = Br.FirstOrDefault(CU => CU.Id == i).LEDs;
                if (i < lines)
                {
                    for (j = b.Count-1; j>=0; j--)
                    {
                        c = Color.FromArgb(b[j].Red, b[j].Green, b[j].Blue);
                        p2.SetPixel(s.x_Points[i * b.Count + (b.Count-j)], s.y_Points[i * b.Count + (b.Count -j)], c);
                    }
                }
                else
                {
                    for (j = 0; j < b.Count; j++)
                    {
                        c = Color.FromArgb(b[j].Red, b[j].Green, b[j].Blue);
                        p2.SetPixel(s.x_Points[i * b.Count + j], s.y_Points[i * b.Count + j], c);
                    }
                }                                            
            }

            pictureBox2.Image = p2;

            pictureBox1.Visible = false;
            pictureBox2.Visible = true;
            
            create_file(@".\Data\"+ txtFileName.Text + ".bin",data);
            byte[] ConfData = new byte[3];
            //lines /= 2;
            lines *= 2;
            ConfData[0] = (byte)(lines / 256);
            ConfData[1] = (byte)(lines % 256);
            ConfData[2] = (byte)(Count_LEDs*2);
            create_file(@".\Data\Conf" + txtFileName.Text + ".bin", ConfData);
        }

        private void txtLEDsInABranch_TextChanged(object sender, EventArgs e)
        {
            check_leds_in_a_branch();
            pictureBox2.Visible = false;
            BtnSave.Enabled = false;
        }
    }
}
