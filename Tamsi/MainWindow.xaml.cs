using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Deployment.WindowsInstaller;

namespace Tamsi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataTable dt = initializeDataTable();

            gridPackages.AutoGenerateColumns = false;
            gridPackages.CanUserAddRows = false;
            gridPackages.CanUserDeleteRows = false;
            gridPackages.CanUserResizeRows = false;
            //gridPackages.DataContext = dt.DefaultView;
        }

        #region Properties.

        public IList<string> ProductLocalPackages { get; private set; }

        public IList<string> PatchLocalPackages { get; private set; }

        public IList<string> ProductUnknownPackages { get; private set; }

        public IList<string> PatchUnknownPackages { get; private set; }

        public IList<InstallerPackage> InstallerPackages { get; set; }

        public ObservableCollection<InstallerPackage> Packages { get; set; }

        #endregion

        private DataTable initializeDataTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("File", typeof(string));
            dt.Columns.Add("File Size", typeof(string));
            //dt.Columns.Add("Patient", typeof(string));
            //dt.Columns.Add("Date", typeof(DateTime));

            return dt;
        }

        private DataTable createDataTable(IList<InstallerPackage> packages)
        {
            DataTable dt = initializeDataTable();

            foreach (InstallerPackage package in packages)
            {
                string sizeDisplay = null;

                if (package.PackageSize > 1028)
                {
                    sizeDisplay = (package.PackageSize / 1028).ToString() + "KB";
                }
                else
                {
                    sizeDisplay = package.PackageSize.ToString() + "B";
                }

                dt.Rows.Add(package.PackagePath, sizeDisplay);
            }

            return dt;
        }

        private IList<string> listProductLocalPackages()
        {
            List<string> localPackages = new List<string>();

            foreach (ProductInstallation product in ProductInstallation.AllProducts)
            {
                localPackages.Add(product.LocalPackage);
            }

            return localPackages;
        }

        private IList<string> listPatchLocalPackages()
        {
            List<string> localPackages = new List<string>();

            foreach (PatchInstallation patch in PatchInstallation.AllPatches)
            {
                localPackages.Add(patch.LocalPackage);
            }

            return localPackages;
        }

        private IList<string> listUnknownPackages(string packagePattern, IList<string> knownPackages)
        {
            List<string> unknownPackages = new List<string>();
            string[] files = Directory.GetFiles(@"C:\Windows\Installer", packagePattern);

            foreach (string file in files)
            {
                if (!knownPackages.Contains(file, StringComparer.OrdinalIgnoreCase))
                {
                    unknownPackages.Add(file);
                }
            }

            return unknownPackages;
        }

        private string getMsiProductCode(string msiPath)
        {
            string productCodeQuery = "SELECT * FROM Property WHERE Property = 'ProductCode'";

            using (Session product = Installer.OpenPackage(msiPath, true))
            {
                using (View view = product.Database.OpenView(productCodeQuery))
                {
                    view.Execute();

                    using (Record record = view.Fetch())
                    {
                        return record.GetString(2);
                    }
                }
            }
        }

        private string getMsiProductCode(Database database)
        {
            string productCodeQuery = "SELECT * FROM Property WHERE Property = 'ProductCode'";

            using (View view = database.OpenView(productCodeQuery))
            {
                view.Execute();

                using (Record record = view.Fetch())
                {
                    return record.GetString(2);
                }
            }
        }

        private IList<string> verifyUnknownPackages(IList<string> unknownPackages)
        {
            List<string> packages = new List<string>();

            foreach (string file in unknownPackages)
            {
                bool isPackage = Installer.VerifyPackage(file);

                using (Database db = new Database(file, DatabaseOpenMode.ReadOnly))
                {
                    SummaryInfo summaryInfo = new SummaryInfo(file, false);

                    string code = getMsiProductCode(db);
                    ProductInstallation product = new ProductInstallation(code);

                    if (!product.IsInstalled)
                    {
                        packages.Add(file);
                    }
                }
            }

            return packages;
        }

        private IList<InstallerPackage> listInstallerPackages()
        {
            List<InstallerPackage> packages = new List<InstallerPackage>();

            foreach (string file in ProductUnknownPackages)
            {
                InstallerPackage package = new InstallerPackage();
                package.PackageType = "msi";
                package.PackagePath = file;

                FileInfo fileInfo = new FileInfo(file);
                package.PackageSize = fileInfo.Length;

                packages.Add(package);
            }

            return packages;
        }

        private string calculateTotalSize(IEnumerable<InstallerPackage> packages)
        {
            long size = 0;

            foreach (InstallerPackage package in packages)
            {
                size += package.PackageSize;
            }

            //if ((size >= 0) && (size < 1028))
            //{
            //    return size.ToString() + " B";
            //}
            //else if ((size >= 1028) && (size < (1028 * 1028)))
            //{
            //    return (size / 1028).ToString() + " KB";
            //}
            //else if ((size >= 1028 * 1028) && (size < (1028 * 1028 * 1028)))
            //{
            //    return (size / (1028 * 1028)).ToString() + " MB";
            //}
            //else
            //{
            //    return (size / (1028 * 1028 * 1028)).ToString() + " GB";
            //}

            if ((size >= 0) && (size < 1028))
            {
                return size.ToString() + " B";
            }
            else
            {
                return (size / 1028).ToString() + " KB";
            }
        }

        private void check()
        {
            string[] files = Directory.GetFiles(@"C:\Windows\Installer");

            IList<string> unknownPackages = listUnknownPackages("*.msi", ProductLocalPackages);
            ProductUnknownPackages = verifyUnknownPackages(unknownPackages);
            unknownPackages = listUnknownPackages("*.msp", PatchLocalPackages);
            //PatchUnknownPackages = verifyUnknownPackages(unknownPackages);
            PatchUnknownPackages = unknownPackages;

            IList<InstallerPackage> packages = listInstallerPackages();
            DataTable dt = createDataTable(packages);

            gridPackages.ItemsSource = packages;  
            //gridPackages.ItemsSource = dt.Rows;
            //gridPackages.DataContext = dt.DefaultView;

            totalSizeLabel.Content = "Total: " + calculateTotalSize(packages);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ProductLocalPackages = listProductLocalPackages();
            PatchLocalPackages = listPatchLocalPackages();

            check();
         }
    }
}
