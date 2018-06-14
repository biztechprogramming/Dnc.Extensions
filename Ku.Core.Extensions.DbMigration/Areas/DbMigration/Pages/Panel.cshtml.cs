﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ku.Core.Extensions.DbMigration.AssemblyLocator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Ku.Core.Extensions.DbMigration.DbMigration.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class PanelModel : PageModel
    {
        private readonly IAssemblyLocator _locator;
        private static IList<Assembly> Assemblies;
        private readonly DbMigrationOptions _options;
        private readonly IDbTool _dbTool;

        public PanelModel(IAssemblyLocator locator, IOptions<DbMigrationOptions> option, IDbTool dbTool)
        {
            _locator = locator;
            _options = option.Value;
            _dbTool = dbTool;
        }

        public List<Poco> Pocos { set; get; }

        public async Task OnGetAsync()
        {
            var tables = await _dbTool.GetTablesAsync();
            Pocos = new List<Poco>();
            Assemblies = _locator.GetAssemblies();
            foreach (var x in Assemblies)
            {
                //查找带有TableAttribute的类
                var tps = x.DefinedTypes.Where(y => y.GetCustomAttribute(typeof(TableAttribute), true) != null);
                foreach (var item in tps)
                {
                    Poco model = new Poco();
                    model.Name = item.Name;
                    model.FullName = item.FullName;
                    model.Assembly = item.Assembly.FullName;
                    model.GUID = item.GUID;
                    var attr = item.GetCustomAttribute<TableAttribute>();
                    model.TableName = item.GetCustomAttribute<TableAttribute>()?.Name;
                    model.Comment = item.GetCustomAttribute<DisplayAttribute>()?.Name;

                    var table = tables.SingleOrDefault(t => t.TableName.Equals(model.TableName, StringComparison.OrdinalIgnoreCase));
                    if (table != null)
                    {
                        model.DbExist = true;
                        model.DbComment = table.Comment;
                        model.Tag = "new";
                    }
                    Pocos.Add(model);
                }
            }
        }

        /// <summary>
        /// 取得列表数据
        /// </summary>
        public async Task<IActionResult> OnGetDataAsync(string assembly, string poco)
        {
            var asm = Assemblies.Single(x => x.FullName.Equals(assembly));
            var type = asm.DefinedTypes.Single(x => x.FullName.Equals(poco));
            var properties = type.GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public)
                .Where(x => !x.GetAccessors()[0].IsVirtual && x.GetCustomAttribute<NotMappedAttribute>() == null);

            var items = new List<PocoField>();

            foreach (var property in properties)
            {
                var field = new PocoField();
                field.Name = property.Name;
                var attr = property.GetCustomAttribute<DisplayAttribute>();
                if (attr != null)
                {
                    field.Comment = attr.Name;
                }

                //字段类型
                field.DataType = GetFieldType(property);

                //是否可空
                field.Nullable = IsNullableField(property);

                //是否主键
                field.IsKey = property.GetCustomAttribute<KeyAttribute>() != null;

                items.Add(field);
            }

            var migrations = new List<string>();
            //取得DB端信息
            var tableName = type.GetCustomAttribute<TableAttribute>()?.Name;
            var table = _dbTool.GetTables().SingleOrDefault(x => x.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
            if (table == null)
            {
                //数据库端未创建表
                migrations.Add("create table");
                foreach (var item in items)
                {
                    item.DbComment = item.Comment;
                    item.DbDataType = item.DataType;
                    item.DbIsKey = item.IsKey;
                    item.DbNullable = item.Nullable;
                }
            }
            else
            {
                var fields = await _dbTool.GetTableFieldsAsync(tableName);
                foreach (var item in items)
                {
                    List<string> diff = new List<string>();
                    //开始判断是否有差异
                    var field = fields.SingleOrDefault(x => x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
                    if (field == null)
                    {
                        //数据库没有该字段
                        migrations.Add($"add column {item.Name}");
                    }
                    else
                    {
                        //数据库有该字段
                        item.DbDataType = field.DataType;
                        item.DbNullable = field.Nullable;
                        item.DbComment = field.Comment;
                        item.DbIsKey = field.IsKey;

                        if (item.DbDataType != item.DataType 
                            || item.DbNullable != item.Nullable
                            || (item.DbComment??"") != (item.Comment??""))
                        {
                            migrations.Add($"alter column {item.Name}");
                        }
                    }
                }

                //处理仅数据库端才有的字段
                foreach (var item in fields.Where(x => !items.Any(i => i.Name.Equals(x.Name))))
                {
                    var field = new PocoField();
                    field.Name = item.Name;
                    field.DataType = field.DataType;
                    field.Nullable = field.Nullable;
                    field.Comment = field.Comment;
                    field.IsKey = field.IsKey;
                    field.DbDataType = field.DataType;
                    field.DbNullable = field.Nullable;
                    field.DbComment = field.Comment;
                    field.DbIsKey = field.IsKey;

                    items.Add(field);

                    migrations.Add($"drop column {item.Name}");
                }
            }

            return new DbMigrationJsonResult(new {
                items = new LayuiPagerResult<PocoField>(items.OrderByDescending(x => x.IsKey), 1, 999, items.Count),
                migrations = migrations
            });
        }

        private bool IsNullableField(PropertyInfo property)
        {
            if (property.PropertyType == typeof(string))
            {
                if (property.GetCustomAttribute<RequiredAttribute>() != null)
                {
                    return false;
                }
                return true;
            }
            else
            {
                return IsNullableType(property.PropertyType);
            }
        }

        /// <summary>
        /// 获取字段类型
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private string GetFieldType(PropertyInfo property)
        {
            var type = GetRealType(property.PropertyType);
            if (type == typeof(string))
            {
                //取得长度
                var maxLength = 0;
                var stringLengthAttribute = property.GetCustomAttribute<StringLengthAttribute>();
                if (stringLengthAttribute != null)
                {
                    maxLength = stringLengthAttribute.MaximumLength;
                }
                var maxLengthAttribute = property.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLengthAttribute != null)
                {
                    var vmax = maxLengthAttribute.Length;
                    if (maxLength < vmax) maxLength = vmax;
                }
                if (maxLength == 0)
                {
                    return "longtext";
                }
                else
                {
                    return $"varchar({maxLength})";
                }
            }
            else if (type == typeof(bool))
            {
                return "bit";
            }
            else if (type == typeof(int) || type == typeof(Int32))
            {
                return "int";
            }
            else if (type == typeof(short) || type == typeof(Int16))
            {
                return "smallint";
            }
            else if (type == typeof(long) || type == typeof(Int64))
            {
                return "bigint";
            }
            else if (type == typeof(decimal))
            {
                return "decimal(65, 30)";
            }
            else if (type == typeof(float))
            {
                return "float";
            }
            else if (type == typeof(double))
            {
                return "double";
            }
            else if (type == typeof(DateTime))
            {
                return "datetime(6)";
            }
            else if (type.IsEnum)
            {
                return "smallint";
            }
            else
            {
                return "";
            }
        }

        private bool IsNullableType(Type theType)
        {
            return (theType.IsGenericType && theType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)));
        }

        private Type GetRealType(Type theType)
        {
            if (IsNullableType(theType))
            {
                return theType.GetGenericArguments()[0];
            }
            return theType;
        }

    }
}