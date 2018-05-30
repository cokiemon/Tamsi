using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tamsi
{
    public class InstallerPackage
    {
        public string PackageType { get; set; }

        public string PackagePath { get; set; }

        public long PackageSize { get; set; }

        public string PackageSizeDisplay
        {
            get
            {
                //if ((PackageSize >= 0) && (PackageSize < 1028))
                //{
                //    return PackageSize.ToString() + " B";
                //}
                //else if ((PackageSize >= 1028) && (PackageSize < (1028 * 1028)))
                //{
                //    return (PackageSize / 1028).ToString() + " KB";
                //}
                //else
                //{
                //    return (PackageSize / (1028 * 1028)).ToString() + " MB";
                //}
                if ((PackageSize >= 0) && (PackageSize < 1028))
                {
                    return PackageSize.ToString() + " B";
                }
                else
                {
                    return (PackageSize / 1028).ToString() + " KB";
                }
            }
        }
    }
}
