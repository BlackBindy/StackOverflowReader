using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackOverflowReader
{
    public class SearchOption : INotifyPropertyChanged
    {
        static String[] dateStringArr = { "Today", "This Week", "This Month", "This Year", "3 Years", "All" };

        public String Title { get; set; }
        public String Tag { get; set; }
        public String User { get; set; }
        private int date = 0;
        public int Date {
            get { return date; }
            set
            {
                date = value;
                DateString = dateStringArr[date];
            }
        }
        private String dateString = dateStringArr[0];
        public String DateString
        {
            get { return dateString; }
            set
            {
                dateString = value;
                NotifyPropertyChanged("DateString");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

    }
}
