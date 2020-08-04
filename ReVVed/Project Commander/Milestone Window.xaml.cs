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

namespace RVVD.Project_Commander
{
    /// <summary>
    /// Interaction logic for Milestone_Window.xaml
    /// </summary>
    public partial class Milestone_Window : Window
    {
        public string textBox_Milestone
        {
            get
            {
                return textBoxMilestone.Text;
            }
        }

        public string dateTimePicker
        {
            get
            {
                return datePicker.Text;
            }
        }

        public Milestone_Window()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
