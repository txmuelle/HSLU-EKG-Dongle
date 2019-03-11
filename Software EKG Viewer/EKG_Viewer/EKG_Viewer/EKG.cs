using System;
using System.IO.Ports;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;

namespace EKG_Viewer
{
    /// <summary>
    /// The ECG Class, to do the communication with the dongle
    /// 
    /// this class is based on the XMC ECG Project of Jost. H.
    /// </summary>
    class EKG
    {
        #region variables

        private bool varIsConnected;
        private bool varIsSampling;
        private byte[] LastSample_Array = new byte[27];
        private bool varAutoLeadOffEnabled;
        private bool ConfigChanged_field;

        #endregion

        #region Delegate

        public event NewDataHandler NewDataEvent;
        public delegate void NewDataHandler(int[] data);

        #endregion

        #region Properties
        public bool IsConnected
        {
            get { return varIsConnected; }
        }

        public bool IsSampling
        {
            get { return varIsSampling; }
        }

        public bool HasConfigChanged
        {
            get { return ConfigChanged_field; }
            set { ConfigChanged_field = value; }
        }

        public int BaudRate
        {
            get { return serialPort1.BaudRate; }
            set { serialPort1.BaudRate = value; }
        }

        public string PortName
        {
            get { return serialPort1.PortName; }
            set { serialPort1.PortName = value; }
        }


        public byte[] LastSample
        {
            get { return LastSample_Array; }
        }


        public bool AutoLeadOffEnabled
        {
            get { return varAutoLeadOffEnabled; }
            set { varAutoLeadOffEnabled = value; }
        }

        public int LOFFP;
        public int LOFFN;

        #endregion
        //_________________________Objekte Instaziieren___________________//

        private SerialPort serialPort1 = new SerialPort();  //Seriellen Port für die Kommunikation zum EKG hinzufügen
        public Queue Daten_FIFO = new Queue();              //Neuen FIFO Speicher für die speicherung der Samples

        //_________________________Standartkonstruktor___________________//
        #region constructor 
        public EKG()
        {

        }
        #endregion

        //_________________________Methoden___________________//
        #region Methodes
        public void ChangeConfic()
        {

        }

        public void Connect()
        {
            try
            {
                serialPort1.Open();
                serialPort1.DiscardInBuffer();//Buffer leeren
                SendByte(1);
            }
            catch (Exception ex)
            {
                serialPort1.Close();
                varIsConnected = false;
                throw new Exception(ex.Message);
            }

            try
            {
                WaitForACK();
            }
            catch (Exception ex)
            {
                serialPort1.Close();
                varIsConnected = false;
                throw new Exception(ex.Message + "\r\nSerieller Port weiterhin geschlossen!");
            }


            int ID;
            try
            {
                SendByte(4);
                ID = WaitForByte();
            }
            catch (Exception ex)
            {
                SendByte(2);
                serialPort1.Close();
                varIsConnected = false;
                throw new Exception(ex.Message + "\r\nSerieller Port weiterhin geschlossen!");
            }

            if (ID != 0xD)
            {
                SendByte(2);
                serialPort1.Close();
                varIsConnected = false;
                throw new Exception("Falsche Hardware ID!\r\nEingelesene ID: " + ID + "\r\nErwartete ID: 146");
            }


            varIsConnected = true;
        }

        public void Disconnect()
        {
            varIsConnected = false;
            serialPort1.DiscardInBuffer();

            try
            {

                SendByte(2);
            }
            catch (Exception ex)
            {
                serialPort1.Close();
                varIsConnected = false;
                throw new Exception(ex.Message + "\r\nSerieller Port wurde geschlossen! Evtl. hat die EKG Hardware das trennen der Verbindung nicht mitbekommen. Bitte EKG neu anschließen");
            }

            try
            {
                WaitForACK();
            }
            catch (Exception ex)
            {
                serialPort1.Close();
                varIsConnected = false;
                throw new Exception(ex.Message + "\r\nSerieller Port wurde geschlossen! Evtl. hat die EKG Hardware das trennen der Verbindung nicht mitbekommen. Bitte EKG neu anschließen");
            }

            varIsSampling = false;
            serialPort1.Close();
            varIsConnected = false;
        }

        /*
         * the sample Thread is used to get the samples from the Serial Port
         * 
         */
        private void PollSample()
        {

            Daten_FIFO.Clear();
            int cnt = 0;
            while (IsSampling == true)
            {

                if (serialPort1.BytesToRead > 0)
                {
                    int temp = serialPort1.ReadByte();
                    if (temp == 0xAB)
                    {
                        try
                        {
                            //Daten_FIFO.Enqueue(temp);

                            while (Daten_FIFO.Count < 27)
                            {
                                temp = serialPort1.ReadByte();
                                Daten_FIFO.Enqueue(temp);
                            }
                            temp = serialPort1.ReadByte();
                            if (temp == 0x0A)
                            {
                                //Thread Calculate_Thread = new Thread(CalculateSample);
                                //Calculate_Thread.IsBackground = true;
                                //Calculate_Thread.Start();
                                int[] data = new int[27];
                                for (int i = 0; i <= 26; i++)
                                {
                                    data[i] = (int)Daten_FIFO.Dequeue();
                                }
                                CalculateSample(data);
                            }
                            else
                            {
                                Daten_FIFO.Clear();
                            }
                        }
                        catch
                        {
                            throw new Exception("End-Byte nicht Empfangen!\r\nNach einem Sample wurde ein kein End-Byte empfangen!");
                        }

                    }
                    else
                    {
                        cnt++;
                        if (cnt > 56)
                        {
                            serialPort1.DiscardInBuffer();
                            cnt = 0;
                        }
                    }
                }



                //if (serialPort1.BytesToRead > 0)
                //{
                //    int temp = serialPort1.ReadByte();
                //    if (temp == 0x0A)
                //    {
                //        temp = serialPort1.ReadByte();
                //    }
                //    Daten_FIFO.Enqueue(temp);
                //}

                //if (Daten_FIFO.Count >= 27)
                //{
                //    //Thread Calculate_Thread = new Thread(CalculateSample);
                //    //Calculate_Thread.IsBackground = true;
                //    //Calculate_Thread.Start();
                //    int[] data = new int[27];
                //    for (int i = 0; i <= 26; i++)
                //    {
                //        data[i] = (int)Daten_FIFO.Dequeue();
                //    }
                //    CalculateSample(data);
                //    try
                //    {
                //        int temp = 0x00;
                //        while ( temp == 0x0A) {
                //            temp = serialPort1.ReadByte();
                //        } 
                //    }
                //    catch
                //    {
                //        throw new Exception("End-Byte nicht Empfangen!\r\nNach einem Sample wurde ein kein End-Byte empfangen!");
                //    }
                //}

            }
        }


