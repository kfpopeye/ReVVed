using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RVVD
{
    /// <summary>
    /// These are also used for analytics.
    /// </summary>
    class Constants
    {
        public static string GROUP_NAME = "ReVVed";
        public static string OPEN_FOLDER_MACRO_NAME = "Open Folder";
        public static string COMP_COMM_MACRO_NAME = "Component Commander";
        public static string PROJ_COMM_MACRO_NAME = "Project Commander";
        public static string MERGE_TEXT_MACRO_NAME = "Merge Text";
        public static string POLY_LINE_MACRO_NAME = "Poly Line";
        public static string WEB_LINK_MACRO_NAME = "Web Link";
        public static string CHANGE_CASE_MACRO_NAME = "Change Case";
        public static string UPPER_CASE_MACRO_NAME = "Upper Case";
        public static string GRID_FLIP_MACRO_NAME = "Grid Flip";
        public static string REVISIONIST_MACRO_NAME = "Revisionist";

        /// <summary>
        /// %localappdata%\pkhlineworks\ReVVed\
        /// </summary>
        public static string RevvedBasePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\pkhlineworks\\ReVVed\\";
    }
}
