﻿using Dnc.Extensions.Dapper.Attributes;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Dnc.Extensions.Dapper.Builders
{
    public class ConditionBuilder : BaseBuilder
    {
        protected StringBuilder sb = new StringBuilder();

        public (string sql, Dictionary<string, object> param) Build()
        {
            return (sb.ToString(), param);
        }

        public ConditionBuilder Expression<TEntity>(Expression<Func<TEntity, object>> field, string @operator, object value)
        {
            var t1 = FormatTableAliasKey<TEntity>();
            var n1 = field.GetPropertyName();
            return Expression(FormatFiled(t1, n1), @operator, value);
        }

        public ConditionBuilder Expression(string field, string @operator, object value)
        {
            var p = GetParameterName();
            param.Add(p, value);

            sb.Append($"{field}{@operator}@{p}");

            return this;
        }

        public ConditionBuilder Append(string sql, params KeyValuePair<string, object>[] @params)
        {
            if (@params != null)
            {
                foreach (var item in @params)
                {
                    param.Add(item.Key, item.Value);
                }
            }
            sb.Append(sql);
            return this;
        }

        public ConditionBuilder Expression<T1, T2>(Expression<Func<T1, object>> field1, string @operator, Expression<Func<T1, object>> field2)
        {
            var t1 = FormatTableAliasKey<T1>();
            var n1 = field1.GetPropertyName();

            var t2 = FormatTableAliasKey<T2>();
            var n2 = field2.GetPropertyName();

            sb.Append($"{FormatFiled(t1, n1)}{@operator}{FormatFiled(t2, n2)}");

            return this;
        }

        #region Like & NotLike

        public ConditionBuilder Like(string field, object value, string leftChars = "%", string rightChars = "%")
        {
            var p = GetParameterName();
            param.Add(p, $"{leftChars}{value}{rightChars}");

            sb.Append($"{field} like @{p}");

            return this;
        }

        public ConditionBuilder Like<TEntity>(Expression<Func<TEntity, object>> field, object value, string leftChars = "%", string rightChars = "%")
        {
            var t1 = FormatTableAliasKey<TEntity>();
            var n1 = field.GetPropertyName();
            return Like(FormatFiled(t1, n1), value, leftChars, rightChars);
        }

        public ConditionBuilder NotLike(string field, object value, string leftChars = "%", string rightChars = "%")
        {
            var p = GetParameterName();
            param.Add(p, $"{leftChars}{value}{rightChars}");

            sb.Append($"{field} not like @{p}");

            return this;
        }

        public ConditionBuilder NotLike<TEntity>(Expression<Func<TEntity, object>> field, object value, string leftChars = "%", string rightChars = "%")
        {
            var t1 = FormatTableAliasKey<TEntity>();
            var n1 = field.GetPropertyName();
            return NotLike(FormatFiled(t1, n1), value, leftChars, rightChars);
        }

        #endregion

        #region In & NotIn

        public ConditionBuilder In(string field, object value)
        {
            var p = GetParameterName();
            param.Add(p, value);

            sb.Append($"{field} in (@{p})");

            return this;
        }

        public ConditionBuilder In<TEntity>(Expression<Func<TEntity, object>> field, object value)
        {
            var t1 = FormatTableAliasKey<TEntity>();
            var n1 = field.GetPropertyName();
            return In(FormatFiled(t1, n1), value);
        }

        public ConditionBuilder NotIn(string field, object value)
        {
            var p = GetParameterName();
            param.Add(p, value);

            sb.Append($"{field} not like (@{p})");

            return this;
        }

        public ConditionBuilder NotIn<TEntity>(Expression<Func<TEntity, object>> field, object value)
        {
            var t1 = FormatTableAliasKey<TEntity>();
            var n1 = field.GetPropertyName();
            return NotIn(FormatFiled(t1, n1), value);
        }

        #endregion

        #region 逻辑符

        public ConditionBuilder And()
        {
            sb.Append($" and ");
            return this;
        }


        public ConditionBuilder Or()
        {
            sb.Append($" or ");
            return this;
        }

        public ConditionBuilder ParenOpen()
        {
            sb.Append($" (");
            return this;
        }

        public ConditionBuilder ParenClose()
        {
            sb.Append($") ");
            return this;
        }

        #endregion

        public static ConditionBuilder FromSearch<TEntity>(object search)
        {
            var builder = new ConditionBuilder();
            if (search == null)
            {
                return builder;
            }
            bool isFirst = true;
            foreach (PropertyInfo p in search.GetType().GetProperties())
            {
                //取得ConditionAttribute特性
                var attr = p.GetCustomAttribute<ConditionAttribute>();
                var ignoreWhenNull = attr != null ? attr.WhenNull == WhenNull.Ignore : true;
                var value = p.GetValue(search);

                //值为NULL的处理
                if (value == null && ignoreWhenNull)
                {
                    continue;
                }
                if (p.PropertyType == typeof(string) && string.IsNullOrEmpty((string)value) && ignoreWhenNull)
                {
                    continue;
                }

                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    builder.And();
                }

                //取得字段名
                var name = (attr != null && !string.IsNullOrEmpty(attr.Name)) ? attr.Name : p.Name;

                var t = FormatTableAliasKey<TEntity>();

                var field = builder.FormatFiled(t, name);

                ConditionOperation operation = attr != null ? attr.Operation : ConditionOperation.Equal;
                switch (operation)
                {
                    case ConditionOperation.Equal:
                        builder.Expression(field, "=", value);
                        break;
                    case ConditionOperation.NotEqual:
                        builder.Expression(field, "<>", value);
                        break;
                    case ConditionOperation.In:
                        builder.In(field, value);
                        break;
                    case ConditionOperation.NotIn:
                        builder.NotIn(field, value);
                        break;
                    case ConditionOperation.Like:
                        builder.Like(field, value, attr?.LeftChar, attr?.RightChar);
                        break;
                    case ConditionOperation.NotLike:
                        builder.NotLike(field, value, attr?.LeftChar, attr?.RightChar);
                        break;
                    case ConditionOperation.Greater:
                        builder.Expression(field, ">", value);
                        break;
                    case ConditionOperation.GreaterOrEqual:
                        builder.Expression(field, ">=", value);
                        break;
                    case ConditionOperation.Less:
                        builder.Expression(field, "<", value);
                        break;
                    case ConditionOperation.LessOrEqual:
                        builder.Expression(field, "<=", value);
                        break;
                    case ConditionOperation.Custom:
                        //自定义
                        var s = attr?.CustomSql;
                        if (!string.IsNullOrEmpty(s))
                        {
                            string pn = builder.GetParameterName();
                            s = s.Replace("@value", "@" + pn);
                            builder.Append(s, new KeyValuePair<string, object>(pn, value));
                        }
                        break;
                    default:
                        builder.Expression(field, "=", value);
                        break;
                }
            }

            return builder;
        }

        public static ConditionBuilder FormDynamic<TEntity>(dynamic where)
        {
            var builder = new ConditionBuilder();
            if (where as object == null)
            {
                return builder;
            }
            var t = FormatTableAliasKey<TEntity>();
            bool isFirst = true;
            foreach (var item in Dapper.GetDynamicFields(where as object))
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    builder.And();
                }

                builder.Expression(builder.FormatFiled(t, item.Name), "=", item.Value);
            }

            return builder;
        }
    }
}
