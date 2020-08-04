using System;
using System.Collections.Generic;
using System.Linq;
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
using System.IO;
using System.Reflection;

using Autodesk.Revit.UI;

namespace Project_Commander
{
    /// <summary>
    /// Interaction logic for Docking_Pane.xaml
    /// </summary>
    public partial class Docking_Pane : Page, Autodesk.Revit.UI.IDockablePaneProvider
    {
        private string m_projectName = string.Empty;
        private DateTime m_lastModified;
        private string m_XMLfilepath;

        private string safeProjectName
        {
            get
            {
                return pkhCommon.StringHelper.SafeForXML(m_projectName);
            }
        }

        public Docking_Pane()
        {
            InitializeComponent();
            m_XMLfilepath = RVVD.Constants.RevvedBasePath + "ProjectCommander.xml";
            if (File.Exists(m_XMLfilepath))
                m_lastModified = System.IO.File.GetLastWriteTime(m_XMLfilepath);
            else
                m_lastModified = DateTime.Now;
        }

        public void UpdateProject(string projName)
        {
            if (!File.Exists(m_XMLfilepath))                                                         //if no database exists
            {
                if (dockpanel1.IsInitialized)
                    dockpanel1.BeginInit();
                XmlDataProvider xdp = dockPage.FindResource("xmlsource") as XmlDataProvider;
                xdp.Document = new System.Xml.XmlDocument();
                System.Xml.XmlNode node = createBlankNode(m_projectName);
                dockpanel1.DataContext = node;
                dockpanel1.EndInit();
            }
            else if (projName == null ||                                                            //if user requests update from docking pane or open document is family
                m_projectName != projName ||                                                        //if a new project has been opened or
                m_lastModified.CompareTo(System.IO.File.GetLastWriteTime(m_XMLfilepath)) != 0)      //if the database has been update since last update
            {
                m_lastModified = System.IO.File.GetLastWriteTime(m_XMLfilepath);
                if(projName != null)
                    m_projectName = projName;

                if (dockpanel1.IsInitialized)
                    dockpanel1.BeginInit();
                XmlDataProvider xdp = dockPage.FindResource("xmlsource") as XmlDataProvider;
                xdp.Document = new System.Xml.XmlDocument();
                xdp.Document.Load(m_XMLfilepath);

                System.Xml.XmlNode node = xdp.Document.SelectSingleNode("//Project[@ProjectID='" + safeProjectName + "']");
                if (node == null)
                    node = createBlankNode(m_projectName);
                dockpanel1.DataContext = node;
                dockpanel1.EndInit();
            }

            tbUF1.Text = RVVD.Properties.Settings.Default.PC_UserField1;
            tbUF2.Text = RVVD.Properties.Settings.Default.PC_UserField2;
        }

        private System.Xml.XmlNode createBlankNode(string name)
        {
            System.Xml.XmlDocument d = new System.Xml.XmlDocument();
            d.LoadXml("<?xml version=\"1.0\" standalone=\"yes\"?><ProjectDataSet><Project ProjectID=\"" + safeProjectName + "\"><UserField1>This project has no</UserField1><UserField2>entry in the database</UserField2></Project></ProjectDataSet>");
            return d.SelectSingleNode("//Project[@ProjectID='" + safeProjectName + "']");
        }

        private void dockPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateProject(null);
        }

        //interface requirement
        public void SetupDockablePane(Autodesk.Revit.UI.DockablePaneProviderData data)
        {
            data.FrameworkElement = this as FrameworkElement;
            data.InitialState = new Autodesk.Revit.UI.DockablePaneState();
            data.InitialState.DockPosition = DockPosition.Tabbed;
            data.InitialState.TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser;
        }

        private void menuitem_Refresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateProject(null);
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class ProjectNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            return pkhCommon.StringHelper.ConvertFromXML(str);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
