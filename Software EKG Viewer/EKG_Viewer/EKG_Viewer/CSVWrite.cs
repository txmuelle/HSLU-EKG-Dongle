using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace EKG_Viewer
{
    /// <summary>
    /// Class to store one CSV row
    /// </summary>
    public class CsvRow : List<string>
    {
        public string LineText { get; set; }
    }

    /// <summary>
    /// Class to write data to a CSV file
    /// </summary>
    public class CSVFileWriter : StreamWriter
    {
        public FileStream fs;
        public StreamWriter sw;
        public string date;
        public CSVFileWriter(Stream stream)
            : base(stream)
        {
        }
        public CSVFileWriter(string filename)
            : base(filename)
        {
            DateTime dateTime = DateTime.Now;
            date = dateTime.ToString("dd/MM/yyyy HH/mm/ss");
            fs = new FileStream(("Messung vom " + date + ".csv"), FileMode.Create);
            sw = new StreamWriter(fs);
        }



        /// <summary>
        /// Writes a single row to a CSV file.
        /// </summary>
        /// <param name="row">The row to be written</param>
        public void WriteRow(CsvRow row)
        {
            StringBuilder builder = new StringBuilder();
            bool firstColumn = true;
            foreach (string value in row)
            {
                // Add separator if this isn't the first value
                if (!firstColumn)
                    builder.Append(',');
                // Implement special handling for values that contain comma or quote
                // Enclose in quotes and double up any double quotes
                if (value.IndexOfAny(new char[] { '"', ',' }) != -1)
                    builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                else
                    builder.Append(value);
                firstColumn = false;
            }
            row.LineText = builder.ToString();
            WriteLine(row.LineText);
            writeonesample(row.LineText);
        }
        public void writeHeader()
        {
            try
            {

                String[] text = { "sep=, ", "EKG Messung vom " + date, "gemessen mit dem EKG Dongle der Hochschule Luzern BAT HS18", "", " ", " , Lead1, Lead2, Lead3, aVR, aVL, aVF, V1, V2, V3, V4, V5, V6" };
                for (int i = 0; i < text.Length; i++)
                {
                    sw.WriteLine(text[i]);
                }
                // sw.Close();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + " Speichern fehlgeschlagen!");
            }
        }
        public void writeonesample(string str)
        {
            try
            {
                sw.WriteLine(str);

            }
            catch (Exception e)
            {
                throw new Exception(e.Message + " Speichern fehlgeschlagen!");
            }
        }

        public void closeWriter()
        {
            sw.Close();
        }


#if false  //only for debuging
        public void WriteTest()
        {
            // Write sample data to CSV file
            using (CSVFileWriter writer = new CSVFileWriter("WriteTest.csv"))
            {
                for (int i = 0; i < 100; i++)
                {
                    CsvRow row = new CsvRow();
                    for (int j = 0; j < 5; j++)
                        row.Add(String.Format("Column{0}", j));
                    writer.WriteRow(row);
                }
            }
        } 
#endif
    }
}
