using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph; //Für ZedGraph
using System.IO.Ports; //Für Seriellen Port
using System.Threading; //Für Threading

namespace EKG_Viewer
    ///<summary>
    /// the form one is the Mainform of this Application
    /// 
    /// this class is based on the XMC EKG Project of Jost H.
    /// </summary>

{
    public partial class Form1 : Form
    {
        #region variables
        //---Globale Deklarationen---
        double zeit = 0;            //Variable für die Zeit des letzten samples

        EKG ekg1 = new EKG();   //Neue EKG Instanz
        bool beat = false;
        bool recording = false;
        public CSVFileWriter writer; //CSV File Writer
        bool fullscreen = false;

        //---Variablen für Filter---


        int i = 0;
        int[] zeiger = new int[8];
        double[] b = new double[12];
        double[] circular_buffer0 = new double[12];
        double[] circular_buffer1 = new double[12];
        double[] circular_buffer2 = new double[12];
        double[] circular_buffer3 = new double[12];
        double[] circular_buffer4 = new double[12];
        double[] circular_buffer5 = new double[12];
        double[] circular_buffer6 = new double[12];
        double[] circular_buffer7 = new double[12];

        double[,] SampleSaveBuffer = new double[100, 12];
        int SampleCount = 0;

        //for(i = 0; i < 11; i++)
        //circular_buffer[i] = 0;
        #endregion
        #region constructer
        public Form1()
        {
            InitializeComponent();                              //Zeichnet alle Elemente auf der Form

            foreach (string s in SerialPort.GetPortNames())     //Liest verfügbare Com Ports ein..
            {
                PORTBOX.Items.Add(s);                           //..und fügt sie zur Auswahl hinzu
            }

            BAUDBOX.SelectedItem = "115200";
            CH1TypeBox.SelectedItem = "Normal";
            CH2TypeBox.SelectedItem = "Normal";
            CH3TypeBox.SelectedItem = "Normal";
            CH4TypeBox.SelectedItem = "Normal";
            CH5TypeBox.SelectedItem = "Normal";
            CH6TypeBox.SelectedItem = "Normal";
            CH7TypeBox.SelectedItem = "Normal";
            CH8TypeBox.SelectedItem = "Normal";
            CH1GainBox.SelectedItem = "6";
            CH2GainBox.SelectedItem = "6";
            CH3GainBox.SelectedItem = "6";
            CH4GainBox.SelectedItem = "6";
            CH5GainBox.SelectedItem = "6";
            CH6GainBox.SelectedItem = "6";
            CH7GainBox.SelectedItem = "6";
            CH8GainBox.SelectedItem = "6";
            LOFF_Periode_Box.SelectedItem = "500ms";

            ekg1.NewDataEvent += new EKG.NewDataHandler(ekg1_NewDataEvent); //Abonieren des "New Data" Events


            // Filterkoeffizienten b[0] = b_N, ..., b[nc - 1] = b_0
            //			b[0] = -0.048916;
            //			b[1] = -0.066589;
            //			b[2] = -0.082893;
            //			b[3] = -0.096076;
            //			b[4] = -0.104652;
            //			b[5] =  0.796336;
            //			b[6] = -0.104652;
            //			b[7] = -0.096076;
            //			b[8] = -0.082893;
            //			b[9] = -0.066589;
            //			b[10] = 0.048916;

            b[0] = -0.021653307460162728;
            b[1] = -0.037981079732206075;
            b[2] = -0.062415260408351145;
            b[3] = -0.103191080649192538;
            b[4] = -0.190861991171255463;
            b[5] = -0.602132557843853444;
            b[6] = 0.602132557843853444;
            b[7] = 0.190861991171255463;
            b[8] = 0.103191080649192538;
            b[9] = 0.062415260408351145;
            b[10] = 0.037981079732206075;
            b[11] = 0.021653307460162728;

            // creat a new Write to write sample data to CSV file
            writer = new CSVFileWriter("test.txt");
            writer.writeHeader();

        }
        #endregion
        #region methodes
        //---Event bei laden der Main Form---
        void Form1FormLoad(object sender, EventArgs e) {
            SetupGraph();			//Aufrufen der SetupGraph Methode
        }
        //---Hier werden alle Einstellungen der Graph Pane vorgenommen---
        void SetupGraph()
        {
            GraphWindow.GraphPane.Title.IsVisible = false;
            GraphWindow.GraphPane.XAxis.Title.Text = "Sekunden";
            GraphWindow.GraphPane.XAxis.Title.FontSpec.Size = 6;
            GraphWindow.GraphPane.XAxis.Scale.FontSpec.Size = 6;
            GraphWindow.GraphPane.YAxis.Title.IsVisible = false;
            GraphWindow.GraphPane.Y2Axis.Title.IsVisible = false;
            //GraphWindow.GraphPane.YAxis.Scale.IsVisible = false;		//Für Y Scale
            GraphWindow.GraphPane.YAxis.Scale.FormatAuto = false;
            GraphWindow.GraphPane.Y2Axis.Scale.IsVisible = false;
            GraphWindow.GraphPane.Border.IsVisible = false;
            GraphWindow.GraphPane.Chart.Border.IsVisible = false;
            GraphWindow.GraphPane.Legend.Position = ZedGraph.LegendPos.Bottom;
            GraphWindow.GraphPane.Legend.IsVisible = false;


            //Farben des Graph Panes einstellen
            GraphWindow.GraphPane.Fill = new Fill(Color.DarkGray);
            GraphWindow.GraphPane.Chart.Fill = new Fill(Color.Black);
            GraphWindow.GraphPane.Chart.Fill.IsVisible = true;

            //Raster einstellen
            GraphWindow.GraphPane.XAxis.Scale.Min = -20;
            GraphWindow.GraphPane.XAxis.Scale.Max = 0;
            GraphWindow.GraphPane.YAxis.Scale.Min = ((-12) * trackBar2.Value);
            GraphWindow.GraphPane.YAxis.Scale.Max = trackBar2.Value;
            GraphWindow.GraphPane.XAxis.Scale.MinorStep = 0.25;
            GraphWindow.GraphPane.XAxis.Scale.MajorStep = 1;
            GraphWindow.GraphPane.YAxis.MajorGrid.IsVisible = true;
            GraphWindow.GraphPane.YAxis.MinorGrid.IsVisible = false;
            GraphWindow.GraphPane.XAxis.MajorGrid.IsVisible = true;
            GraphWindow.GraphPane.XAxis.MajorGrid.Color = Color.White;
            GraphWindow.GraphPane.XAxis.MinorGrid.IsVisible = true;
            GraphWindow.GraphPane.YAxis.MinorTic.IsOutside = false;
            GraphWindow.GraphPane.YAxis.MajorTic.IsOutside = false;
            GraphWindow.GraphPane.XAxis.MinorTic.IsOutside = false;
            GraphWindow.GraphPane.XAxis.MajorTic.IsOutside = false;

            //Erstelle je eine List für jede der 12 Ableitungen
            RollingPointPairList lead1_list = new RollingPointPairList(1000);
            RollingPointPairList lead2_list = new RollingPointPairList(1000);
            RollingPointPairList lead3_list = new RollingPointPairList(1000);
            RollingPointPairList aVR_list = new RollingPointPairList(1000);
            RollingPointPairList aVL_list = new RollingPointPairList(1000);
            RollingPointPairList aVF_list = new RollingPointPairList(1000);
            RollingPointPairList V1_list = new RollingPointPairList(1000);
            RollingPointPairList V2_list = new RollingPointPairList(1000);
            RollingPointPairList V3_list = new RollingPointPairList(1000);
            RollingPointPairList V4_list = new RollingPointPairList(1000);
            RollingPointPairList V5_list = new RollingPointPairList(1000);
            RollingPointPairList V6_list = new RollingPointPairList(1000);

            //Erstelle einen Graphen für jede der 12 Ableitungen
            LineItem lead1_graph = GraphWindow.GraphPane.AddCurve("L1", lead1_list, Color.FloralWhite, SymbolType.None);
            LineItem lead2_graph = GraphWindow.GraphPane.AddCurve("L2", lead2_list, Color.FloralWhite, SymbolType.None);
            LineItem lead3_graph = GraphWindow.GraphPane.AddCurve("L3", lead3_list, Color.FloralWhite, SymbolType.None);
            LineItem aVR_graph = GraphWindow.GraphPane.AddCurve("aVR", aVR_list, Color.FloralWhite, SymbolType.None);
            LineItem aVL_graph = GraphWindow.GraphPane.AddCurve("aVL", aVL_list, Color.FloralWhite, SymbolType.None);
            LineItem aVF_graph = GraphWindow.GraphPane.AddCurve("aVF", aVF_list, Color.FloralWhite, SymbolType.None);
            LineItem V1_graph = GraphWindow.GraphPane.AddCurve("V1", V1_list, Color.Pink, SymbolType.None);
            LineItem V2_graph = GraphWindow.GraphPane.AddCurve("V2", V2_list, Color.NavajoWhite, SymbolType.None);
            LineItem V3_graph = GraphWindow.GraphPane.AddCurve("V3", V3_list, Color.YellowGreen, SymbolType.None);
            LineItem V4_graph = GraphWindow.GraphPane.AddCurve("V4", V4_list, Color.PowderBlue, SymbolType.None);
            LineItem V5_graph = GraphWindow.GraphPane.AddCurve("V5", V5_list, Color.SandyBrown, SymbolType.None);
            LineItem V6_graph = GraphWindow.GraphPane.AddCurve("V6", V6_list, Color.MediumPurple, SymbolType.None);



            GraphWindow.AxisChange();
        }


        //---Event bei der Ankunft eines neuen Samples---
        void ekg1_NewDataEvent(int[] lead)
        {
            zeit += 0.02;           //Erhöhe die Zeit um eins
            Draw_Graph(lead);   //Rufe die Funktion zum Darstellen des Graphen auf
        }

        //---Methode zum Zeichnen des neuen Samples---
        void Draw_Graph(int[] lead)
        {
            // temp[0] = lead 2
            // temp[1] = lead 3
            // temp[2] = V1
            // temp[3] = V2
            // temp[4] = V3
            // temp[5] = V4
            // temp[6] = V5
            // temp[7] = V6
            // temp[8] = lead 1
            // temp[9] = aVR
            // temp[10] = aVL
            // temp[11] = aVR


            double[] temp = new Double[12];

            int abstand = 100;

            MethodInvoker gettrackbar2 = delegate
            {
                abstand = trackBar2.Value;
            };
            Invoke(gettrackbar2);



            //Lead2
            LineItem curve = GraphWindow.GraphPane.CurveList[1] as LineItem;
            IPointListEdit list = curve.Points as IPointListEdit;
            circular_buffer0[zeiger[0]] = lead[0];
            zeiger[0] = (zeiger[0] + 1) % 12;
            for (i = 0; i < 12; i++)
            {
                temp[0] += (b[i] * circular_buffer0[(zeiger[0] + i) % 12]);
            }
            if (Lead2Enabled.Checked == true)
            {
                list.Add(zeit, (temp[0] - abstand));
            }
            else
            {
                list.Clear();
            }


            //Lead3
            curve = GraphWindow.GraphPane.CurveList[2] as LineItem;
            list = curve.Points as IPointListEdit;
            circular_buffer1[zeiger[1]] = lead[1];
            zeiger[1] = (zeiger[1] + 1) % 12;
            for (i = 0; i < 12; i++)
            {
                temp[1] += (b[i] * circular_buffer1[(zeiger[1] + i) % 12]);
            }
            if (Lead3Enabled.Checked == true)
            {
                list.Add(zeit, (temp[1] - (2 * abstand)));
            }
            else
            {
                list.Clear();
            }

            //Lead1
            curve = GraphWindow.GraphPane.CurveList[0] as LineItem;
            list = curve.Points as IPointListEdit;
            temp[8] = (temp[0] - temp[1]);

            if (Lead1Enabled.Checked == true)
            {
                list.Add(zeit, temp[8]);
            }
            else
            {
                list.Clear();
            }

            //V1
            curve = GraphWindow.GraphPane.CurveList[6] as LineItem;
            list = curve.Points as IPointListEdit;
            if (V1Enabled.Checked == true)
            {
                circular_buffer2[zeiger[2]] = lead[2];
                zeiger[2] = (zeiger[2] + 1) % 12;
                for (i = 0; i < 12; i++)
                {
                    temp[2] += (b[i] * circular_buffer2[(zeiger[2] + i) % 12]);
                }
                list.Add(zeit, (temp[2] - (6 * abstand)));
            }
            else
            {
                list.Clear();
            }

            //V2
            curve = GraphWindow.GraphPane.CurveList[7] as LineItem;
            list = curve.Points as IPointListEdit;
            if (V2Enabled.Checked == true)
            {
                circular_buffer3[zeiger[3]] = lead[3];
                zeiger[3] = (zeiger[3] + 1) % 12;
                for (i = 0; i < 12; i++)
                {
                    temp[3] += (b[i] * circular_buffer3[(zeiger[3] + i) % 12]);
                }
                list.Add(zeit, (temp[3] - (7 * abstand)));
            }
            else
            {
                list.Clear();
            }

            //Beat detection
            if (temp[3] <= (-2000))
            {
                

                MethodInvoker beater = delegate
                {
                    label28.Visible = true;
                    ekg1.LED_On();

                };
                Invoke(beater);


                if (!beat)
                {
                    Thread piep_thread = new Thread(delegate () { piep(2000, 100); });
                    piep_thread.IsBackground = true;
                    piep_thread.Start();
                    beat = true;
                }



            }
            else
            {
                MethodInvoker nobeat = delegate
                {
                    label28.Visible = false;
                    ekg1.LED_Off();
                };
                Invoke(nobeat);
                beat = false;
            }

            //V3
            curve = GraphWindow.GraphPane.CurveList[8] as LineItem;
            list = curve.Points as IPointListEdit;
            if (V3Enabled.Checked == true)
            {
                circular_buffer4[zeiger[4]] = lead[4];
                zeiger[4] = (zeiger[4] + 1) % 12;
                for (i = 0; i < 12; i++)
                {
                    temp[4] += (b[i] * circular_buffer4[(zeiger[4] + i) % 12]);
                }
                list.Add(zeit, (temp[4] - (8 * abstand)));
            }
            else
            {
                list.Clear();
            }


            //V4
            curve = GraphWindow.GraphPane.CurveList[9] as LineItem;
            list = curve.Points as IPointListEdit;
            if (V4Enabled.Checked == true)
            {
                circular_buffer5[zeiger[5]] = lead[5];
                zeiger[5] = (zeiger[5] + 1) % 12;
                for (i = 0; i < 12; i++)
                {
                    temp[5] += (b[i] * circular_buffer5[(zeiger[5] + i) % 12]);
                }
                list.Add(zeit, (temp[5] - (9 * abstand)));
            }
            else
            {
                list.Clear();
            }

            //V5
            curve = GraphWindow.GraphPane.CurveList[10] as LineItem;
            list = curve.Points as IPointListEdit;
            if (V5Enabled.Checked == true)
            {
                circular_buffer6[zeiger[6]] = lead[6];
                zeiger[6] = (zeiger[6] + 1) % 12;
                for (i = 0; i < 12; i++)
                {
                    temp[6] += (b[i] * circular_buffer6[(zeiger[6] + i) % 12]);
                }
                list.Add(zeit, (temp[6] - (10 * abstand)));
            }
            else
            {
                list.Clear();
            }



            //V6
            curve = GraphWindow.GraphPane.CurveList[11] as LineItem;
            list = curve.Points as IPointListEdit;
            if (V6Enabled.Checked == true)
            {
                circular_buffer7[zeiger[7]] = lead[7];
                zeiger[7] = (zeiger[7] + 1) % 12;
                for (i = 0; i < 12; i++)
                {
                    temp[7] += (b[i] * circular_buffer7[(zeiger[7] + i) % 12]);
                }
                list.Add(zeit, (temp[7] - (11 * abstand)));
            }
            else
            {
                list.Clear();
            }



            //aVR
            curve = GraphWindow.GraphPane.CurveList[3] as LineItem;
            list = curve.Points as IPointListEdit;
            temp[9] = (-(temp[0] + temp[1])) / 2;

            if (aVREnabled.Checked == true)
            {
                list.Add(zeit, (temp[9] - (3 * abstand)));
            }
            else
            {
                list.Clear();
            }

            //aVL
            curve = GraphWindow.GraphPane.CurveList[4] as LineItem;
            list = curve.Points as IPointListEdit;
            temp[10] = ((temp[0] - temp[1]) / 2);

            if (aVLEnabled.Checked == true)
            {
                list.Add(zeit, (temp[10] - (4 * abstand)));
            }
            else
            {
                list.Clear();
            }


            //aVF
            curve = GraphWindow.GraphPane.CurveList[5] as LineItem;
            list = curve.Points as IPointListEdit;
            temp[11] = ((temp[1] + temp[2]) / 2);

            if (aVFEnabled.Checked == true)
            {
                list.Add(zeit, (temp[11] - (5 * abstand)));
            }
            else
            {
                list.Clear();
            }

            //Wenn Autoscroll aktiv ist
            if (checkBox6.Checked == true)
            {
                GraphAutoSize();
            }
            else
            {
                GraphWindow.AxisChange();
                GraphWindow.Invalidate();
            }

            if (recording)
            {
                //get the Data for one row
                CsvRow row = new CsvRow();
                row.Add(String.Format("{0}", SampleCount)); //#
                row.Add(String.Format(temp[8].ToString())); //Lead 1
                row.Add(String.Format(temp[0].ToString())); //Lead 2
                row.Add(String.Format(temp[1].ToString())); //Lead 3
                row.Add(String.Format(temp[9].ToString())); //aVR
                row.Add(String.Format(temp[10].ToString()));//aVL
                row.Add(String.Format(temp[11].ToString()));//aVF
                row.Add(String.Format(temp[2].ToString())); //V1
                row.Add(String.Format(temp[3].ToString())); //V2
                row.Add(String.Format(temp[4].ToString())); //V3
                row.Add(String.Format(temp[5].ToString())); //V4
                row.Add(String.Format(temp[6].ToString())); //V5
                row.Add(String.Format(temp[7].ToString())); //V6

                //write the row in the buffer
                writer.WriteRow(row);
                writer.Flush();

                SampleCount++;

                /*
                //Safe to file
                if (SampleCount < 10)
                {
                    for (i = 0; i < 12; i++)
                    {
                        SampleSaveBuffer[SampleCount, i] = temp[i];

                    }
                    SampleCount++;
                }
                else
                {

                    for (int i = 0; i < 10; i++)
                    {
                        //get the Data for one row
                        CsvRow row = new CsvRow();
                        row.Add(String.Format("{0}", i)); //#
                        row.Add(String.Format(SampleSaveBuffer[i, 8].ToString())); //Lead 1
                        row.Add(String.Format(SampleSaveBuffer[i, 0].ToString())); //Lead 2
                        row.Add(String.Format(SampleSaveBuffer[i, 1].ToString())); //Lead 3
                        row.Add(String.Format(SampleSaveBuffer[i, 9].ToString())); //aVR
                        row.Add(String.Format(SampleSaveBuffer[i, 10].ToString()));//aVL
                        row.Add(String.Format(SampleSaveBuffer[i, 11].ToString()));//aVF
                        row.Add(String.Format(SampleSaveBuffer[i, 2].ToString())); //V1
                        row.Add(String.Format(SampleSaveBuffer[i, 3].ToString())); //V2
                        row.Add(String.Format(SampleSaveBuffer[i, 4].ToString())); //V3
                        row.Add(String.Format(SampleSaveBuffer[i, 5].ToString())); //V4
                        row.Add(String.Format(SampleSaveBuffer[i, 6].ToString())); //V5
                        row.Add(String.Format(SampleSaveBuffer[i, 7].ToString())); //V6

                        //write the row in the buffer
                        writer.WriteRow(row); 
                    }

                    SampleCount = 0;
                }*/
            }



        }



        void GraphAutoSize()
        {
            Scale xScale = GraphWindow.GraphPane.XAxis.Scale;
            xScale.Max = zeit;
            MethodInvoker gettrackbar = delegate
            {
                xScale.Min = xScale.Max - trackBar1.Value;
            };
            Invoke(gettrackbar);

            GraphWindow.AxisChange();
            GraphWindow.Invalidate();
        }




        void piep(int ton, int dauer)
        {
            Console.Beep(ton, dauer);
        }





        void TrackBar1Scroll(object sender, EventArgs e)
        {
            Scale xScale = GraphWindow.GraphPane.XAxis.Scale;
            xScale.Min = xScale.Max - trackBar1.Value;
            //GraphResize();
            GraphWindow.AxisChange();
            GraphWindow.Invalidate();
        }


        void LOFF_Auswertung(int LOFFP, int LOFFN)
        {
            int temp;
            //LL
            if ((LOFFP & 3) == 0)
            {
                LOFF_LL_Button.BackColor = Color.OliveDrab;
            }
            else
            {
                LOFF_LL_Button.BackColor = Color.Peru;
            }

            //LA
            temp = LOFFN;
            if ((temp & 1) == 0)
            {
                LOFF_LA_Button.BackColor = Color.OliveDrab;
            }
            else
            {
                LOFF_LA_Button.BackColor = Color.Peru;
            }

            //RA
            temp = LOFFN;
            if ((temp & 2) == 0)
            {
                LOFF_RA_Button.BackColor = Color.OliveDrab;
            }
            else
            {
                LOFF_RA_Button.BackColor = Color.Peru;
            }

            //V1
            temp = LOFFN;
            if ((temp & 4) == 0)
            {
                LOFF_V1_Button.BackColor = Color.OliveDrab;
            }
            else
            {
                LOFF_V1_Button.BackColor = Color.Peru;
            }

            //V2
            temp = LOFFN;
            if ((temp & 8) == 0)
            {
                LOFF_V2_Button.BackColor = Color.OliveDrab;
            }
            else
            {
                LOFF_V2_Button.BackColor = Color.Peru;
            }

            //V3
            temp = LOFFN;
            if ((temp & 16) == 0)
            {
                LOFF_V3_Button.BackColor = Color.OliveDrab;
            }
            else
            {
                LOFF_V3_Button.BackColor = Color.Peru;
            }

            //V4
            temp = LOFFN;
            if ((temp & 32) == 0)
            {
                LOFF_V4_Button.BackColor = Color.OliveDrab;
            }
            else
            {
                LOFF_V4_Button.BackColor = Color.Peru;
            }

            //V5
            temp = LOFFN;
            if ((temp & 64) == 0)
            {
                LOFF_V5_Button.BackColor = Color.OliveDrab;
            }
            else
            {
                LOFF_V5_Button.BackColor = Color.Peru;
            }

            //V6
            temp = LOFFN;
            if ((temp & 128) == 0)
            {
                LOFF_V6_Button.BackColor = Color.OliveDrab;
            }
            else
            {
                LOFF_V6_Button.BackColor = Color.Peru;
            }
        }





        void TrackBar2Scroll(object sender, EventArgs e)
        {
            Scale yScale = GraphWindow.GraphPane.YAxis.Scale;
            yScale.Min = ((-12) * trackBar2.Value);
            yScale.Max = trackBar2.Value;
            //GraphResize();
            GraphWindow.AxisChange();
            GraphWindow.Invalidate();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writer.closeWriter();
        }

        private void PORT_aktualisieren_Click(object sender, EventArgs e)
        {
            PORTBOX.Items.Clear();
            foreach (string s in SerialPort.GetPortNames())
            {
                PORTBOX.Items.Add(s);
            }
        }

        private void COM_open_button_Click(object sender, EventArgs e)

        {
            if (ekg1.IsConnected == false)
            {
                try
                {
                    ekg1.PortName = PORTBOX.Text;
                    ekg1.BaudRate = int.Parse(BAUDBOX.Text);
                    ekg1.Connect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                if (ekg1.IsConnected == true)
                {
                    COM_status.Text = "CONNECTED";
                    COM_status.BackColor = Color.YellowGreen;
                    PORT_aktualisieren.Enabled = false;
                    PORTBOX.Enabled = false;
                    BAUDBOX.Enabled = false;
                    COM_open_button.Text = "Disconnect";
                    Start_Sampling_Button.Enabled = true;
                    //Form1.ActiveForm.Text = "EKG Viewer - Connected";
                    Thread piep_thread = new Thread(delegate () { piep(2000, 100); });
                    piep_thread.IsBackground = true;
                    piep_thread.Start();
                }
                else
                {
                    COM_status.Text = "NOT CONNECTED";
                    COM_status.BackColor = Color.Peru;
                    PORT_aktualisieren.Enabled = true;
                    PORTBOX.Enabled = true;
                    BAUDBOX.Enabled = false;
                    COM_open_button.Text = "Connect";
                    Form1.ActiveForm.Text = "EKG Viewer";
                }
            }

            else if (ekg1.IsConnected == true)
            {
                try
                {
                    ekg1.Disconnect();
                    COM_status.Text = "NOT CONNECTED";
                    COM_status.BackColor = Color.Peru;
                    PORT_aktualisieren.Enabled = true;
                    PORTBOX.Enabled = true;
                    BAUDBOX.Enabled = false;
                    COM_open_button.Text = "Connect";
                    Start_Sampling_Button.Enabled = false;
                    Form1.ActiveForm.Text = "EKG Viewer";
                    Thread piep_thread = new Thread(delegate () { piep(1500, 100); });
                    piep_thread.IsBackground = true;
                    piep_thread.Start();
                }
                catch (Exception ex)
                {
                    COM_status.Text = "EKG Reset needed!";
                    COM_status.BackColor = Color.Red;
                    PORT_aktualisieren.Enabled = true;
                    PORTBOX.Enabled = true;
                    BAUDBOX.Enabled = false;
                    COM_open_button.Text = "Connect";
                    Start_Sampling_Button.Enabled = false;
                    Form1.ActiveForm.Text = "EKG Viewer - EKG Reset needed!";
                    Thread piep_thread = new Thread(delegate () { piep(3000, 500); });
                    piep_thread.IsBackground = true;
                    piep_thread.Start();
                    MessageBox.Show(ex.Message);
                }
            }

        }

        private void Start_Sampling_Button_Click(object sender, EventArgs e)
        {
            if (ekg1.HasConfigChanged == true)
            {
                ekg1.ChangeConfic();
            }

            if (ekg1.IsSampling == false)
            {
                try
                {
                    ekg1.StartSampling();
                    SampleStatusLabel.Text = "SAMPLING";
                    SampleStatusLabel.BackColor = Color.YellowGreen;
                    Thread piep_thread = new Thread(delegate () { piep(2000, 100); });
                    piep_thread.IsBackground = true;
                    piep_thread.Start();
                    COM_open_button.Enabled = false;
                    Start_Sampling_Button.Text = "STOP";
                    TabControl1.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    SampleStatusLabel.Text = "ERROR";
                    SampleStatusLabel.BackColor = Color.Red;
                    Thread piep_thread = new Thread(delegate () { piep(3000, 500); });
                    piep_thread.IsBackground = true;
                    piep_thread.Start();
                    COM_open_button.Enabled = true;
                    Start_Sampling_Button.Text = "START";
                    TabControl1.Enabled = true;

                }
            }
            else
            {
                try
                {
                    ekg1.StopSampling();
                    SampleStatusLabel.Text = "STOPPED";
                    SampleStatusLabel.BackColor = Color.Peru;
                    Thread piep_thread = new Thread(delegate () { piep(1500, 100); });
                    piep_thread.IsBackground = true;
                    piep_thread.Start();
                    COM_open_button.Enabled = true;
                    Start_Sampling_Button.Text = "START";
                    TabControl1.Enabled = true;
                    recording = false;
                    radioButton1.Checked = false;
                    radioButton1.Text = "recording stopped";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    SampleStatusLabel.Text = "ERROR";
                    SampleStatusLabel.BackColor = Color.Red;
                    Thread piep_thread = new Thread(delegate () { piep(3000, 500); });
                    piep_thread.IsBackground = true;
                    piep_thread.Start();
                    COM_open_button.Enabled = true;
                    Start_Sampling_Button.Text = "START";
                    TabControl1.Enabled = true;

                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(button1.Text== "Enable all") { 
            Lead1Enabled.Checked = true;
            Lead2Enabled.Checked = true;
            Lead3Enabled.Checked = true;
            aVREnabled.Checked = true;
            aVLEnabled.Checked = true;
            aVFEnabled.Checked = true;
            V1Enabled.Checked = true;
            V2Enabled.Checked = true;
            V3Enabled.Checked = true;
            V4Enabled.Checked = true;
            V5Enabled.Checked = true;
            V6Enabled.Checked = true;
            button1.Text = "Disable all";
            }
            else
            {
                Lead1Enabled.Checked = false;
                Lead2Enabled.Checked = false;
                Lead3Enabled.Checked = false;
                aVREnabled.Checked = false;
                aVLEnabled.Checked = false;
                aVFEnabled.Checked = false;
                V1Enabled.Checked = false;
                V2Enabled.Checked = false;
                V3Enabled.Checked = false;
                V4Enabled.Checked = false;
                V5Enabled.Checked = false;
                V6Enabled.Checked = false;
                button1.Text = "Enable all";
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {

            try
            {
                if (SampleStatusLabel.Text == "SAMPLING") {
                    ekg1.StopSampling();
                    ekg1.Disconnect();
                }
                Close();
            }
            catch (Exception)
            {

            }

        }

        #endregion

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about1 = new About();
            about1.Show();
        }

        private void fullscreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
           

            if(fullscreen){
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.Bounds = Screen.PrimaryScreen.Bounds;
                this.fullscreen = false;
                this.fullscreenToolStripMenuItem.Text = "Exit Full Screen";
            }
            
            else
            {
               this.WindowState = FormWindowState.Maximized;
               this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                this.fullscreen = true;
                this.fullscreenToolStripMenuItem.Text = "Full Screen";
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                recording = true;
                radioButton1.Text = "recording...";
            }
            else
            {
                recording = false;
                radioButton1.Text = "recording stopped";
                radioButton1.Checked = false;
            }

        }

        private void radioButton1_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                radioButton1.Checked = true;
            }
            else
            {
                radioButton1.Checked = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            writer.closeWriter();
            try
            {
                ekg1.StopSampling();
                ekg1.Disconnect();
            }
            catch (Exception)
            { }
        }
    }
}
