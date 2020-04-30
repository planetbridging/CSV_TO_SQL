using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace CSV_TO_SQL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private List<string> LstSelectedFiles;
        private string SelectedFolderOutput;
        private static int[] StatusPercent = {10,20,30,40,50,60,70,80,90,100};
        private string Divider;
        private int ChunkSize;
        private string[] ExtractedTitles;
        private string SaveExtractedTitles;
        public MainWindow()
        {
            InitializeComponent();
            LstSelectedFiles = new List<string>();
            SelectedFolderOutput = "";
            Divider = "|";
            ChunkSize = 5000;
            SaveExtractedTitles = "";
        }

        

        //--------------------------------------------------------------------------listeners
        private void BtnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            SelectFiles();
        }

        private void BtnOutput_Click(object sender, RoutedEventArgs e)
        {
            SelectOutputFolder();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            StartProcessing();
        }

        private void TxtChunk_TextChanged(object sender, TextChangedEventArgs e)
        {
            DetectFalseInput();
        }

        private void BtnSplitFiles_Click(object sender, RoutedEventArgs e)
        {
            StartSplitingProcess();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StartExtraction();
        }

        private void BtnExtractNCombine_Click(object sender, RoutedEventArgs e)
        {

        }

       

        //--------------------------------------------------------------------------listeners


        //--------------------------------------------------------------------------BtnAddFiles Reactions
        private void SelectFiles()
        {
            LstSelectedFiles.Clear();
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string f in openFileDialog.FileNames)
                {
                    LstSelectedFiles.Add(f);
                }
                RefreshSelectedFiles();
            }
        }

        private void RefreshSelectedFiles()
        {
            LstFiles.Items.Clear();
            foreach (string f in LstSelectedFiles)
            {
                string fn = new FileInfo(f).Name;
                string fname_cleaned = System.IO.Path.GetFileNameWithoutExtension(fn);
                LstFiles.Items.Add(fname_cleaned);
            }
        }
        //--------------------------------------------------------------------------BtnOutput Reactions

        private void SelectOutputFolder()
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            DialogResult result = folderDialog.ShowDialog();
            if (result.ToString() == "OK")
            {
                SelectedFolderOutput = folderDialog.SelectedPath;
            }
        }

        //--------------------------------------------------------------------------BtnOutput Reactions


        //--------------------------------------------------------------------------BtnStart Reactions

        public delegate void DelegateLstUpdateStatus(string p);
        public delegate void DelegateUpdateTxtCurrentFileStatus(string p);

        private void LstUpdateStatus(string p)
        {
            TxtFileStatus.Content = p;
        }

        private void UpdateTxtCurrentFileStatus(string p)
        {
            TxtCurrentFileStatus.Content = p;
        }

        private void StartProcessing()
        {
            Divider = TxtDivider.Text;
            Thread test = new Thread(new ThreadStart(ProcessLoadedFiles));
            test.Start();
        }
        
        private void ProcessLoadedFiles()
        {
            int count = 1;
            int total = LstSelectedFiles.Count;
            foreach (string f in LstSelectedFiles)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                ObjCsv ocsv = new ObjCsv(f);

                List<string> tmp = new List<string>();
                int ccount = 1;
                int ctotal = ocsv.Data.Count;
                int currentpercent = 0;
                string titles = ArrayToSql(ocsv.Data[0].Split(new string[] { Divider }, StringSplitOptions.None),false);
                string tblname = System.IO.Path.GetFileNameWithoutExtension(f);

                Parallel.For(1, ocsv.Data.Count,
                   index => {
                       string d = ocsv.Data[index];
                       string[] dsplit = d.Split(new string[] { Divider }, StringSplitOptions.None);

                       string insertinto = "INSERT INTO " + tblname +" "+ titles.ToLower() + "VALUES";
                       insertinto += ArrayToSql(dsplit,true) + ";";
                       tmp.Add(insertinto);
                       ccount++;

                       int percentComplete = (int)Math.Round((double)(100 * ccount) / ctotal);

                       if (currentpercent != percentComplete && StatusPercent.Contains(percentComplete))
                       {
                           TxtCurrentFileStatus.Dispatcher.Invoke(
                               new DelegateUpdateTxtCurrentFileStatus(this.UpdateTxtCurrentFileStatus),
                               new object[] { percentComplete + "%" }
                           );
                           currentpercent = percentComplete;
                       }

                       
                   });
                string outputpath = SelectedFolderOutput + "\\" + tblname + ".sql";
                SaveFile(outputpath, tmp);
                TxtFileStatus.Dispatcher.Invoke(
                    new DelegateLstUpdateStatus(this.LstUpdateStatus),
                    new object[] { count + "/"  + total }
                );
                count++;
            }

            TxtFileStatus.Dispatcher.Invoke(
                    new DelegateLstUpdateStatus(this.LstUpdateStatus),
                    new object[] { "DONE" }
                );

            TxtCurrentFileStatus.Dispatcher.Invoke(
                    new DelegateUpdateTxtCurrentFileStatus(this.UpdateTxtCurrentFileStatus),
                    new object[] { "DONE" }
                );
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private string ArrayToSql(string[] d, Boolean indent)
        {
            string output = "(";
            for (int i = 0; i <= d.Length - 1; i++)
            {

                string dinput = d[i];
                if (dinput == "")
                {
                    dinput = "NULL";
                }
                else
                {
                    if (indent)
                    {
                        dinput = "'" + d[i] + "'";
                    }
                    else
                    {
                        dinput = d[i];
                    }
                    
                }

                if (i != d.Length - 1)
                {
                    output += dinput + ",";
                }
                else
                {
                    output += dinput + ")";
                }
            }
            return output;
        }

        private void SaveFile(string path, List<string> output)
        {
            using (StreamWriter sw = File.CreateText(path))
            {
                foreach (string l in output)
                {
                    sw.WriteLine(l);
                }
            }
        }
        //--------------------------------------------------------------------------BtnStart Reactions


        //--------------------------------------------------------------------------Split Reactions

        private static readonly Regex _regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        private void DetectFalseInput()
        {
            if (!IsTextAllowed(TxtChunkSize.Text))
            {
                System.Windows.MessageBox.Show("Must be a number");
            }
        }

        private void StartSplitingProcess()
        {
            ChunkSize = Int32.Parse(TxtChunkSize.Text);
            Thread test = new Thread(new ThreadStart(SplitFiles));
            test.Start();
        }

        private void SplitFiles()
        {
            for (int i = 0; i <= LstSelectedFiles.Count-1;i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                string fname_cleaned = System.IO.Path.GetFileNameWithoutExtension(LstSelectedFiles[i]);
                int chunknumber = 1;
                ObjCsv ocsv = new ObjCsv(LstSelectedFiles[i]);
                int count = 0;
                List<string> tmp = new List<string>();
                int currentpercent = 0;
                for (int d = 0; d <= ocsv.Data.Count - 1; d++)
                {
                    if (count <= ChunkSize)
                    {
                        tmp.Add(ocsv.Data[d]);
                    }
                    else
                    {
                        string tblname = "chunk_" + chunknumber +"_" + fname_cleaned + ".sql";
                        string outputpath = SelectedFolderOutput + "\\" + tblname + ".sql";
                        SaveFile(outputpath, tmp);
                        chunknumber++;
                        tmp.Clear();
                        count = 0;
                    }

                    int percentComplete = (int)Math.Round((double)(100 * d) / ocsv.Data.Count - 1);
                    if (currentpercent != percentComplete && StatusPercent.Contains(percentComplete))
                    {
                        TxtCurrentFileStatus.Dispatcher.Invoke(
                            new DelegateUpdateTxtCurrentFileStatus(this.UpdateTxtCurrentFileStatus),
                            new object[] { percentComplete + "%" }
                        );
                        currentpercent = percentComplete;
                    }

                    count++;
                }
                TxtFileStatus.Dispatcher.Invoke(
                    new DelegateLstUpdateStatus(this.LstUpdateStatus),
                    new object[] { i + "/" + (LstSelectedFiles.Count - 1) }
                );
            }
            Console.WriteLine("Done");
            TxtFileStatus.Dispatcher.Invoke(
                    new DelegateLstUpdateStatus(this.LstUpdateStatus),
                    new object[] { "DONE" }
                );

            TxtCurrentFileStatus.Dispatcher.Invoke(
                    new DelegateUpdateTxtCurrentFileStatus(this.UpdateTxtCurrentFileStatus),
                    new object[] { "DONE" }
                );
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


        //--------------------------------------------------------------------------Split Reactions


        //--------------------------------------------------------------------------BtnExtract Reactions
        private void StartExtraction()
        {
            ExtractedTitles = TxtExtractedTitles.Text.Split(new string[] { Divider }, StringSplitOptions.None);
            SaveExtractedTitles = TxtExtractedTitles.Text;
            Thread test = new Thread(new ThreadStart(ProcessExtraction));
            test.Start();
        }

        private void ProcessExtraction()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            for (int i = 0; i <= LstSelectedFiles.Count - 1; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                int total = LstSelectedFiles.Count;
                int currentpercent = 0;
                string fname_cleaned = System.IO.Path.GetFileNameWithoutExtension(LstSelectedFiles[i]);
                ObjCsv ocsv = new ObjCsv(LstSelectedFiles[i]);
                string[] titles = ocsv.Data[0].Split(new string[] { Divider }, StringSplitOptions.None);
                List<string> output = new List<string>();
                int count = 0;
                output.Add(SaveExtractedTitles);
                /*Parallel.For(1, ocsv.Data.Count,
                   index =>
                   {
                       
                   });*/

                for (int index = 1; index <= ocsv.Data.Count -1;index++)
                {
                    string[] orow = ocsv.Data[index].Split(new string[] { Divider }, StringSplitOptions.None);
                    string rowbuild = "";

                    if (orow.Length == titles.Length)
                    {
                        for (int t = 0; t <= titles.Length - 1; t++)
                        {
                            if (ExtractedTitles.Contains(titles[t]))
                            {
                                if (t == titles.Length - 1)
                                {
                                    rowbuild += orow[t];
                                }
                                else
                                {
                                    rowbuild += orow[t] + Divider;
                                }
                            }
                        }
                        output.Add(rowbuild);
                    }


                    int percentComplete = (int)Math.Round((double)(100 * count) / ocsv.Data.Count);

                    if (currentpercent != percentComplete && StatusPercent.Contains(percentComplete))
                    {
                        TxtCurrentFileStatus.Dispatcher.Invoke(
                            new DelegateUpdateTxtCurrentFileStatus(this.UpdateTxtCurrentFileStatus),
                            new object[] { percentComplete + "%" }
                        );
                        currentpercent = percentComplete;
                    }
                    count++;
                }
                string outputpath = SelectedFolderOutput + "\\" + fname_cleaned + "_Extracted.txt";
                output = RemoveDuplicates(output);
                SaveFile(outputpath, output);
                TxtFileStatus.Dispatcher.Invoke(
                    new DelegateLstUpdateStatus(this.LstUpdateStatus),
                    new object[] { i + "/" + total }
                );
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            TxtFileStatus.Dispatcher.Invoke(
                    new DelegateLstUpdateStatus(this.LstUpdateStatus),
                    new object[] { "DONE" }
                );

            TxtCurrentFileStatus.Dispatcher.Invoke(
                    new DelegateUpdateTxtCurrentFileStatus(this.UpdateTxtCurrentFileStatus),
                    new object[] { "DONE" }
                );
        }

        private List<string> RemoveDuplicates(List<string> l)
        {
            List<string> uniqueList = l.Distinct().ToList();
            return uniqueList;
        }

        //--------------------------------------------------------------------------BtnExtract Reactions


        //--------------------------------------------------------------------------BtnExtractNCombine Reactions
        private void StartExtractionNCombine()
        {
            ExtractedTitles = TxtExtractedTitles.Text.Split(new string[] { Divider }, StringSplitOptions.None);
            SaveExtractedTitles = TxtExtractedTitles.Text;
            Thread test = new Thread(new ThreadStart(ProcessExtraction));
            test.Start();
        }

        private void ProcessExtractionNCombine()
        {

        }

        //--------------------------------------------------------------------------BtnExtractNCombine Reactions
    }
}
