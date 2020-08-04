using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RVVD.Weblink
{
    public class WebAddress
    {
        private string _address;
        private string _parameterName;
        private ElementId _parmID;

        public WebAddress(Parameter p)
        {
            _address = p.AsString();
            _parameterName = p.Definition.Name;
            _parmID = p.Id;
        }

        public string Address { get { return _address; } }
        public string Name { get { return _parameterName; } }
        public ElementId ID { get { return _parmID; } }
    }
}
