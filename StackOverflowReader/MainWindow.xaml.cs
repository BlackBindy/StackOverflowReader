using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Reflection;

namespace StackOverflowReader
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Post> searchedItems;
        SearchOption searchOption;

        public MainWindow()
        {
            InitializeComponent();
            searchedItems = new ObservableCollection<Post>();
            searchOption = new SearchOption();

            PostList.ItemsSource = searchedItems;
            SearchOptionGrid.DataContext = searchOption;
        }

        void Search(String title, String tag, String user, int date)
        {
            String rawResult = RequestJson(title, tag, user, date);
            ParseJson(rawResult);
        }

        private void onSearchClick(object sender, RoutedEventArgs e)
        {
            Search(searchOption.Title, searchOption.Tag, searchOption.User, searchOption.Date);
        }

        private void onPreviewMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            Post selectedPost = ((ListView)sender).SelectedItem as Post;
            try
            {
                PostGrid.DataContext = new Post() { Title = selectedPost.Title, User = selectedPost.User, Date = selectedPost.Date, Link = selectedPost.Link };
                StackOverflowBrowser.Navigate(selectedPost.Link);
                StackOverflowBrowser.Navigated += (a, b) => { HideScriptErrors(StackOverflowBrowser, true); };
            }
            catch (Exception exc)
            {

            }
        }

        private String RequestJson(String title, String tag, String user, int date)
        {
            string result = null;
            string baseUrl = "https://api.stackexchange.com/2.2/search/advanced?order=desc&sort=activity&site=stackoverflow";
            string addUrl = "";

            if (title != null && title.Equals("") == false)
                addUrl = addUrl + "&title=" + title;
            if (tag != null && tag.Equals("") == false)
                addUrl = addUrl + "&tagged=" + tag;
            if (user != null && user.Equals("") == false)
                addUrl = addUrl + "&user=" + user;

            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            long toDate = (long)t.TotalSeconds;

            switch (date) //fromdate=1451606400&todate=1451692800 -- 86400 -> 하루를 초단위로 나타낸거 [....] / week : 604800 / month : 2592000 / year : 31536000 / 3 years : 94608000
            {
                case 0: // Today
                    addUrl = addUrl + "&fromdate=" + (toDate - 86400) + "&todate=" + toDate;
                    break;
                case 1: // This week
                    addUrl = addUrl + "&fromdate=" + (toDate - 604800) + "&todate=" + toDate;
                    break;
                case 2: // This month
                    addUrl = addUrl + "&fromdate=" + (toDate - 2592000) + "&todate=" + toDate;
                    break;
                case 3: // This year
                    addUrl = addUrl + "&fromdate=" + (toDate - 31536000) + "&todate=" + toDate;
                    break;
                case 4: // 3 Years
                    addUrl = addUrl + "&fromdate=" + (toDate - 94608000) + "&todate=" + toDate;
                    break;
                case 5: // All
                default:
                    break;
            }

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl + addUrl);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();

                using (GZipStream decompress = new GZipStream(stream, CompressionMode.Decompress))
                {
                    StreamReader reader = new StreamReader(decompress);
                    result = reader.ReadToEnd();
                }
                stream.Close();
                response.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            return result;
        }

        private void ParseJson(String json)
        {
            searchedItems.Clear();

            JObject obj = JObject.Parse(json);
            JArray array = JArray.Parse(obj["items"].ToString());
            foreach(JObject itemObj in array)
            {
                Post post = new Post();
                post.Title = itemObj["title"].ToString();
                post.User = itemObj["owner"]["display_name"].ToString();

                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(itemObj["creation_date"].ToString()));
                post.Date = dt.Year + "." + dt.Month + "." + dt.Day;
                post.Link = itemObj["link"].ToString();
                searchedItems.Add(post);
            }
        }

        public void HideScriptErrors(WebBrowser wb, bool hide)
        {
            var fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;
            var objComWebBrowser = fiComWebBrowser.GetValue(wb);
            if (objComWebBrowser == null)
            {
                wb.Loaded += (o, s) => HideScriptErrors(wb, hide); //In case we are to early
                return;
            }
            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });
        }
    }
}
