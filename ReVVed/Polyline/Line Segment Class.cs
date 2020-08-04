using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Debug = System.Diagnostics.Debug;

namespace RVVD.Polyline
{
    /// <summary>
    /// The line_segment class used by the line manager class
    /// </summary>
    internal class LineSegment
    {
        private Element lineelement = null;
        private LocationCurve locationcurve = null;
        private Autodesk.Revit.DB.XYZ startpoint = XYZ.Zero;
        private Autodesk.Revit.DB.XYZ endpoint = XYZ.Zero;
        private double line_length = 0;
        private bool joinedatstart = false, joinedatend = false;
        private Transform displayTransform = null;
        internal bool isLine = false;

        internal LineSegment(Element el)
        {
            locationcurve = el.Location as LocationCurve;
            lineelement = el;
            Line l = locationcurve.Curve as Line;

            if (l != null)
            {
                isLine = true;
                setDisplayTransform();

                if (locationcurve.Curve.IsBound)
                {
                    startpoint = locationcurve.Curve.GetEndPoint(0);
                    endpoint = locationcurve.Curve.GetEndPoint(1);
                }
                else
                {
                    startpoint = l.Origin;
                    endpoint = l.Origin.Add(l.Direction.Multiply(l.Length));
                }
                line_length = locationcurve.Curve.Length;

                ElementArray elar = locationcurve.get_ElementsAtJoin(0);    //TODO: also get arcs, circles, etc
                if (elar.IsEmpty)
                    joinedatstart = false;
                else
                    joinedatstart = true;

                elar = locationcurve.get_ElementsAtJoin(1);
                if (elar.IsEmpty)
                    joinedatend = false;
                else
                    joinedatend = true;

                Debug.WriteLine("Line Segment:" + startpoint.X.ToString() + " - " + startpoint.Y.ToString() + " - " + startpoint.Z.ToString() +
                    " End: " + endpoint.X.ToString() + " - " + endpoint.Y.ToString() + " - " + endpoint.Z.ToString());
            }
        }

        private void setDisplayTransform()
        {
            CurveElement ce = lineelement as CurveElement;
            Plane p = ce.SketchPlane.GetPlane();

            //rotate normal to match BasisZ
            Transform t;
            double a = XYZ.BasisZ.AngleTo(p.Normal);
            if (pkhCommon.Util.IsZero(a))
            {
                t = Transform.Identity;
                Debug.Print("Axis #1: Identity");
            }
            else
            {
                XYZ axis;
                if (pkhCommon.Util.IsEqual(a, Math.PI))
                {
                    Debug.Write("a = Math.PI - ");
                    axis = p.XVec;
                }
                else
                    axis = p.Normal.CrossProduct(XYZ.BasisZ);
                Debug.Print("Axis #1: {0} at angle {1}", axis.ToString(), a);
                t = Transform.CreateRotationAtPoint(axis, a, XYZ.Zero);
            }

            //rotate Xvec to match BasisX
            Transform t1;
            double a1 = XYZ.BasisX.AngleTo(p.XVec);
            if (pkhCommon.Util.IsZero(a1))
            {
                t1 = Transform.Identity;
                Debug.Print("Axis #2: Identity");
            }
            else
            {
                XYZ axis;
                if (pkhCommon.Util.IsEqual(a1, Math.PI))
                {
                    Debug.Write("a = Math.PI - ");
                    axis = p.Normal;
                }
                else
                    axis = p.YVec.CrossProduct(p.XVec);
                Debug.Print("Axis #2: {0} at angle {1}", axis.ToString(), a1);
                t1 = Transform.CreateRotationAtPoint(axis, a1, XYZ.Zero);
            }

            displayTransform = t.Multiply(t1);
        }

        private Element linestyle
        {
            get
            {
                CurveElement ce = lineelement as CurveElement;
                return ce.LineStyle;
            }
            set
            {
                CurveElement ce = lineelement as CurveElement;
                ce.LineStyle = value as Element;
            }
        }

        #region public accessors
        /// <summary>
        /// Sets the end join condition where this line meets the param line.
        /// </summary>
        internal void setEndJoinConditions(LineSegment ls, bool isEndLine)
        {
            if (startpoint.IsAlmostEqualTo(ls.startpoint) || startpoint.IsAlmostEqualTo(ls.endpoint))
            {
                joinedatstart = true;
                if (isEndLine) joinedatend = false;
            }
            if (endpoint.IsAlmostEqualTo(ls.startpoint) || endpoint.IsAlmostEqualTo(ls.endpoint))
            {
                joinedatend = true;
                if (isEndLine) joinedatstart = false;
            }
        }

        /// <summary>
        /// Match the linestyle parameter of this line to the rhs line.
        /// </summary>
        internal void MatchLineStyle(LineSegment rhs)
        {
            this.linestyle = rhs.linestyle;
        }

        internal List<XYZ> GetNewPoints(double newlength)
        {
            XYZ free_end = null;
            XYZ joined_end = null;
            XYZ tVector = null;

            if (!joinedatstart)
            {
                free_end = startpoint; joined_end = endpoint;
            }
            else
            {
                free_end = endpoint; joined_end = startpoint;
            }

            tVector = free_end.Subtract(joined_end).Normalize().Multiply(newlength);
            Transform set_length = Transform.CreateTranslation(tVector); //Transform.CreateTranslation() instead
            XYZ newendpoint = set_length.OfPoint(free_end);

            List<XYZ> pointslist = new List<XYZ>();
            pointslist.Add(newendpoint);
            pointslist.Add(joined_end);

            return pointslist;
        }

        internal bool Equals(LineSegment rhs)
        {
            if (startpoint.IsAlmostEqualTo(rhs.startpoint) && endpoint.IsAlmostEqualTo(rhs.endpoint) && (line_length == rhs.line_length))
                return true;
            return false;
        }

        internal bool joinedAtStart
        {
            get { return joinedatstart; }
            set { joinedatstart = value; }
        }

        internal bool joinedAtEnd
        {
            get { return joinedatend; }
            set { joinedatend = value; }
        }

        internal LocationCurve getLine()
        {
            return locationcurve;
        }

        internal Element element
        {
            get { return lineelement; }
        }

        internal double length
        {
            get { return Math.Round(line_length, 8); }
        }

        internal double Sx
        {
            get { return displayTransform.OfPoint(startpoint).X; }
        }

        internal double Sy
        {
            get { return displayTransform.OfPoint(startpoint).Y; }
        }

        internal double Ex
        {
            get { return displayTransform.OfPoint(endpoint).X; }
        }

        internal double Ey
        {
            get { return displayTransform.OfPoint(endpoint).Y; }
        }
        #endregion
    }
}