using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ElectronicCorrectionNotebook.DataStructure
{
    public class ErrorItem : INotifyPropertyChanged
    {
        private Guid id = Guid.NewGuid(); // 私有字段，存储实际的值
        public Guid Id
        {
            get => id;
            set
            {
                if (id != value)
                {
                    id = value;
                    OnPropertyChanged();
                }
            }
        }

        private string title; // 私有字段
        public string Title   // 公共属性
        {
            get => title;
            set
            {
                if (title != value)
                {
                    title = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTimeOffset? date; // 私有字段
        public DateTimeOffset? Date   // 公共属性
        {
            get => date;
            set
            {
                if (date != value)
                {
                    date = value;
                    OnPropertyChanged();
                }
            }
        }

        private List<string> filePaths = new List<string>(); // 私有字段
        public List<string> FilePaths                       // 公共属性
        {
            get => filePaths;
            set
            {
                if (filePaths != value)
                {
                    filePaths = value;
                    OnPropertyChanged();
                }
            }
        }

        private double rating; // 私有字段
        public double Rating   // 公共属性
        {
            get => rating;
            set
            {
                if (rating != value)
                {
                    rating = value;
                    OnPropertyChanged();
                }
            }
        }

        // 实现 INotifyPropertyChanged 接口
        public event PropertyChangedEventHandler PropertyChanged;

        // 通知属性更改的方法
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
