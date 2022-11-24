using System; 
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls; 
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Collections.ObjectModel;

namespace Death_to_Blend1
{ 

    public class BlendOne
    {
        public BlendOne(string n, string p, long s)
        {
            Path = p;
            Size = s;
            Name = n;
        }

        public string Name { get; set; }

        public string Path { get; set; }
        public long Size { get; set; } 
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string currentDirectory;
        private long tmpTotalFileSize = 0;
        public ObservableCollection<BlendOne> BlendOnes { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            BlendOnes = new ObservableCollection<BlendOne>() { };
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

        }

        private void ButtonSetDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.Title = "Select Directory";
            dialog.IsFolderPicker = true;
            dialog.InitialDirectory = currentDirectory;

            dialog.AddToMostRecentlyUsedList = false;
            dialog.AllowNonFileSystemItems = false;
            dialog.DefaultDirectory = currentDirectory;
            dialog.EnsureFileExists = true;
            dialog.EnsurePathExists = true;
            dialog.EnsureReadOnly = false;
            dialog.EnsureValidNames = true;
            dialog.Multiselect = false;
            dialog.ShowPlacesList = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string folder = dialog.FileName;
                textBoxDirectory.Text = folder;
                ButtonScan.IsEnabled = true;
                BlendOnes.Clear();
                CurrentTaskStatus.Text = "Ready to scan";
            }

            ButtonDelete.IsEnabled = false;
        }

        private async void ButtonScan_Click(object sender, RoutedEventArgs e)
        {
            tmpTotalFileSize = 0;
            BlendOnes.Clear();
            ProgressCurrent.IsIndeterminate = true;

            await ScanDir(textBoxDirectory.Text, "*.blend1", SearchOption.AllDirectories, ProcessFile);

            ProgressCurrent.IsIndeterminate = false;
            
            if (BlendOnes.Count > 0)
            {
                ButtonDelete.IsEnabled = true;
                CurrentTaskStatus.Text = "Ready to delete " + BlendOnes.Count + " files with total size of " + CalculateSizeInHumanNumbers(tmpTotalFileSize);
            } else
            {
                string status = "Directory is clean";
                CurrentTaskStatus.Text = status;
                MessageBox.Show(status, "Clean", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }


        private async Task ScanDir(string path, string searchPattern, SearchOption searchOption, Func<string, Task> ProcessFile)
        {
            await Task.Run(() =>
            {
                Task.Yield();

                foreach (string file in Directory.EnumerateFiles(path, searchPattern, searchOption))
                {
                    Task.Run(() => ProcessFile(file));
                }
            });
        }


        private async Task ProcessFile(string file)
        {
            await Task.Run(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    FileInfo fi = new FileInfo(file);
                    BlendOnes.Add(new BlendOne(fi.Name, fi.DirectoryName, fi.Length));
                    CurrentTaskStatus.Text = "Discovered " + file;
                    tmpTotalFileSize += fi.Length;
                });
                
            });
        }
         

        void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            ProgressCurrent.Minimum = 0;
            ProgressCurrent.Maximum = BlendOnes.Count;
            int count = 1;
            foreach (BlendOne file in BlendOnes)
            {
                count++;
                string path = file.Path;
                string name = file.Name;
                var fi = new FileInfo(path + "/" + name);
                fi.Delete(); 
                CurrentTaskStatus.Text = "Deleted " + file.Path;
                ProgressCurrent.Value = count;
            }
            string status = "Deleted " + BlendOnes.Count + " files with total size of " + CalculateSizeInHumanNumbers(tmpTotalFileSize);
            CurrentTaskStatus.Text = status;
            ProgressCurrent.Value = 0;
            BlendOnes.Clear();
            tmpTotalFileSize = 0;

            MessageBox.Show(status, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private string CalculateSizeInHumanNumbers(long number)
        {

            long size = 0;
            // in GB
            if (number >= 1000000000)
            {
                size = number / 1000000000;
                return size.ToString() + " GB";
            }

            // in MB
            if (number >= 1000000 && number < 1000000000)
            {
                size = number / 1000000;
                return size.ToString() + " MB";
            }

            // in KB
            if (number >= 1000 && number < 1000000)
            {
                size = number / 1000;
                return size.ToString() + " KB";
            }

            return number.ToString() + "Bytes";
        }
         
    }
}
