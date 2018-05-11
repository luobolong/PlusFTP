using System.Windows;

namespace PlusFTP.Windows
{
    public partial class VerifyCertificateWindow : MahApps.Metro.Controls.MetroWindow
    {
        private VerifyCertificateWindow(Window owner)
        {
            this.Owner = owner;
            InitializeComponent();
        }
    }
}