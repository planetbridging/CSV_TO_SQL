using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV_TO_SQL
{
    public class ObjCsv
    {

        public List<string> Data;
        private string SelectedFile;
        public ObjCsv(string f)
        {
            Data = new List<string>();
            SelectedFile = f;
            LoadFile();
        }

        private void LoadFile()
        {
            try
            {
                int counter = 0;
                string line;
                System.IO.StreamReader file =
                    new System.IO.StreamReader(SelectedFile);
                while ((line = file.ReadLine()) != null)
                {
                    Data.Add(line);
                    counter++;
                }

                file.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem loading file");
            }
        }
    }
}
