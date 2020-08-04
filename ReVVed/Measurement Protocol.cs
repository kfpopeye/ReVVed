using System;
using System.Windows;
using System.Reflection;
using System.ComponentModel;
using System.Net;
using System.IO;

using Autodesk.Revit.UI;

namespace RVVD
{
    //from https://developers.google.com/analytics/devguides/collection/protocol/v1/

    //https://www.google-analytics.com/collect
    //v=1                         // Version.
    //&tid=UA-XXXXX-Y             // Tracking ID / Property ID.
    //&cid=555                    // Anonymous Client ID.

    //&t=screenview               // Screenview hit type.
    //&an=ReVVed                  // App name.
    //&av=1.5.0                   // App version. get from assembly
    //&cd=Home                    // Screen name / content description.

    class Measurement_Protocol_OLD : IDisposable
    {
        private string payload = null;
        private Assembly TheAssembly = null;
        private HttpWebRequest myHttpWebRequest = null;

        public Measurement_Protocol_OLD()
        {
            TheAssembly = typeof(Measurement_Protocol_OLD).Assembly;           
        }

        public void sendAnalytic(string command, ExternalCommandData commandData)
        {
            if (Properties.Settings.Default.AllowAnalytics == false)
                return;
#if DEBUG
            Properties.Settings.Default.AllowAnalytics = false;
            Properties.Settings.Default.Save();
            TaskDialog.Show("Debug info", "Analytics turned off while in debug mode.");
#endif

            try
            {
                Autodesk.Revit.ApplicationServices.Application revitApp = commandData.Application.Application;
                payload = "https://www.google-analytics.com/collect?v=1" +
                    "&tid=UA-17553189-2" +
                    String.Format("&cid={0}", Properties.Settings.Default.AnonUId) +
                    "&t=screenview" +
                    "&an=" + AssemblyProduct.Replace(" ", "%20") +
                    String.Format("&av={0}.{1}.{2}.{3}", MajorVersion, MinorVersion, Build, Revision) + //of my extension
                    String.Format("&cd1={0}&cd2={1}", revitApp.VersionName, revitApp.VersionNumber) +    //tracks revit name and version in custom google dimensions
                    "&ul=" + LocalizationProvider.GetLocalizedValue<string>("ANALYTICS_language") +    //language
                    "&cd=" + command.Replace(" ", "%20");
                
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(payload);
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                myHttpWebResponse.Close();
            }
            catch(Exception)
            {
                //display message to user and shut off analytics

                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ANALYTICS_DIA001_Title")); //ReVVed User Experience
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ANALYTICS_DIA001_MainCont"); //An error has occured while gathering usage information.
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("ANALYTICS_DIA001_MainInstr"); //An error occured while trying to send usage information. This could be a temporary problem but please submit the bug report that will follow. Please select an option below:
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ANALYTICS_DIA001_Comm1")); //Turn off gathering for this session only."
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, LocalizationProvider.GetLocalizedValue<string>("ANALYTICS_DIA001_Comm2")); //Turn off permanently."
                td.FooterText = LocalizationProvider.GetLocalizedValue<string>("ANALYTICS_DIA001_Footer"); //You can turn this back on at anytime in the Options menu.
                TaskDialogResult tdr = td.Show();
                if (tdr == TaskDialogResult.CommandLink1)
                    Properties.Settings.Default.AnonUId = System.Guid.Empty;
                Properties.Settings.Default.AllowAnalytics = false;
                Properties.Settings.Default.Save();
                throw;
            }
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = TheAssembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(TheAssembly.CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return TheAssembly.GetName().Version.ToString();
            }
        }

        public string MajorVersion
        {
            get
            {
                return TheAssembly.GetName().Version.Major.ToString();
            }
        }

        public string MinorVersion
        {
            get
            {
                return TheAssembly.GetName().Version.Minor.ToString();
            }
        }

        public string Build
        {
            get
            {
                return TheAssembly.GetName().Version.Build.ToString();
            }
        }

        public string Revision
        {
            get
            {
                return TheAssembly.GetName().Version.Revision.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = TheAssembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = TheAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = TheAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = TheAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        public void Dispose()
        {
        }
    }
}
