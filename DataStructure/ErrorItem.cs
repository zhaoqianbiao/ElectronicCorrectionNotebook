using System;
using System.Collections.Generic;

namespace ElectronicCorrectionNotebook.DataStructure
{
    public class ErrorItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();                          // guid标识符
        public string Title { get; set; }                                       // 标题
        public DateTime Date { get; set; }                                      // 日期
        public List<string> CorrectionTag { get; set; } = new List<string>();   // 错题标签
        public List<string> FilePaths { get; set; } = new List<string>();       // 文件路径
        public double Rating { get; set; }                                      // 重要度
    }
}
