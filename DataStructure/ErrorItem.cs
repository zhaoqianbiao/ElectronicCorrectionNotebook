using System;
using System.Collections.Generic;

namespace ElectronicCorrectionNotebook.DataStructure
{
    public class ErrorItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }  // 用于存储 RTF 格式的字符串
        public List<string> FilePaths { get; set; } = new List<string>();
        public double Rating { get; set; }
    }
}
