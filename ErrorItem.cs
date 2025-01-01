using System;
using System.Collections.Generic;

namespace ElectronicCorrectionNotebook
{
    public class ErrorItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public List<string> FilePaths { get; set; } = new List<string>();
        public double Rating { get; set; }
    }
}
