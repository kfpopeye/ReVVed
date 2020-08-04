using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RVVD
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class web_link : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(web_link));

        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /*
             * This scans a selected element for a URL parameter and opens a mini-browser to surf to that URL
             */

            try
            {
                //using (Measurement_Protocol mp = new Measurement_Protocol())
                //{
                //    mp.sendAnalytic(Constants.WEB_LINK_MACRO_NAME, commandData);
                //}

                string addressToLoad = null;
                ElementSet sel = new ElementSet();
                foreach (ElementId eid in commandData.Application.ActiveUIDocument.Selection.GetElementIds())
                {
                    sel.Insert(commandData.Application.ActiveUIDocument.Document.GetElement(eid));
                }


                System.Collections.ObjectModel.ObservableCollection<Weblink.WebAddress> adresses = new System.Collections.ObjectModel.ObservableCollection<Weblink.WebAddress>();
                System.Collections.IEnumerator en = sel.GetEnumerator();
                en.MoveNext();
                FamilyInstance fi = en.Current as FamilyInstance;

                if (fi != null)
                {
                    foreach (Parameter parm in fi.Parameters)
                    {
                        if (parm.Definition.ParameterType == ParameterType.URL && parm.HasValue)
                            adresses.Add(new Weblink.WebAddress(parm));
                    }
                    FamilySymbol fs = fi.Symbol;
                    foreach (Parameter parm in fs.Parameters)
                    {
                        if (parm.Definition.ParameterType == ParameterType.URL && parm.HasValue)
                            adresses.Add(new Weblink.WebAddress(parm));
                    }
                    Family fml = fi.Symbol.Family;
                    foreach (Parameter parm in fml.Parameters)
                    {
                        if (parm.Definition.ParameterType == ParameterType.URL && parm.HasValue)
                            adresses.Add(new Weblink.WebAddress(parm));
                    }
                }
                else
                {
                    Element el = en.Current as Element;
                    if (el != null)
                    {
                        foreach (Parameter parm in el.Parameters)
                        {
                            if (parm.Definition.ParameterType == ParameterType.URL && parm.HasValue)
                                adresses.Add(new Weblink.WebAddress(parm));
                        }
                        ElementType et = commandData.Application.ActiveUIDocument.Document.GetElement(el.GetTypeId()) as ElementType;
                        if (et != null)
                        {
                            foreach (Parameter parm in et.Parameters)
                            {
                                if (parm.Definition.ParameterType == ParameterType.URL && parm.HasValue)
                                    adresses.Add(new Weblink.WebAddress(parm));
                            }
                        }
                    }
                }

                //check to see if any URL addresses were found
                if (adresses.Count == 0)
                {
                    TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("WL_Title"), "The selected element has no web adresses stored in it.");
                    log.InfoFormat("{0} found no web addresses", Constants.WEB_LINK_MACRO_NAME);
                    return Result.Failed;
                }
                else if (adresses.Count == 1)
                {
                    addressToLoad = adresses[0].Address;
                }
                else
                {
                    Weblink.WebAddressChooser form1 = new Weblink.WebAddressChooser(adresses);
                    IntPtr handle = commandData.Application.MainWindowHandle;
                    System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(form1);
                    wih.Owner = handle;
                    form1.ShowDialog();
                    if ((bool)form1.DialogResult)
                    {
                        addressToLoad = form1.SelectectedAdress;
                    }
                    log.InfoFormat("{0} found {1} web addresses", Constants.WEB_LINK_MACRO_NAME, adresses.Count.ToString());
                }

                //open address in browser
                if (addressToLoad != null && addressToLoad.Length > 0)
                {
                    log.InfoFormat("{0} is opening website at {1}", Constants.WEB_LINK_MACRO_NAME, addressToLoad);
                    if (Properties.Settings.Default.MiniBrowser)
                    {
                        pkhCommon.Windows.Browser thebrowser = new pkhCommon.Windows.Browser();
                        thebrowser.Show(addressToLoad);
                    }
                    else
                    {
                        System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                        myProcess.StartInfo.FileName = addressToLoad;
                        myProcess.StartInfo.UseShellExecute = true;
                        myProcess.StartInfo.RedirectStandardOutput = false;
                        myProcess.Start();
                        myProcess.Dispose();
                    }
                    return Result.Succeeded;
                }
                return Result.Cancelled;
            }
            catch (Exception err)
            {
                log.Error("Unexpected exception", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("WL_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());

                return Result.Failed;
            }
        }
    }

    public class wl_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            if (appdata.Application.Documents.Size == 0)
                return false;

            if (appdata.ActiveUIDocument.Selection.GetElementIds().Count != 1)
                return false;

            return true;
        }
    }
}