using Autodesk.Revit.UI;

namespace RVVD
{
    /// <summary>
    /// Contextual help is only supported in Revit 2013 and up
    /// </summary>
    class Chelp
    {
        internal static ContextualHelp coco_help;
        internal static ContextualHelp of_help;
        internal static ContextualHelp pc_help;
        internal static ContextualHelp mt_help;
        internal static ContextualHelp pl_help;
        internal static ContextualHelp wl_help;
        internal static ContextualHelp cc_help;
        internal static ContextualHelp gf_help;
        internal static ContextualHelp rev_help;
        internal static ContextualHelp opt_help;
        internal static ContextualHelp help_help; //opens to default page

        internal static void initializeHelp()
        {
            string HelpPath = System.IO.Directory.GetParent(Properties.Settings.Default.AddinPath).FullName; //Contents
            string helpFile = LocalizationProvider.GetLocalizedValue<string>("HELP_FILE");

            coco_help = new ContextualHelp(ContextualHelpType.ChmFile, HelpPath + helpFile);
            coco_help.HelpTopicUrl = @"ComponentCommander.htm";

            of_help = new ContextualHelp(ContextualHelpType.ChmFile, HelpPath + helpFile);
            of_help.HelpTopicUrl = @"OpenFolder.htm";

            pc_help = new ContextualHelp(ContextualHelpType.ChmFile, HelpPath + helpFile);
            pc_help.HelpTopicUrl = @"ProjectCommander.htm";

            mt_help = new ContextualHelp(ContextualHelpType.ChmFile, HelpPath + helpFile);
            mt_help.HelpTopicUrl = @"MergeText.htm";

            pl_help = new ContextualHelp(ContextualHelpType.ChmFile, HelpPath + helpFile);
            pl_help.HelpTopicUrl = @"Polyline.htm";

            wl_help = new ContextualHelp(ContextualHelpType.ChmFile, HelpPath + helpFile);
            wl_help.HelpTopicUrl = @"WebLink.htm";

            cc_help = new ContextualHelp(ContextualHelpType.ChmFile, HelpPath + helpFile);
            cc_help.HelpTopicUrl = @"ChangeCase.htm";

            gf_help = new ContextualHelp(ContextualHelpType.ChmFile, HelpPath + helpFile);
            gf_help.HelpTopicUrl = @"GridFlip.htm";

            rev_help = new ContextualHelp(ContextualHelpType.ChmFile, HelpPath + helpFile);
            rev_help.HelpTopicUrl = @"Revisionist.htm";

            opt_help = new ContextualHelp(ContextualHelpType.ChmFile, HelpPath + helpFile);
            opt_help.HelpTopicUrl = @"Other.htm";

            help_help = new ContextualHelp(ContextualHelpType.ChmFile, HelpPath + helpFile);
            help_help.HelpTopicUrl = @"Welcome.htm";
        }
    }
}
