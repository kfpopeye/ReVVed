using System;
using System.Collections.Generic;
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

namespace RVVD.Weblink
{
    /// <summary>
    /// Interaction logic for WebAddressChooser.xaml
    /// </summary>
    public partial class WebAddressChooser : Window
    {
        public string SelectectedAdress = null;
        public System.Collections.ObjectModel.ObservableCollection<Weblink.WebAddress> adressData { get { return data; } }
        System.Collections.ObjectModel.ObservableCollection<Weblink.WebAddress> data = null;

        public WebAddressChooser(System.Collections.ObjectModel.ObservableCollection<Weblink.WebAddress> adresses)
        {
            InitializeComponent();
            data = adresses;
            this.DataContext = this;
            adressList.SelectedIndex = 0;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            Weblink.WebAddress address = adressList.SelectedItem as Weblink.WebAddress;
            SelectectedAdress = address.Address;
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