        private void CalculateSample(int[] data)
        {

            // lead[0] = lead 2
            // lead[1] = lead 3
            // lead[2] = V1
            // lead[3] = V2
            // lead[4] = V3
            // lead[5] = V4
            // lead[6] = V5
            // lead[7] = V6

            int[] lead = new int[9];

            //____________LEAD2____________//
            lead[0] = (data[3] * 65535) + (data[4] * 256) + data[5];

            if (lead[0] >= 0x800000)
            {
                lead[0] -= 0x1000000;
            }
            lead[0] *= -1;


            //____________LEAD3____________//
            lead[1] = (data[6] * 65535) + (data[7] * 256) + data[8];

            if (lead[1] >= 0x800000)
            {
                lead[1] -= 0x1000000;
            }
            lead[1] *= -1;

            //____________V1____________//
            lead[2] = (data[9] * 65535) + (data[10] * 256) + data[11];

            if (lead[2] >= 0x800000)
            {
                lead[2] -= 0x1000000;
            }
            lead[2] *= -1;

            //____________V2____________//
            lead[3] = (data[12] * 65535) + (data[13] * 256) + data[14];

            if (lead[3] >= 0x800000)
            {
                lead[3] -= 0x1000000;
            }
            lead[3] *= -1;

            //____________V3____________//
            lead[4] = (data[15] * 65535) + (data[16] * 256) + data[17];

            if (lead[4] >= 0x800000)
            {
                lead[4] -= 0x1000000;
            }
            lead[4] *= -1;

            //____________V4____________//
            lead[5] = (data[18] * 65535) + (data[19] * 256) + data[20];

            if (lead[5] >= 0x800000)
            {
                lead[5] -= 0x1000000;
            }
            lead[5] *= -1;

            //____________V5____________//
            lead[6] = (data[21] * 65535) + (data[22] * 256) + data[23];

            if (lead[6] >= 0x800000)
            {
                lead[6] -= 0x1000000;
            }
            lead[6] *= -1;

            //____________V6____________//
            lead[7] = (data[24] * 65535) + (data[25] * 256) + data[26];

            if (lead[7] >= 0x800000)
            {
                lead[7] -= 0x1000000;
            }
            lead[7] *= -1;

            //____________StatusRegister___________//
            lead[8] = (data[0] * 65535) + (data[1] * 256) + data[2];



            NewDataEvent(lead);

        }

        private void WaitForACK()
        {
            Thread.Sleep(80);
            if (serialPort1.BytesToRead != 0)
            {
                if (serialPort1.ReadByte() == 6)
                {
                    return;
                }
                else
                {
                    throw new Exception("Falsches ACK!\r\nNach einem Befehl wurde ein falsches ACK-Byte empfangen!");
                }
            }
            else
            {
                throw new Exception("Acknowledgement Read Timeout!\r\nNach einem Befehl wurde kein ACK-Byte empfangen!");
            }
          
        }

        private int WaitForByte()
        {
            Thread.Sleep(30);
            if (serialPort1.BytesToRead != 0)
            {
                return (serialPort1.ReadByte());
            }
            else
            {
                throw new Exception("Serial Read Timeout!\r\nNach einer Anfrage wurde kein Byte empfangen!");
            }
        }

        private void SendByte(byte wert)
        {
            byte[] jaa = new byte[1] { wert };      //Neue Byte Variable

            serialPort1.Write(jaa, 0, 1);       //Sende Byte Variable
        }

        public void StartSampling()
        {
            try
            {
                serialPort1.DiscardInBuffer();
                SendByte(5);
                WaitForACK();
                varIsSampling = true;
                Thread Sample_Thread = new Thread(PollSample);
                Sample_Thread.IsBackground = true;
                Sample_Thread.Start();
            }
            catch (Exception ex)
            {
                SendByte(6);
                varIsSampling = false;
                throw new Exception(ex.Message + "\r\nSampling wurde nicht gestartet");
            }

        }

        public void LED_On()
        {
            try
            {
                serialPort1.DiscardInBuffer();
                SendByte(7);
                //WaitForACK();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\r\nLED nicht getoggelt");
            }

        }

        public void LED_Off()
        {
            try
            {
                serialPort1.DiscardInBuffer();
                SendByte(8);
                //WaitForACK();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\r\nLED nicht getoggelt");
            }

        }


        public void StopSampling()
        {
            try
            {
                varIsSampling = false;
                SendByte(6);
                //WaitForACK();
                serialPort1.DiscardInBuffer();

                //Sample_Thread.Abort();
            }
            catch (Exception ex)
            {
                SendByte(6);
                varIsSampling = false;
                throw new Exception(ex.Message + "\r\nSampling wurde nicht gestartet");
            }

        }

        #endregion


    }
}
