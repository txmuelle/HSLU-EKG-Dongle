using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EKG_Viewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            ///Programmablauf:
            ///- auf Startbefehl warten
            ///- überpüfen ob Dongle angeschlossen
            ///- Messung starten
            ///for(;;){
            ///- Daten empfangen
            ///- im GUI ausgeben
            ///- Daten zwischenspeichern
            ///if(stopsignal) abbruch, Daten in Speicher schreiben
            ///}
        }
    }
}
