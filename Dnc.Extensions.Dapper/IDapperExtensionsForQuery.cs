﻿using Dapper;
using Dnc.Extensions.Dapper.Builders;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dnc.Extensions.Dapper
{
    public static class IDapperExtensionsForQuery
    {
        #region QueryOne & QueryOneAsync

        public static TEntity QueryOne<TEntity>(this IDapper dapper, QueryBuilder builder) where TEntity : class
        {
            var result = builder.Build();

            dapper.Log("QueryOne", result.sql);
            return dapper.Connection.QueryFirstOrDefault<TEntity>(result.sql, result.param, dapper.DbTransaction, dapper.Timeout);
        }

        public static async Task<TEntity> QueryOneAsync<TEntity>(this IDapper dapper, QueryBuilder builder) where TEntity : class
        {
            var result = builder.Build();

            dapper.Log("QueryOneAsync", result.sql);
            return await dapper.Connection.QueryFirstOrDefaultAsync<TEntity>(result.sql, result.param, dapper.DbTransaction, dapper.Timeout);
        }

        public static TEntity QueryOne<TEntity>(this IDapper dapper, dynamic where, dynamic order = null) where TEntity : class
        {
            var builder = new SimpleQueryBuilder<TEntity>(where as object, order as object);

            return dapper.QueryOne<TEntity>(builder);
        }

        public static async Task<TEntity> QueryOneAsync<TEntity>(this IDapper dapper, dynamic where, dynamic order = null) where TEntity : class
        {
            var builder = new SimpleQueryBuilder<TEntity>(where as object, order as object);
            return await dapper.QueryOneAsync<TEntity>(builder);
        }

        #endregion

        #region QueryList & QueryListAsync

        public static IEnumerable<TEntity> QueryList<TEntity>(this IDapper dapper, QueryBuilder builder) where TEntity : class
        {
            var result = builder.Build();

            dapper.Log("QueryList", result.sql);
            return dapper.Connection.Query<TEntity>(result.sql, result.param, dapper.DbTransaction, true, dapper.Timeout);
        }

        public static async Task<IEnumerable<TEntity>> QueryListAsync<TEntity>(this IDapper dapper, QueryBuilder builder) where TEntity : class
        {
            var result = builder.Build();

            dapper.Log("QueryListAsync", result.sql);
            return await dapper.Connection.QueryAsync<TEntity>(result.sql, result.param, dapper.DbTransaction, dapper.Timeout);
        }



        #endregion

        private static (string sql, DynamicParameters parameters) _Query(IDapper dapper, string field, object tableJoin, object where, object order, bool isOne)
        {
            DynamicParameters parameters;

            //处理WHERE语句
            string whereSql;
            if (where is DapperSql dSql)
            {
                whereSql = dapper.Dialect.FormatWhereSql(null, dSql.Sql);
                parameters = new DynamicParameters(dSql.Parameters as object);
            }
            else if (where is string s)
            {
                whereSql = dapper.Dialect.FormatWhereSql(null, s);
                parameters = new DynamicParameters();
            }
            else
            {
                parameters = new DynamicParameters(where);
                var fields = Dapper.GetDynamicFields(where).Select(x => x.Name).ToList();
                whereSql = dapper.Dialect.FormatWhereSql(fields, null);
            }

            //处理ORDER语句
            string orderSql = null;
            if (order is DapperSql dSqlOrder)
            {
                orderSql = dapper.Dialect.FormatOrderSql(null, dSqlOrder.Sql);
            }
            else if (order is string s)
            {
                orderSql = dapper.Dialect.FormatOrderSql(null, s);
            }
            else if (order != null)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                Dapper.GetDynamicFields(order).ForEach(item => dict.Add(item.Name, (string)item.Value));
                orderSql = dapper.Dialect.FormatOrderSql(dict, null);
            }

            //处理tableJoin
            string tableJoinSql = null;
            if (tableJoin is string str1)
            {
                tableJoinSql = str1;
            }
            else if (tableJoin is DapperSql ds)
            {
                tableJoinSql = ds.Sql;
                var dynamicFields = Dapper.GetDynamicFields(ds.Parameters as object);
                var expandoObject = new ExpandoObject() as IDictionary<string, object>;
                dynamicFields.ForEach(p => expandoObject.Add(p.Name, p.Value));
                parameters.AddDynamicParams(expandoObject);
            }

            var sql = dapper.Dialect.FormatQuerySql(field, tableJoinSql, whereSql, orderSql, isOne);
            return (sql, parameters);
        }


        #region QueryList

        public static IEnumerable<TEntity> QueryList<TEntity>(this IDapper dapper, dynamic where, dynamic order = null) where TEntity : class
        {
            (string sql, DynamicParameters parameters) = _Query(dapper, "*", dapper.Dialect.FormatTableName<TEntity>(), where as object, order as object, false);
            if (string.IsNullOrEmpty(sql))
            {
                throw new DapperException("SQL异常！");
            }
            dapper.Log("QueryList", sql);
            return dapper.Connection.Query<TEntity>(sql, parameters, dapper.DbTransaction, true, dapper.Timeout);
        }

        public static IEnumerable<TEntity> QueryList<TEntity>(this IDapper dapper, string field, dynamic tableJoin, dynamic where, dynamic order = null)
        {
            (string sql, DynamicParameters parameters) = _Query(dapper, field, tableJoin as object, where as object, order as object, false);
            if (string.IsNullOrEmpty(sql))
            {
                throw new DapperException("SQL异常！");
            }
            dapper.Log("QueryList", sql);
            return dapper.Connection.Query<TEntity>(sql, parameters, dapper.DbTransaction, true, dapper.Timeout);
        }

        public static IEnumerable<TReturn> QueryList<TFirst, TSecond, TReturn>(this IDapper dapper, string field, dynamic tableJoin, dynamic where, dynamic order, Func<TFirst, TSecond, TReturn> map, string splitOn = "Id")
        {
            (string sql, DynamicParameters parameters) = _Query(dapper, field, tableJoin as object, where as object, order as object, false);
            if (string.IsNullOrEmpty(sql))
            {
                throw new DapperException("SQL异常！");
            }
            dapper.Log("QueryList", sql);
            return dapper.Connection.Query(sql, map, parameters, dapper.DbTransaction, true, splitOn);
        }

        #endregion

        #region QueryListAsync

        public static async Task<IEnumerable<TEntity>> QueryListAsync<TEntity>(this IDapper dapper, dynamic where, dynamic order = null) where TEntity : class
        {
            (string sql, DynamicParameters parameters) = _Query(dapper, "*", dapper.Dialect.FormatTableName<TEntity>(), where as object, order as object, false);
            if (string.IsNullOrEmpty(sql))
            {
                throw new DapperException("SQL异常！");
            }
            dapper.Log("QueryListAsync", sql);
            return await dapper.Connection.QueryAsync<TEntity>(sql, parameters, dapper.DbTransaction, dapper.Timeout);
        }

        public static async Task<IEnumerable<TEntity>> QueryListAsync<TEntity>(this IDapper dapper, string field, dynamic tableJoin, dynamic where, dynamic order = null)
        {
            (string sql, DynamicParameters parameters) = _Query(dapper, field, tableJoin as object, where as object, order as object, false);
            if (string.IsNullOrEmpty(sql))
            {
                throw new DapperException("SQL异常！");
            }
            dapper.Log("QueryListAsync", sql);
            return await dapper.Connection.QueryAsync<TEntity>(sql, parameters, dapper.DbTransaction, dapper.Timeout);
        }

        public static async Task<IEnumerable<TReturn>> QueryListAsync<TFirst, TSecond, TReturn>(this IDapper dapper, string field, dynamic tableJoin, dynamic where, dynamic order, Func<TFirst, TSecond, TReturn> map, string splitOn = "Id")
        {
            (string sql, DynamicParameters parameters) = _Query(dapper, field, tableJoin as object, where as object, order as object, false);
            if (string.IsNullOrEmpty(sql))
            {
                throw new DapperException("SQL异常！");
            }
            dapper.Log("QueryListAsync", sql);
            return await dapper.Connection.QueryAsync(sql, map, parameters, dapper.DbTransaction, true, splitOn);
        }

        #endregion



        public static async Task<(int count, IEnumerable<TEntity> items)> QueryPageAsync<TEntity>(this IDapper dapper, QueryBuilder builder)
        {
            var result = builder.Build();

            dapper.Log("ExecuteScalarAsync", result.countSql);
            var count = (await dapper.Connection.ExecuteScalarAsync<int?>(result.countSql, result.param, dapper.DbTransaction, dapper.Timeout)).GetValueOrDefault();
            var limit = builder.GetLimit();
            if (count == 0 || count <= ((limit.page - 1) * limit.rows)) return (count, new TEntity[] { });

            dapper.Log("QueryPageAsync", result.pageSql);
            var items = await dapper.Connection.QueryAsync<TEntity>(result.pageSql, result.param, dapper.DbTransaction, dapper.Timeout);
            return (count, items);
        }

    }
}