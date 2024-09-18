using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void GeneratePrusa(object sender, RoutedEventArgs e)
        {
            StartBambu.IsEnabled = false;
            StartPrusa.IsEnabled = false;
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Document"; // Default file name
            dialog.DefaultExt = ".hfp"; // Default file extension
            dialog.Filter = "Hueforge project (.hfp)|*.hfp"; // Filter files by extension
            dialog.DefaultDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                StatusLabel.Content = "Status : Generating .3mf for " + filename;                
                await Task.Run(() =>
                {
                    Tools.CreatePrusaPackage(filename);
                });
                StatusLabel.Content = "Status : Completed .3mf generation for " + filename;
            }
            else
                StatusLabel.Content = "Status : Idle";
            StartBambu.IsEnabled = true;
            StartPrusa.IsEnabled = true;
        }

        private async void GenerateBBL(object sender, RoutedEventArgs e)
        {
            StartBambu.IsEnabled = false;
            StartPrusa.IsEnabled = false;
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Document"; // Default file name
            dialog.DefaultExt = ".hfp"; // Default file extension
            dialog.Filter = "Hueforge project (.hfp)|*.hfp"; // Filter files by extension
            dialog.DefaultDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                StatusLabel.Content = "Status : Generating .3mf for " + filename;
                await Task.Run(() =>
                {
                    Tools.CreatePackage(filename);
                });
                StatusLabel.Content = "Status : Completed .3mf generation for " + filename;
            }
            else
                StatusLabel.Content = "Status : Idle";
            StartBambu.IsEnabled = true;
            StartPrusa.IsEnabled = true;
        }

        private async void PrusaDrop_Drop(object sender, DragEventArgs e)
        {
            string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            int projectsCount = 0;
            foreach (string filename in fileList)
            {
                if (filename.EndsWith(".hfp")) {
                    StatusLabel.Content = "Status : Generating .3mf for " + filename;
                    await Task.Run(() =>
                    {
                        Tools.CreatePackage(filename);
                        projectsCount++;
                    });
                    StatusLabel.Content = "Status : Completed .3mf generation for " + filename;
                }
                else
                    StatusLabel.Content = "Status : Skipped wrong file " + filename;
            }
            StatusLabel.Content = "Status : Completed .3mf generation for " + projectsCount + " projects.";
        }

        private async void BBLDrop_Drop(object sender, DragEventArgs e)
        {
            string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            int projectsCount = 0;
            foreach (string filename in fileList)
            {
                if (filename.EndsWith(".hfp"))
                {
                    StatusLabel.Content = "Status : Generating .3mf for " + filename;
                    await Task.Run(() =>
                    {
                        Tools.CreatePrusaPackage(filename);
                        projectsCount++;
                    });
                    StatusLabel.Content = "Status : Completed .3mf generation for " + filename;
                }
                else
                    StatusLabel.Content = "Status : Skipped wrong file " + filename;
            }
            StatusLabel.Content = "Status : Completed .3mf generation for " + projectsCount + " projects.";
        }
    }
}