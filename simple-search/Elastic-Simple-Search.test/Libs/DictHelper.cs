using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elastic_Simple_Search.Test.Libs
{
    public static class DictHelper
    {
        public static string ToFieldList(this Dictionary<string, object> fields)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var f in fields.Keys)
            {
                sb.Append(f);
                sb.Append(',');
            }
            return sb.ToString();
        }
    }
}
