﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Ku.Core.Extensions.Layui.Test.Models
{
    public class DemoModel
    {
        /// <summary>
        /// ID
        /// </summary>
        [DataType("hidden")]
        public long Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        [Required, MaxLength(20), MinLength(5)]
        [Display(Name = "名称", Description = "附加说明文字")]
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$", ErrorMessage = "输入的名称不符合规则")]
        public string Name { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [Display(Name = "手机号")]
        [DataType(DataType.PhoneNumber)] //也可以为[DataType("mobile")]
        public string Mobile { get; set; }

        /// <summary>
        /// 链接
        /// </summary>
        [MaxLength(256)]
        [Display(Name = "链接")]
        [DataType(DataType.Url)]
        public string Url { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        [MaxLength(256)]
        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        [Display(Name = "序号", Prompt ="0~9999")]
        public int OrderIndex { get; set; } = 0;

        /// <summary>
        /// 序号
        /// </summary>
        [Display(Name = "数字", Prompt = "0~9999")]
        [Range(-1, 999.05)]
        public decimal Dec { get; set; } = 0;

        /// <summary>
        /// 开关
        /// </summary>
        [Display(Name = "开关", Prompt = "开|关")]
        public bool Switch { get; set; } = true;

        [Display(Name = "时间")]
        [DisplayFormat(DataFormatString = "yyyy-MM-dd HH:mm:ss")] //定义显示格式
        [DataType(DataType.DateTime)] //也可以这样[DataType("datetime")]
        public DateTime? Date { set; get; }

        [Display(Name = "年份")]
        [DataType("year")]
        public int Year { set; get; }

        [Display(Name = "月份")]
        [DisplayFormat(DataFormatString = "现在是yyyy年MM月")]
        [DataType("month")]
        public string Month { set; get; }

        /// <summary>
        /// 枚举
        /// </summary>
        [Display(Name = "性别")]
        public EmSex Sex { set; get; }

        /// <summary>
        /// 多行文本
        /// </summary>
        [Display(Name = "备注", Prompt = "请输入您的备注信息...")]
        [MaxLength(500)]
        [DataType(DataType.MultilineText)] //或者[DataType("textarea")]
        public string Remark { set; get; }
    }

    /// <summary>
    /// 性别
    /// </summary>
    public enum EmSex : short
    {
        [Display(Name = "保密")]
        Secret = 0,

        [Display(Name = "男")]
        Male = 1,

        [Display(Name = "女")]
        Female = 2
    }
}
