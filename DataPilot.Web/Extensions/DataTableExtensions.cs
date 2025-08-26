using System.Data;
using System.Dynamic;

namespace DataPilot.Web.Extensions
{
    public static class DataTableExtensions
    {
        public static List<dynamic> ToDynamicList(this DataTable table)
        {
            var dynamicList = new List<dynamic>();

            foreach (DataRow row in table.Rows)
            {
                IDictionary<string, object> expando = new ExpandoObject();
                foreach (DataColumn col in table.Columns)
                {
                    var value = row[col];
                    expando[col.ColumnName] = value == DBNull.Value ? null : value;
                }
                dynamicList.Add(expando);
            }

            return dynamicList;
        }
    }
}
