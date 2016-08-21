using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.IO;

namespace PWM
{
    public partial class MainForm : Form
    {
        
        public MainForm()
        {
            InitializeComponent();

            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon1.BalloonTipText = "Нажмите, чтобы отобразить окно";
            notifyIcon1.BalloonTipTitle = "Подсказка";
            notifyIcon1.ShowBalloonTip(12);
            var ports = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
            GetPorts();
            try
            {
                comboBox1.Text = ports[2];
            }
            catch (IndexOutOfRangeException)
            {
                MessageBox.Show("Нет портов", "ОШИБКА", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        bool state = true, StateDiod=true;

        public delegate void MethodInvoke();
        private DateTime time;
        List<double> dat = new List<double>();
        double I = 0;
        int f1 = 0, f = 0, n = 0, f2 = 0;
        byte Hi = 0, Hj = 0, Li = 0, Lj = 0, StDi=0;

        //void SetZero(string PortName)
        //{ 
        //     using (var port = new Port(PortName))
        //    {
        //        port.SetData(zero);
        //     }
        //}

        byte[] zero = new byte[2]{0,0};               // сейчас не используется
        public void Check(string PortName)
        {
            File.Delete("result.txt");
            int i = 0;
            int j = 0;
            byte[] buf = new byte[2];

            using (var port = new Port(PortName))
            {
                port.SetData(zero);
                while (DateTime.Now.AddMinutes(0) <= time)
                {
                    f1 = port.GetData();
                    f = f1 & 1023;
                    I = ((f1 >> 16)/10000)/9.1;

                    if (f >= 1022)
                    {
                        timer1.Stop();
                        DialogResult result = MessageBox.Show("Превышено напряжение в 5 В", "ОШИБКА", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        if (result == DialogResult.OK)
                        {
                            Environment.Exit(0);
                        }
                    }


                    Other(f);
                    
                    try
                    {
                        if (((Convert.ToInt32((Convert.ToDouble(textBoxVolt.Text)) / (5.0 / 1024.0)) - f) < -6) || (Convert.ToInt32((Convert.ToDouble(textBoxVolt.Text)) / (5.0 / 1024.0)) - f) > 6)
                        {
                           
                            if (f < Convert.ToInt32(Convert.ToDouble(textBoxVolt.Text) / (5.0 / 1024.0)))
                            {
                                if (i == 1023)
                                {
                                    timer1.Stop();
                                    DialogResult result = MessageBox.Show("Превышена верхняя граница", "ОШИБКА", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                    port.SetData(zero);
                                    if (result == DialogResult.OK)
                                    {
                                        Environment.Exit(0);
                                    }
                                }
                                else
                                {
                                    if (state) 
                                    {
                                        i++;
                                    }
                                    else
                                    {
                                        j++;    
                                    }
          
                                }
                            }
                            else
                            {
                                if (i == 0)
                                {
                                    timer1.Stop();
                                    DialogResult result = MessageBox.Show("Превышена нижняя граница", "ОШИБКА", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                    port.SetData(zero);
                                    if (result == DialogResult.OK)
                                    {
                                        Environment.Exit(0);
                                    }
                                }
                                else
                                {
                                    if (state)
                                    {
                                        i--;
                                    }
                                    else
                                    {
                                        j--;
                                    }
                                }
                            }

                           
                            
                            Li = (byte)(i & 255);
                            Hi = (byte)(i >> 8);

                            Lj = (byte)(j & 255);
                            Hj = (byte)(j >> 8);
                            
                            
                            buf = new byte[2] { Li, Hi};
                            //buf = new byte[5] { Li, Hi, Lj, Hj, StDi };
                        }
                    }
                    catch (FormatException)
                    { MessageBox.Show("Неверный формат", "ОШИБКА", MessageBoxButtons.OK, MessageBoxIcon.Stop); }

                    catch (OverflowException)
                    { MessageBox.Show("Слишком большое/малое число", "ОШИБКА", MessageBoxButtons.OK, MessageBoxIcon.Stop); }

                    catch (Exception)
                    { MessageBox.Show("Введите допустимые значения", "ОШИБКА", MessageBoxButtons.OK, MessageBoxIcon.Stop); }

                    using (var writer = new StreamWriter("result.txt", true))
                    {
                        writer.Write(i);
                        writer.WriteLine();
                    }
                  //  dat.Add(f * (5.0 / 1024.0));
                    //if(h==9)
                    //{
                        Plot(f * (5.0 / 1024.0), I);
                    //}

                        Thread.Sleep(50);
                   // Thread.Sleep(300);
                    port.SetData(buf);

                    Digit(i);

                   

                    if (checkBox1.Checked == true && checkBox2.Checked == true)
                    {
                        if (n == 10)
                        {
                            StateDiod = !StateDiod;
                            n = 0;
                        }
                        n++;
                    }
                   



                    if (checkBox1.Checked && checkBox2.Checked==false)
                    {
                        StateDiod = true;
                    }
                    if (checkBox2.Checked && checkBox1.Checked == false)
                    {
                        StateDiod = false;
                    }
                    if (StateDiod)
                    {
                        StDi = 1;
                    }
                    else
                    {
                        StDi = 2;
                    }
                    if (checkBox1.Checked == false && checkBox2.Checked == false)
                    {
                        StDi = 0;
                    }
                }

                timer1.Stop();
                
                port.Dispose();
            }
        }

        int pointIndex = 0;//шаг сетки оп оси X
        //общее количество точек на графике
        int numberOfPointsInChart = 100;
        //количество точек в графике после удаления
        int numberOfPointsAfterRemoval = 100;
        double maxADC=0, maxI=0;
        void Plot(double ADC, double I)
        {
            Invoke((MethodInvoke)delegate()
            {
                chart1.Series[0].Points.AddXY(pointIndex+1,ADC);
                chart1.Series[1].Points.AddY(pointIndex+1,I);
                //chart1.Series[1].Points.AddXY(pointIndex+1, g / 500);
                //chart1.Series[2].Points.AddXY(pointIndex + 1, k / 500);
                ++pointIndex;
                while (chart1.Series[0].Points.Count > numberOfPointsInChart)
                {
                    while (chart1.Series[0].Points.Count > numberOfPointsAfterRemoval)
                    {
                        chart1.Series[0].Points.RemoveAt(0);
                    }
                    chart1.ChartAreas[0].AxisX.Minimum = pointIndex - numberOfPointsAfterRemoval;
                    chart1.ChartAreas[0].AxisX.Maximum = chart1.ChartAreas[0].AxisX.Minimum + numberOfPointsInChart;
                    if (ADC > maxADC)
                    {
                        maxADC = ADC;
                        chart1.ChartAreas[0].AxisY.Maximum = maxADC + 0.2;
                    }

                    if (I > maxI)
                    {
                        maxI = I;
                        chart1.ChartAreas[0].AxisY2.Maximum = maxI + 0.2;
                    }
                }
            });
        }
        void Digit(int i)
        {
            Invoke((MethodInvoke)delegate()
            {
                //label3.Text = (f).ToString() + "  принимаемое слово";
               // label9.Text = H.ToString() + "  отправляю";
                label10.Text = "ШИМ отправка = " + i.ToString();
                label14.Text = "Прием = " + f2.ToString();
                label9.Text = "Ток = " + I.ToString();
            });
        }

        void Other(int f)
        {
            Invoke((MethodInvoke)delegate()
            {
                label8.Text = ("потенциал:\n" + ((f * (5.0 / 1024.0))).ToString());
                label13.Text = "переключение ключа " + StDi.ToString();
            });
        }

        private void EnableControl(bool enable)
        {
            buttonStart.Enabled = enable;
            textBoxTime.Enabled = enable;
            comboBox1.Enabled = enable;
            buttonRefreshPort.Enabled = enable;
        }
        
        public void GetPorts()
        {
            var ports = Port.GetPortNames();
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(ports);
            try
            {
                comboBox1.Text = ports[0];
            }
            catch (IndexOutOfRangeException)
            {
                MessageBox.Show("Нет портов", "Ошибка ввода/вывода", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private async void buttonStart_Click(object sender, EventArgs e)
        {
            chart1.Series[0].Points.Clear();
            chart1.Series[1].Points.Clear();
            EnableControl(false);
            timer1.Start();
            try
            {
            trackBar1.Value = Convert.ToInt32(Convert.ToDouble(textBoxVolt.Text) / (5.0 / 1024.0));
            chart1.Series[0].Points.Clear();

            time = DateTime.Now.AddMinutes(Convert.ToDouble(textBoxTime.Text));
            }
            catch (FormatException)
            { MessageBox.Show("Неверный формат", "ОШИБКА", MessageBoxButtons.OK, MessageBoxIcon.Stop); }

            catch (OverflowException)
            { MessageBox.Show("Слишком большое/малое число", "ОШИБКА", MessageBoxButtons.OK, MessageBoxIcon.Stop); }

            catch (Exception)
            { MessageBox.Show("Введите допустимые значения", "ОШИБКА", MessageBoxButtons.OK, MessageBoxIcon.Stop); }

            var PortName = comboBox1.SelectedItem as string;
            await Task.Run(() => Check(PortName));
            EnableControl(true);
        }

        private void buttonRefreshPort_Click(object sender, EventArgs e)
        {
            GetPorts();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBoxVolt.Text = (Convert.ToDouble(trackBar1.Value) * (5.0 / 1024.0)).ToString();
        }

        
        private void timer1_Tick(object sender, EventArgs e)
        {
            DateTime dt = DateTime.Now;
            label4.Text = dt.Hour + ":" + dt.Minute + ":" + dt.Second;
            label5.Text = time.Hour + ":" + time.Minute + ":" + time.Second;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("Вы действительно хотите закрыть программу?", "PWM", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
            {
                Environment.Exit(0);
            }
            if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            state = !state;    
        }
    }
}
