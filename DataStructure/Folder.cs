using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicCorrectionNotebook.DataStructure
{
    public class Folder
    {
        public string FolderName { get; set; }

        public List<ErrorItem> ErrorItems { get; set; } = new List<ErrorItem>();

    }
}
