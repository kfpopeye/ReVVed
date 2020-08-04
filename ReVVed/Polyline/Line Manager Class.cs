using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RVVD.Polyline
{
    /// <summary>
    /// This class performs functions related to handling line elements via the line_segment class
    /// </summary>
    internal class Line_Manager
    {
        private List<LineSegment> lines;
        private double lengthofpolyline;
        private double min_X;
        private double min_Y;
        private double max_X;
        private double max_Y;
        private LineSegment start_line;
        private LineSegment end_line;
        private ElementSet user_selected_lines;
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(poly_line));

        #region public accessors
        internal bool SetPolylineLength(int end, double newlength, Document doc)
        {
            LineSegment the_end = null;
            double delta = newlength - lengthofpolyline;
            Line c = null;
            CurveElement ce = null;
            List<XYZ> plist = null;

            if (end == 0)
                the_end = start_line;
            else
                the_end = end_line;

            if (delta == 0)                         // length is NOT changing
                return true;
            if (delta > 0)                           // length is getting longer
                plist = the_end.GetNewPoints(delta);
            if (delta < 0)                          // length is getting shorter
            {
                if (Math.Abs(delta) > the_end.length)
                {
                    // if the change in length (delta) is longer than the last line segment we have to delete the segment
                    // and get the next segment to repeat this test
                    while (Math.Abs(delta) > the_end.length)
                    {
                        LineSegment nextline = null;
                        if (end == 0)
                        {
                            nextline = lines[1] as LineSegment;
                            nextline.setEndJoinConditions(the_end, false);
                            lines.RemoveAt(0);
                            lines.TrimExcess();
                        }
                        else
                        {
                            nextline = lines[lines.Count - 2] as LineSegment;
                            nextline.setEndJoinConditions(the_end, false);
                            lines.RemoveAt(lines.Count - 1);
                            lines.TrimExcess();
                        }
                        delta += the_end.length;
                        doc.Delete(the_end.element.Id);
                        the_end = nextline;
                    }
                    plist = the_end.GetNewPoints(delta);
                }
                else
                    plist = the_end.GetNewPoints(delta);
            }

            c = Line.CreateBound(plist[0] as XYZ, plist[1] as XYZ); //Use Line.CreateBound() instead.
            if (c == null)
                throw new NullReferenceException("Line was null in Line_Manager.SetPolylineLength");

            ModelLine ml = the_end.element as ModelLine;  // check to see if the line is model or detail

            if (ml == null)
                ce = doc.Create.NewDetailCurve(doc.ActiveView, c);
            else
            {
                SketchPlane sp = ml.SketchPlane;
                ce = doc.Create.NewModelCurve(c, sp) as ModelLine;
            }

            if (ce == null)
                return false;
            else
            {
                LineSegment newline = new LineSegment(ce as Element);
                newline.MatchLineStyle(the_end);
                doc.Delete(the_end.element.Id);
                return true;
            }
        }

        internal LineSegment getEndLine(int end)
        {
            if (end == 0)
                return start_line;
            else
                return end_line;
        }

        internal float width
        {
            get { return (float)(max_X - min_X); }
        }

        internal float height
        {
            get { return (float)(max_Y - min_Y); }
        }

        internal double minX
        {
            get { return min_X; }
        }

        internal double minY
        {
            get { return min_Y; }
        }

        internal int TotalLines
        {
            get { return lines.Count; }
        }

        internal double TotalLength
        {
            get { return lengthofpolyline; }
        }

        internal bool HasPath
        {
            get
            {
                if (start_line == null || end_line == null)
                    return false;
                return true;
            }
        }
        #endregion

        // constructor
        internal Line_Manager(ElementSet lineSet, Autodesk.Revit.DB.View aview)
        {
            StringBuilder errData = new StringBuilder("\n\nLine Data\n");
            int x = 0;

            user_selected_lines = lineSet;
            start_line = null;
            end_line = null;
            lengthofpolyline = 0;
            lines = new List<LineSegment>();

            foreach (Element e in lineSet)
            {
                LocationCurve lc = e.Location as LocationCurve;
                Line l = lc.Curve as Line;

                int start = 0, end = 0;
                start = lc.get_ElementsAtJoin(0).Size;
                end = lc.get_ElementsAtJoin(1).Size;

                if (start == 0 && end == 0) // if both ends have no lines joined to it. This is a 'T' join filter it out.
                    user_selected_lines.Erase(e);
                else
                    lines.Add(new LineSegment(e));

                errData.AppendLine("Line " + x.ToString() + ":" + l.GetEndPoint(0).ToString() + " , " + l.GetEndPoint(1).ToString());
                x++;
            }
            log.InfoFormat("{0}:Line Manager create {1} line segments", Constants.POLY_LINE_MACRO_NAME, lines.Count.ToString());
            System.Diagnostics.Debug.Print("Created {0} line segments.", lines.Count);

            if (lines.Count != 0)
            {
                find_start_line();
                if (start_line == null)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("Polyline Tool error", "The start of the polyline could not be detected.");
                    log.WarnFormat("{0}:Line Manger could not detect the start of the polyline", Constants.POLY_LINE_MACRO_NAME);
                }
                else
                {
                    sort_lines_from_start_to_end();
                    set_bounds(aview);
                    foreach (LineSegment ls in lines)
                    {
                        lengthofpolyline += ls.length;
                    }
                }
            }

            user_selected_lines.Dispose();
        }

        private void find_start_line()
        {
            LineSegment ls;
            System.Collections.IEnumerator lnEnum = lines.GetEnumerator();
            lnEnum.Reset();

            while (lnEnum.MoveNext() && start_line == null) //quick cycle through lines until you find one joined at one end only (the start line)
            {
                ls = lnEnum.Current as LineSegment;
                if (ls.joinedAtEnd ^ ls.joinedAtStart) // if line is only joined at one end it must be a start line
                {
                    start_line = ls; //starting line has been found
                }
            }

            lnEnum.Reset();

            while (lnEnum.MoveNext() && start_line == null) //deeper cycle through lines until you find one joined at one end only (the start line)
            {
                int lines_at_start = 0, lines_at_end = 0;
                LocationCurve locCurve;
                ElementArray elar;
                ls = lnEnum.Current as LineSegment;
                locCurve = ls.getLine();

                elar = locCurve.get_ElementsAtJoin(0);
                foreach (Element e in elar) // find out how many lines are joined at the start end
                    if (user_selected_lines.Contains(e))
                        ++lines_at_start;

                elar = locCurve.get_ElementsAtJoin(1);
                foreach (Element e in elar) // find out how many lines are joined at the end end
                    if (user_selected_lines.Contains(e))
                        ++lines_at_end;

                if (lines_at_start == 1 && lines_at_end == 2)
                {
                    start_line = ls; // if line is joined only on 1 end by only 1 valid line (a user selected line) it is a start line
                    start_line.joinedAtStart = false;
                    start_line.joinedAtEnd = true;
                }
                else if (lines_at_start == 2 && lines_at_end == 1)
                {
                    start_line = ls; // if line is joined only on 1 end by only 1 valid line (a user selected line) it is a start line
                    start_line.joinedAtStart = true;
                    start_line.joinedAtEnd = false;
                }
            }
        }

        private void sort_lines_from_start_to_end() //sort the lines array from start to end of path
        {
            LineSegment prevline, nextline, currentline;
            List<LineSegment> sorted_array = new List<LineSegment>();

            sorted_array.Add(start_line);

            //get lines joined to start line (current)
            currentline = start_line;
            nextline = get_next_line(start_line, null);
            user_selected_lines.Erase(start_line.element);
            prevline = start_line;

            while (nextline != null)    // add connected lines to array. Null is returned when no connected lines found. Path ends.
            {
                sorted_array.Add(nextline);
                user_selected_lines.Erase(nextline.element);
                end_line = nextline;
                prevline = currentline;
                currentline = nextline;
                nextline = get_next_line(currentline, prevline);
            }

            if (end_line != null)
                end_line.setEndJoinConditions(prevline, true);
            lines = sorted_array;
        }

        private LineSegment get_next_line(LineSegment cl, LineSegment pl) //find next line in path by comparing user selected lines to joined lines
        {
            ElementArray elarray = null;
            LineSegment thenextline = null;
            LineSegment checkline;

            if (pl == null) //this is the first time thru this fx then cl is the start line and there can only be 1 joined end
            {
                if (cl.joinedAtStart)
                {
                    elarray = cl.getLine().get_ElementsAtJoin(0); // get all lines joined to the start end

                    int x = 0;
                    checkline = new LineSegment(elarray.get_Item(x)); // convert the element to a line_segment

                    while (x < elarray.Size && thenextline == null) // cycle thru all lines joined to start end
                    {
                        if (not_valid_line(checkline, cl)) // is checkline equal to current line or not in user selected set
                        {
                            ++x;
                            if (x < elarray.Size)
                                checkline = new LineSegment(elarray.get_Item(x));
                        }
                        else
                        {
                            thenextline = checkline; // this must be the next line
                        }
                    }
                }

                if (cl.joinedAtEnd && thenextline == null) // same as above but for lines at the end end
                {
                    elarray = cl.getLine().get_ElementsAtJoin(1);

                    int x = 0;
                    checkline = new LineSegment(elarray.get_Item(x));

                    while (x < elarray.Size && thenextline == null)
                    {
                        if (not_valid_line(checkline, cl))
                        {
                            ++x;
                            if (x < elarray.Size)
                                checkline = new LineSegment(elarray.get_Item(x));
                        }
                        else
                        {
                            thenextline = checkline;
                        }
                    }
                }
            }
            else
            {
                //compare end points of line to see which end of cl (current line) matches an end of pl (previous line)
                //use the other end to get the next set of joined lines
                int nextend = 2;
                XYZ cl_0, cl_1, pl_0, pl_1;

                cl_0 = cl.getLine().Curve.GetEndPoint(0);
                cl_1 = cl.getLine().Curve.GetEndPoint(1);
                pl_0 = pl.getLine().Curve.GetEndPoint(0);
                pl_1 = pl.getLine().Curve.GetEndPoint(1);

                if ((cl_0.IsAlmostEqualTo(pl_0)) || (cl_0.IsAlmostEqualTo(pl_1)))
                    nextend = 1;
                else if ((cl_1.IsAlmostEqualTo(pl_0)) || (cl_1.IsAlmostEqualTo(pl_1)))
                    nextend = 0;
                elarray = cl.getLine().get_ElementsAtJoin(nextend);
                if (elarray.Size == 0) // no next line (end line)
                {
                    thenextline = null;
                    if (nextend == 0)
                    {
                        cl.joinedAtStart = false;
                        cl.joinedAtEnd = true;
                    }
                    else
                    {
                        cl.joinedAtStart = true;
                        cl.joinedAtEnd = false;
                    }
                }
                else
                {
                    int x = 0;
                    checkline = new LineSegment(elarray.get_Item(x));

                    while (x < elarray.Size && thenextline == null)
                    {
                        if (not_valid_line(checkline, cl))
                        {
                            ++x;
                            if (x < elarray.Size)
                                checkline = new LineSegment(elarray.get_Item(x));
                        }
                        else
                        {
                            thenextline = checkline;
                        }
                    }
                }
            }
            return thenextline;
        }

        private bool not_valid_line(LineSegment lhs, LineSegment rhs)
        {
            if (!lhs.isLine)
                return true;

            if (rhs != null && lhs.Equals(rhs)) // if the next line equals the current line
                return true;

            if (!user_selected_lines.Contains(lhs.element)) // check if thenextline is NOT part of the user selected lines
                return true;

            return false;
        }

        private void set_bounds(Autodesk.Revit.DB.View view) // determine max and min X and Y values for picturebox
        {
            LineSegment l1 = lines[0] as LineSegment;

            max_X = l1.Sx > l1.Ex ? l1.Sx : l1.Ex;
            max_Y = l1.Sy > l1.Ey ? l1.Sy : l1.Ey;
            min_X = l1.Sx < l1.Ex ? l1.Sx : l1.Ex;
            min_Y = l1.Sy < l1.Ey ? l1.Sy : l1.Ey;

            foreach (LineSegment l in lines)
            {
                max_X = l.Sx > max_X ? l.Sx : max_X;
                max_Y = l.Sy > max_Y ? l.Sy : max_Y;
                min_X = l.Sx < min_X ? l.Sx : min_X;
                min_Y = l.Sy < min_Y ? l.Sy : min_Y;

                max_X = l.Ex > max_X ? l.Ex : max_X;
                max_Y = l.Ey > max_Y ? l.Ey : max_Y;
                min_X = l.Ex < min_X ? l.Ex : min_X;
                min_Y = l.Ey < min_Y ? l.Ey : min_Y;
            }
        }

        internal System.Collections.IEnumerator GetLinesEnumerator()
        {
            return lines.GetEnumerator();
        }
    }
}