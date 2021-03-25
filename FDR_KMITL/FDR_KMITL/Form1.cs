using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace FDR_KMITL
{
    
    public partial class mainForm : Form
    {

        MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_TRIPLEX, 0.6d, 0.6d);
        HaarCascade faceDetected;
        Image<Bgr, Byte> Frame;
        Capture camera;
        Image<Gray, Byte> result;
        Image<Gray, Byte> trainedFace = null;
        Image<Gray, Byte> grayFace = null;
        List<Image<Gray, Byte>> trainningImages = new List<Image<Gray, byte>>();
        List<string> labels = new List<string>();
        List<string> Users = new List<string>();
        int count, numLabels, t;
        string name, names = null;
        bool capture_mode = false;
        int capture_count = 0;
        


        
        public mainForm()
        {
            InitializeComponent();
            
            

            faceDetected = new HaarCascade("haarcascade_frontalface_default.xml");
            try
            {
                string Labelsinf = File.ReadAllText(Application.StartupPath + "/Faces/Faces.txt");
                string[] Labels = Labelsinf.Split(',');
                numLabels = Convert.ToInt16(Labels[0]);
                count = numLabels;
                string FacesLoad;
                for (int i=1; i<numLabels + 1; i++)
                {
                    FacesLoad = "face" + i + ".bmp";
                    trainningImages.Add(new Image<Gray, byte>(Application.StartupPath + "/Faces/" + FacesLoad));
                    labels.Add(Labels[i]);
                }


            } catch (Exception ex)
            {
                MessageBox.Show("Nothing in DB " + ex.Message.ToString());
            }
        }

        /*private void btnCapture_Click(object sender, EventArgs e)
        {
            // เป็นการเปลี่ยนโหมดว่ากำลังจับหน้าแบบชุดอยู่หรือไม่
            if(capture_mode == false) {
                capture_mode = true;
                btnCapture.Text = "Stop";
            } else
            {
                capture_count = 0;
                lblFace.Text = "# of faces : 0";
                capture_mode = false;
                btnCapture.Text = "Start";
            }
                
        }*/

        DataTable table = new DataTable();
        private void mainForm_Load(object sender, EventArgs e)
        {
            table.Columns.Add("EmployeeID", typeof(int));
            table.Columns.Add("Time", typeof(string));
            dataGridView1.DataSource = table;

        }
        

        private void dataGridView1_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            DataTable table = new DataTable();
            if (e.ColumnIndex == 0 && dataGridView1.CurrentCell.Value != null)
            {
                foreach(DataGridViewRow row in this.dataGridView1.Rows)
                {
                    if (row.Index == this.dataGridView1.CurrentCell.RowIndex)
                    {
                        continue;
                    }
                    if (this.dataGridView1.CurrentCell.Value == null)
                    {
                        continue;
                    }
                    if (row.Cells[0].Value != null && row.Cells[0].Value.ToString() == dataGridView1.CurrentCell.Value.ToString())
                    {
                        dataGridView1.CurrentCell.Value = null;
                    }
                }
            }
        }
        
        private void btnDR_Click(object sender, EventArgs e)
        {
            
            camera = new Capture();
            camera.QueryFrame();
            Application.Idle += new EventHandler(FrameProcedure2);
            //ใส่ตาราง
            //table.Rows.Add("60010223", "22:00:00");
            //dataGridView1.DataSource = table;
            
            

            btnDR.Enabled = false;
            btnSave.Enabled = false;

        }

        private void button1Re_Click(object sender, EventArgs e)
        {
            //string query = "INSERT INTO dbo.UserCheck VALUES ( '" + dataGridView1.Rows[i].Cells[0].Value + "', CAST(GETDATE() AS DATE), '" + dataGridView1.Rows[i].Cells[1].Value + "','');";
            string sqlDataSource = "Data Source=LAPTOP-3KQ0AE11\\MSSQLSERVER01;Initial Catalog=GUYs;Integrated Security=True;";
            SqlDataReader myReader;
            using (SqlConnection con = new SqlConnection(sqlDataSource))
            {
                for (int i = 0; i < dataGridView1.Rows.Count-2; i++)
                {

                    using (SqlCommand cmd = new SqlCommand(@"INSERT INTO dbo.UserCheck VALUES ( '" + dataGridView1.Rows[i].Cells[0].Value + "', CAST(GETDATE() AS DATE), '" + dataGridView1.Rows[i].Cells[1].Value + "');", con))
                    {
                        con.Open();
                        myReader = cmd.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                        con.Close();
                    }
                }
            }
        }

        

        private void FrameProcedure(object sender, EventArgs e)
        {
            
            Users.Add("");
            Frame = camera.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            grayFace = Frame.Convert<Gray, Byte>();
            // MCvAvgComp[][] facesDetectedNow = grayFace.DetectHaarCascade(faceDetected, 1.2, 10, 
            //    Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));

            MCvAvgComp[][] facesDetectedNow = grayFace.DetectHaarCascade(
                faceDetected,
                1.1,
                10,
                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                new Size(20, 20));
                

            foreach (MCvAvgComp f in facesDetectedNow[0])
            {
                result = Frame.Copy(f.rect).Convert<Gray, Byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                Frame.Draw(f.rect, new Bgr(Color.Green), 2);

                if (trainningImages.ToArray().Length != 0)
                {
                    MCvTermCriteria termCriteria = new MCvTermCriteria(count, 0.001);
                    EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainningImages.ToArray(), labels.ToArray(),
                        2500, ref termCriteria);
                    name = recognizer.Recognize(result);
                    if (name.Length != 0)
                    {
                        
                        Frame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.Green)); 
                    }
                    else
                    {
                        Frame.Draw("Unknown", ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.Green));
                        
                        // code สำหรับใช้จับหน้าเป็นชุดโดยผมตั้งไว้ให้เก็บหน้าไว้แค่ 20 ภาพ ต่อ ครั้ง
                        if (capture_mode == true && capture_count < 20)
                        {
                            count = count + 1;
                            grayFace = camera.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                        
                            MCvAvgComp[][] detectedFaces = grayFace.DetectHaarCascade(
                                    faceDetected,
                                    1.2,
                                    10,
                                    Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                                    new Size(40, 40));


                            trainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                            trainningImages.Add(trainedFace);
                            labels.Add(tbxName.Text);
                            File.WriteAllText(Application.StartupPath + "/Faces/Faces.txt", trainningImages.ToArray().Length.ToString() + ",");
                            capture_count++;
                            //lblFace.Text = "# of faces : " + capture_count;
                            for (int i = 1; i < trainningImages.ToArray().Length + 1; i++)
                            {
                                trainningImages.ToArray()[i - 1].Save(Application.StartupPath + "/Faces/face" + i + ".bmp");
                                File.AppendAllText(Application.StartupPath + "/Faces/Faces.txt", labels.ToArray()[i - 1] + ",");
                            }
                        } else
                        {
                            capture_count = 0;
                            capture_mode = false;
                            //btnCapture.Text = "Start";
                            //lblFace.Text = "# of faces : 0";
                        }
                    }
                    

                } else
                {
                    Frame.Draw("Unknown", ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.Green));

                    // code สำหรับใช้จับหน้าเป็นชุดโดยผมตั้งไว้ให้เก็บหน้าไว้แค่ 20 ภาพ ต่อ ครั้ง
                    if (capture_mode == true && capture_count < 20) {
                        count = count + 1;
                        grayFace = camera.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                        //MCvAvgComp[][] detectedFaces = grayFace.DetectHaarCascade(faceDetected, 1.2, 10,
                        //  Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));

                        MCvAvgComp[][] detectedFaces = grayFace.DetectHaarCascade(
                                faceDetected,
                                1.2,
                                10,
                                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                                new Size(40, 40));


                        trainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                        trainningImages.Add(trainedFace);
                        labels.Add(tbxName.Text);
                        File.WriteAllText(Application.StartupPath + "/Faces/Faces.txt", trainningImages.ToArray().Length.ToString() + ",");
                        capture_count++;
                        //lblFace.Text = "# of faces : " + capture_count;
                        for (int i = 1; i < trainningImages.ToArray().Length + 1; i++)
                        {
                            trainningImages.ToArray()[i - 1].Save(Application.StartupPath + "/Faces/face" + i + ".bmp");
                            File.AppendAllText(Application.StartupPath + "/Faces/Faces.txt", labels.ToArray()[i - 1] + ",");
                        }
                    } else
                    {
                        capture_count = 0;
                        capture_mode = false;
                        //btnCapture.Text = "Start";
                        //lblFace.Text = "# of faces : 0";
                    }
                }
            
                //Users[t - 1] = name;
                Users.Add("");
            }

            cameraBox.Image = Frame;
            names = "";
            Users.Clear();
            

            //Frame.Draw(font.rect, new Bgr(Color.Green), 3);

        }

        private void btnRC_Click(object sender, EventArgs e)
        {
            camera = new Capture();
            camera.QueryFrame();
            Application.Idle += new EventHandler(FrameProcedure);

            btnRC.Enabled = false;
            btnDR.Enabled = false;
        }

        private void btnST_Click(object sender, EventArgs e)
        {
            btnDR.Enabled = true;
            btnRC.Enabled = true;
            Application.Idle -= new EventHandler(FrameProcedure);
            Application.Idle -= new EventHandler(FrameProcedure2);
            camera.Dispose();
            dataGridView1.Update();
            dataGridView1.Refresh();
            
        }

        int EmpID = 0;
        private void FrameProcedure2(object sender, EventArgs e)
        {

            Users.Add("");
            Frame = camera.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            grayFace = Frame.Convert<Gray, Byte>();
            // MCvAvgComp[][] facesDetectedNow = grayFace.DetectHaarCascade(faceDetected, 1.2, 10, 
            //    Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));

            MCvAvgComp[][] facesDetectedNow = grayFace.DetectHaarCascade(
                faceDetected,
                1.1,
                10,
                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                new Size(20, 20));


            foreach (MCvAvgComp f in facesDetectedNow[0])
            {
                result = Frame.Copy(f.rect).Convert<Gray, Byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                Frame.Draw(f.rect, new Bgr(Color.Green), 2);

                if (trainningImages.ToArray().Length != 0)
                {
                    MCvTermCriteria termCriteria = new MCvTermCriteria(count, 0.001);
                    EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainningImages.ToArray(), labels.ToArray(),
                        2500, ref termCriteria);
                    name = recognizer.Recognize(result);
                    if (name.Length != 0)
                    {

                        Frame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.Green));

                        EmpID++;
                        //ใส่ตาราง
                        //ตั้งเวลาไว้ เมื่อ EmpID นับได้ถึง 20
                        if (EmpID == 20)
                        {
                            table.Rows.Add(int.Parse(name), DateTime.Now.ToString("HH:mm:ss"));
                            dataGridView1.DataSource = table;

                            //ส่งข้อมูลไปยังDB
                            string sqlDataSource = "Data Source=LAPTOP-3KQ0AE11\\MSSQLSERVER01;Initial Catalog=GUYs;Integrated Security=True;";
                            SqlDataReader myReader;
                            using (SqlConnection con = new SqlConnection(sqlDataSource))
                            {
                                for (int i = 0; i < dataGridView1.Rows.Count - 2; i++)
                                {

                                    using (SqlCommand cmd = new SqlCommand(@"INSERT INTO dbo.UserCheck VALUES ( '" + dataGridView1.Rows[i].Cells[0].Value + "', CAST(GETDATE() AS DATE), '" + dataGridView1.Rows[i].Cells[1].Value + "','');", con))
                                    {
                                        con.Open();
                                        myReader = cmd.ExecuteReader();
                                        table.Load(myReader);
                                        myReader.Close();
                                        con.Close();
                                    }
                                }
                            }
                            EmpID = 0;
                        }
                    }
                    else
                    {
                        Frame.Draw("Unknown", ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.Green));

                        // code สำหรับใช้จับหน้าเป็นชุดโดยผมตั้งไว้ให้เก็บหน้าไว้แค่ 20 ภาพ ต่อ ครั้ง
                        if (capture_mode == true && capture_count < 20)
                        {
                            count = count + 1;
                            grayFace = camera.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                            MCvAvgComp[][] detectedFaces = grayFace.DetectHaarCascade(
                                    faceDetected,
                                    1.2,
                                    10,
                                    Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                                    new Size(40, 40));


                            trainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                            trainningImages.Add(trainedFace);
                            labels.Add(tbxName.Text);
                            File.WriteAllText(Application.StartupPath + "/Faces/Faces.txt", trainningImages.ToArray().Length.ToString() + ",");
                            capture_count++;
                            //lblFace.Text = "# of faces : " + capture_count;
                            for (int i = 1; i < trainningImages.ToArray().Length + 1; i++)
                            {
                                trainningImages.ToArray()[i - 1].Save(Application.StartupPath + "/Faces/face" + i + ".bmp");
                                File.AppendAllText(Application.StartupPath + "/Faces/Faces.txt", labels.ToArray()[i - 1] + ",");
                            }
                        }
                        else
                        {
                            capture_count = 0;
                            capture_mode = false;
                            //btnCapture.Text = "Start";
                            //lblFace.Text = "# of faces : 0";
                        }
                    }


                }
                else
                {
                    Frame.Draw("Unknown", ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.Green));

                    // code สำหรับใช้จับหน้าเป็นชุดโดยผมตั้งไว้ให้เก็บหน้าไว้แค่ 20 ภาพ ต่อ ครั้ง
                    if (capture_mode == true && capture_count < 20)
                    {
                        count = count + 1;
                        grayFace = camera.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                        //MCvAvgComp[][] detectedFaces = grayFace.DetectHaarCascade(faceDetected, 1.2, 10,
                        //  Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));

                        MCvAvgComp[][] detectedFaces = grayFace.DetectHaarCascade(
                                faceDetected,
                                1.2,
                                10,
                                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                                new Size(40, 40));


                        trainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                        trainningImages.Add(trainedFace);
                        labels.Add(tbxName.Text);
                        File.WriteAllText(Application.StartupPath + "/Faces/Faces.txt", trainningImages.ToArray().Length.ToString() + ",");
                        capture_count++;
                        //lblFace.Text = "# of faces : " + capture_count;
                        for (int i = 1; i < trainningImages.ToArray().Length + 1; i++)
                        {
                            trainningImages.ToArray()[i - 1].Save(Application.StartupPath + "/Faces/face" + i + ".bmp");
                            File.AppendAllText(Application.StartupPath + "/Faces/Faces.txt", labels.ToArray()[i - 1] + ",");
                        }
                    }
                    else
                    {
                        capture_count = 0;
                        capture_mode = false;
                        //btnCapture.Text = "Start";
                        //lblFace.Text = "# of faces : 0";
                    }
                }

                //Users[t - 1] = name;
                Users.Add("");
            }

            cameraBox.Image = Frame;
            names = "";
            Users.Clear();


            //Frame.Draw(font.rect, new Bgr(Color.Green), 3);

        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            count = count + 1;
            grayFace = camera.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            //MCvAvgComp[][] detectedFaces = grayFace.DetectHaarCascade(faceDetected, 1.2, 10,
            //  Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));

            MCvAvgComp[][] detectedFaces = grayFace.DetectHaarCascade(
                    faceDetected,
                    1.2,
                    10,
                    Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                    new Size(40, 40));

            foreach (MCvAvgComp f in detectedFaces[0])
            {

                trainedFace = Frame.Copy(f.rect).Convert<Gray, Byte>();
                break;

            }

            trainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            trainningImages.Add(trainedFace);
            labels.Add(tbxName.Text);
            File.WriteAllText(Application.StartupPath + "/Faces/Faces.txt", trainningImages.ToArray().Length.ToString() + ",");

            for (int i = 1; i < trainningImages.ToArray().Length + 1; i++)
            {
                trainningImages.ToArray()[i - 1].Save(Application.StartupPath + "/Faces/face" + i + ".bmp");
                File.AppendAllText(Application.StartupPath + "/Faces/Faces.txt", labels.ToArray()[i - 1] + ",");
            }

            MessageBox.Show(tbxName.Text + " Added Successfully.");

        }
    }
}
