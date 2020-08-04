using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit;

namespace RVVD.Polyline
{
    /// <summary>
    /// This class performs functions related to handling units of measure in Revit.
    /// It is a static class with static methods, which means the methods can be called without
    /// having to create an instance of the class first.
    /// Note: calls to this class must be wrapped in a transaction as this class uses SubTransactions
    /// </summary>
    
    public static class UnitFunctions
    {
        
        public const double MAX_ROUNDING_PRECISION = 0.000000000001;  // Seems to be the most precision Revit will allow

        // Converts length from internal units (decimal feet) to the units of the active project
        internal static string LengthToProjectUnits(double the_length, Document adoc)
        {
            return UnitFormatUtils.Format(adoc.GetUnits(), UnitType.UT_Length, the_length, true, false);
        }

        // Converts length from internal units (decimal feet) to the OPPOSITE units of the active project
        internal static string LengthToAlternateUnits(double the_length, Document adoc)
        {
            Units altUnits = null;

            if(adoc.DisplayUnitSystem == DisplayUnit.METRIC)
                altUnits = new Units(UnitSystem.Imperial);
            else
                altUnits = new Units(UnitSystem.Metric);

            return UnitFormatUtils.Format(altUnits, UnitType.UT_Length, the_length, true, false); 
        }

        /// <summary>
        /// Converts from internal units (decimal feet) to the units speified by the caller
        /// </summary>
        /// <param name="the_length">The length value to convert</param>
        /// <param name="adoc">The active document</param>
        /// <param name="dispunits">The display units to convert to (metirc\imperial)</param>
        /// <returns></returns>
        internal static string LengthToUnits(double the_length, Document adoc, DisplayUnit dispunits)
        {
            Units altUnits = null;

            if (dispunits == DisplayUnit.METRIC)
                altUnits = new Units(UnitSystem.Metric);
            else
                altUnits = new Units(UnitSystem.Imperial);

            return UnitFormatUtils.Format(altUnits, UnitType.UT_Length, the_length, true, false);
        }
    }
}
