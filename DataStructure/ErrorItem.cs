using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace ElectronicCorrectionNotebook.DataStructure
{
    public class ErrorItem : INotifyPropertyChanged
    {
        // Guid
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

        // Title
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

        // Date
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

        // Files Path
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

        // Rating star
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

        // Tags
        private ObservableCollection<TagItem> tagItems = new ObservableCollection<TagItem>(); // 私有字段
        public ObservableCollection<TagItem> TagItems
        {
            get => tagItems;
            set
            {
                if (tagItems != value)
                {
                    tagItems = value;
                    OnPropertyChanged();
                }
            }
        }
         
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
