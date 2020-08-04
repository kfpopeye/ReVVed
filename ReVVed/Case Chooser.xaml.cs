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

namespace RVVD
{
    /// <summary>
    /// Interaction logic for Case_Chooser.xaml
    /// </summary>
    public partial class Case_Chooser : Window
    {
        public enum CaseMode { Sentance, Upper, Lower, Toggle, Title }
        public CaseMode theMode = CaseMode.Upper;

        public Case_Chooser()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton r = sender as RadioButton;
            int i = int.Parse(r.Tag as string);
            theMode = (CaseMode)i;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
