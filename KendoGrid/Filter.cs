using System;
using System.Collections.Generic;
using System.Linq;

namespace KendoUtilities.KendoGrid
{

    ///<summary>
    /// Represents a filter expression from a Kendo Grid Datasource
    ///</summary>
    public class Filter
    {
        ///<summary>
        /// Gets or sets the name of the filtering field. Set to null if Filters property is set.
        ///</summary>
        public string Field { get; set; }
        ///<summary>
        /// Gets or sets the filtering operator. Set to null if Filters property is set.
        ///</summary>
        public string Operator { get; set; }
        ///<summary>
        /// Gets or sets the filtering value. Set to null if Filters property is set.
        ///</summary>
        public object Value { get; set; }
        ///<summary>
        /// Gets or sets the filtering logic. Can be set to "or" or "and". Set to null if Filters property is set.
        ///</summary>
        public string Logic { get; set; }
        ///<summary>
        /// Gets or sets the list of child filter expressions. Set to null if if there are no child filtering expressions.
        ///</summary>
        public IEnumerable<Filter> Filters { get; set; }

        ///<summary>
        /// Mapping of Kendo datasource filtering options to Dynamic Linq
        ///</summary>
        private static readonly IDictionary<string, string> operatorsLinq = new Dictionary<string, string>{
            {"eq"," =="},
            {"neq","!="},
            {"lt","<"},
            {"lte","<="},
            {"gt",">"},
            {"gte",">="},
            {"startswith","StartsWith"},
            {"endswith","EndsWith"},
            {"contains","Contains"},
            {"doesnotcontain","Contains"}
        };

        ///<summary>
        /// Mapping of Kendo datasource filtering options to SQL
        /// NOTE: Depending on your SQL server of choice these 
        /// mappings may need to change
        ///</summary>
        private static readonly IDictionary<string, string> operatorsSql = new Dictionary<string, string>{
            {"eq"," ="},
            {"neq","<>"},
            {"lt","<"},
            {"lte","<="},
            {"gt",">"},
            {"gte",">="},
            {"isnull","IS NULL"},
            {"isnotnull","IS NOT NULL"},
            /*
            * STRING ONLY OPERATORS BELOW
            *
            * Using ILIKE instead of LIKE to ignore casing
            * NOTE: There is a large performance hit incurred
            * by ignoring case as indexes cannot be used
            */
            {"startswith","ILIKE"},
            {"endswith","ILIKE"},
            {"contains","ILIKE"},
            {"doesnotcontain","NOT ILIKE"},
            {"isempty", "= \'\'"},
            {"isnotempty","<> \'\'"}
        };

        ///<summary>
        /// Returns a flattened list of all child filter expressions
        ///</summary>
        public IList<Filter> All()
        {
            var filters = new List<Filter>();
            Collect(filters);
            return filters;
        }

        private void Collect(IList<Filter> filters)
        {
            if (Filters != null && Filters.Any())
            {
                foreach (Filter filter in Filters)
                {
                    filters.Add(filter);
                    filter.Collect(filters);
                }
            }
            else
            {
                filters.Add(this);
            }
        }

        ///<summary>
        /// Converts the filter expression to a predicate suitable for Dynamic Linq
        /// e.g. "Field1 == @1 and Field2.Contains(@2)"
        ///</summary>
        ///<param name="filters"> A list of flattened filter expressions</param>
        public string ToDynamicLinq(IList<Filter> filters)
        {
            if (Filters != null && Filters.Any())
            {
                return $"({String.Join($" {Logic} ", Filters.Select(filter => filter.ToDynamicLinq(filters)).ToArray())})";
            }

            if (Operator == null)
                return "";

            int index = filters.IndexOf(this);
            string comparison = operatorsLinq[Operator];

            if (String.Equals(Operator, "doesnotcontain", StringComparison.InvariantCultureIgnoreCase))
            {
                return $"!{Field}.{comparison}(@{index})";
            }

            if (String.Equals(Operator, "startswith", StringComparison.InvariantCultureIgnoreCase)
                || String.Equals(Operator, "endswith", StringComparison.InvariantCultureIgnoreCase)
                || String.Equals(Operator, "contains", StringComparison.InvariantCultureIgnoreCase))
            {
                return $"{Field}.{comparison}(@{index})";
            }

            return $"{Field} {comparison} @{index}";
        }

        ///<summary>
        /// Converts the filter expression to a predicate suitable for an
        /// SQL query
        /// e.g. "Field1 <> 1 and Field2 ILIKE 'VALUE'"
        ///</summary>
        ///<param name="filters"> A list of flattened filter expressions</param>
        public string ToSql(IList<Filter> filters)
        {
            if (Filters != null && Filters.Any())
            {
                return $"({String.Join($" {Logic} ", Filters.Select(filter => filter.ToDynamicLinq(filters)).ToArray())})";
            }

            if (Operator == null)
                return "";

            int index = filters.IndexOf(this);
            string comparison = operatorsLinq[Operator];

            if (String.Equals(Operator, "doesnotcontain", StringComparison.InvariantCultureIgnoreCase)
                || String.Equals(Operator, "contains", StringComparison.InvariantCultureIgnoreCase))
            {
                return $"{Field} {comparison} \'%{Value}%\'";
            }

            if (String.Equals(Operator, "startswith", StringComparison.InvariantCultureIgnoreCase))
            {
                return $"{Field} {comparison} \'{Value}%\'";
            }

            if (String.Equals(Operator, "endswith", StringComparison.InvariantCultureIgnoreCase))
            {
                return $"{Field} {comparison} \'%{Value}\'";
            }
            if (String.Equals(Operator, "isnull", StringComparison.InvariantCultureIgnoreCase)
                || String.Equals(Operator, "isnotnull", StringComparison.InvariantCultureIgnoreCase)
                || String.Equals(Operator, "isempty", StringComparison.InvariantCultureIgnoreCase)
                || String.Equals(Operator, "isnotempty", StringComparison.InvariantCultureIgnoreCase))
            {
                return $"{Field} {comparison}";
            }

            return $"{Field} {comparison} \'{Value}\'";
        }
    }
}