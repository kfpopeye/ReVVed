using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Autodesk.Revit.DB;

namespace RVVD.Polyline
{
    /// <summary>
    /// Interaction logic for Polyline_Window.xaml
    /// </summary>
    public partial class Polyline_Window : Window
    {
        float ScaleFactor;
        int SwitchEnd;
        double Xshift;
        double Yshift;
        RenderTargetBitmap Img;
        Line_Manager LineMngr;
        Document adoc = null;

        internal double polylineLength = 0;

        internal int segmentEnd
        {
            get { return SwitchEnd; }
        }

        internal Polyline_Window(Line_Manager line_mngr, Document actdoc)
        {
            InitializeComponent();

            LineMngr = line_mngr;
            polylineLength = LineMngr.TotalLength;
            adoc = actdoc;

            string puLength = UnitFunctions.LengthToProjectUnits(polylineLength, adoc);
            string altLen = UnitFunctions.LengthToAlternateUnits(polylineLength, adoc);
            lngth_label.Text = puLength + " (" + altLen + ")";
            length_textBox.Text = puLength;

            SwitchEnd = 0;
            ScaleFactor = 0; // used to scale line segments to fit picturebox

            float x_factor = (float)(pictureBox1.Width - 20) / line_mngr.width;
            float y_factor = (float)(pictureBox1.Height - 20) / line_mngr.height;
            if (x_factor < y_factor)
                ScaleFactor = Math.Abs(x_factor);
            else
                ScaleFactor = Math.Abs(y_factor);

            Xshift = (line_mngr.minX * -1); //used to shift lines left\right to 0,0
            Yshift = (line_mngr.minY * -1); //used to shift lines up\down to 0,0            
        }

        private void Redraw()
        {

            int x1; int y1; int x2; int y2;
            int w = pictureBox1.Width == 0 ? 220 : (int)pictureBox1.Width;
            int h = pictureBox1.Height == 0 ? 220 : (int)pictureBox1.Height;

            Img = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            DrawingVisual visual = new DrawingVisual();
            Pen p_black = new Pen(Brushes.Black, 1);
            Pen p_red = new Pen(Brushes.Red, 2);

            System.Collections.IEnumerator l_enum = LineMngr.GetLinesEnumerator();
            l_enum.Reset();

            using (DrawingContext r = visual.RenderOpen())
            {
                r.DrawRectangle(Brushes.Wheat, null, new Rect(0, 0, w, h));

                while (l_enum.MoveNext())
                {
                    Polyline.LineSegment l = l_enum.Current as Polyline.LineSegment;
                    x1 = (int)((l.Sx + Xshift) * ScaleFactor);
                    y1 = (int)((l.Sy + Yshift) * ScaleFactor);
                    x2 = (int)((l.Ex + Xshift) * ScaleFactor);
                    y2 = (int)((l.Ey + Yshift) * ScaleFactor);
                    x1 += 10; y1 += 10; x2 += 10; y2 += 10; // provides 10 pixel border
                    r.DrawLine(p_black, new System.Windows.Point(x1, y1), new System.Windows.Point(x2, y2));
                }

                Polyline.LineSegment endline = LineMngr.getEndLine(SwitchEnd);
                x1 = (int)((endline.Sx + Xshift) * ScaleFactor);
                y1 = (int)((endline.Sy + Yshift) * ScaleFactor);
                x2 = (int)((endline.Ex + Xshift) * ScaleFactor);
                y2 = (int)((endline.Ey + Yshift) * ScaleFactor);
                x1 += 10; y1 += 10; x2 += 10; y2 += 10;
                r.DrawLine(p_red, new System.Windows.Point(x1, y1), new System.Windows.Point(x2, y2));
            }

            Img.Render(visual);
            pictureBox1.Source = Img; //Note: image is lfipped vertically in XML to match coord systems
        }

        private void switch_button_Click(object sender, RoutedEventArgs e)
        {
            if (SwitchEnd == 0)
                SwitchEnd = 1;
            else
                SwitchEnd = 0;
            Redraw();
        }

        private void Update_button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Chelp.pl_help.Launch();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void length_textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!length_textBox.IsInitialized)
                return;

            bool result;
            double d = 0;

            result = UnitFormatUtils.TryParse(adoc.GetUnits(), UnitType.UT_Length, length_textBox.Text, out d);
            if (result)
                polylineLength = d;

            update_button.IsEnabled = result;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Redraw();
        }
    }
}
