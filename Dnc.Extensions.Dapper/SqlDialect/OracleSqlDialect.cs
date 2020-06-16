using System;
using System.Collections.Generic;
using System.Text;

namespace Dnc.Extensions.Dapper.SqlDialect
{
	public class OracleSqlDialect : BaseDialect
    {

        public override char ParameterPrefix
        {
            get
            {
                return ':';
            }
        }
        public override string FormatQuerySql(string field, string tableJoin, string where, string order, bool isOne)
        {
            var sql = new StringBuilder("SELECT ");
            if (string.IsNullOrEmpty(field))
            {
                field = "*";
            }
            sql.Append(field);
            sql.Append(" FROM ");
            sql.Append(tableJoin);

            if (!string.IsNullOrEmpty(where))
            {
                sql.Append(where);
            }

            if (!string.IsNullOrEmpty(order))
            {
                sql.Append(order);
            }

            if (isOne)
            {
                sql.Append(" FETCH NEXT 1 ROWS ONLY");
            }
            return sql.ToString();
        }
        public override string FormatQueryPageSql<TEntity>(int page, int rows, string field, string where, string order)
        {
            var sql = new StringBuilder("SELECT ");
            if (string.IsNullOrEmpty(field))
            {
                field = "*";
            }
            sql.Append(field);
            sql.Append(" FROM " + FormatTableName<TEntity>());

            if (!string.IsNullOrEmpty(where))
            {
                sql.Append(where);
            }

            if (!string.IsNullOrEmpty(order))
            {
                sql.Append(order);
            }

            sql.Append($" OFFSET {(page - 1) * rows} ROWS FETCH NEXT {rows} ROWS ONLY");

            return sql.ToString();
        }

        public override string FormatQueryPageSql(int page, int rows, string sql)
        {
            return sql + $" OFFSET {(page - 1) * rows} ROWS FETCH NEXT {rows} ROWS ONLY";
        }
    }
}
