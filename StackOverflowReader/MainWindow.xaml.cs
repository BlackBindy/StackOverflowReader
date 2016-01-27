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
using System.Text;
using System.Collections;

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
            searchedItems.Clear();
            RequestJson(title, tag, user, date);
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

        private void RequestJson(String title, String tag, String user, int date)
        {
            string baseUrl = "https://api.stackexchange.com/2.2/search/advanced?order=desc&sort=activity&site=stackoverflow";
            string addUrl = "";

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

            if (title != null && title.Equals("") == false)
                addUrl = addUrl + "&title=" + title;
            if (tag != null && tag.Equals("") == false)
                addUrl = addUrl + "&tagged=" + tag;

            if (user != null && user.Equals("") == false)
            {
                List<User> userList = RequestJsonName(user);
                foreach (User curUser in userList)
                {
                    string result = null;
                    string newAddUrl = addUrl + "&user=" + curUser.UserID;

                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl + newAddUrl);
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        Stream stream = response.GetResponseStream();

                        using (GZipStream decompress = new GZipStream(stream, CompressionMode.Decompress))
                        {
                            StreamReader reader = new StreamReader(decompress, Encoding.UTF8);
                            result = reader.ReadToEnd();
                        }
                        stream.Close();
                        response.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.StackTrace);
                    }
                    foreach(Post curPost in ParseJson(result))
                        searchedItems.Add(curPost);
                }
            }
            else
            {
                string result = null;

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl + addUrl);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream stream = response.GetResponseStream();

                    using (GZipStream decompress = new GZipStream(stream, CompressionMode.Decompress))
                    {
                        StreamReader reader = new StreamReader(decompress, Encoding.UTF8);
                        result = reader.ReadToEnd();
                    }
                    stream.Close();
                    response.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
                foreach (Post curPost in ParseJson(result))
                    searchedItems.Add(curPost);
            }                       
        }

        private List<Post> ParseJson(String json)
        {
            List<Post> resultArr = new List<Post>();
            if (json != null)
            {
                JObject obj = JObject.Parse(json);
                JArray array = JArray.Parse(obj["items"].ToString());
                foreach (JObject itemObj in array)
                {
                    Post post = new Post();
                    post.Title = WebUtility.HtmlDecode(itemObj["title"].ToString());
                    post.User = WebUtility.HtmlDecode(itemObj["owner"]["display_name"].ToString());

                    DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(itemObj["creation_date"].ToString()));
                    post.Date = dt.Year + "." + dt.Month + "." + dt.Day;
                    post.Link = itemObj["link"].ToString();
                    resultArr.Add(post);
                }
            }
            return resultArr;
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

        private List<User> RequestJsonName(String name)
        {
            List<User> userArr = new List<User>();
            string result = null;
            string baseUrl = "https://api.stackexchange.com/2.2/users?order=desc&sort=reputation&site=stackoverflow";
            string addUrl = "";
            

            if (name != null && name.Equals("") == false)
                addUrl = addUrl + "&inname=" + name;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl + addUrl);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();

                using (GZipStream decompress = new GZipStream(stream, CompressionMode.Decompress))
                {
                    StreamReader reader = new StreamReader(decompress, Encoding.UTF8);
                    result = reader.ReadToEnd();
                }
                stream.Close();
                response.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            userArr = ParseJsonName(result);

            return userArr;
        }

        private List<User> ParseJsonName(String json)
        {
            List<User> resultArr = new List<User>();

            JObject obj = JObject.Parse(json);
            JArray array = JArray.Parse(obj["items"].ToString());
            foreach (JObject itemObj in array)
            {
                User user = new User();
                user.DisplayName = WebUtility.HtmlDecode(itemObj["display_name"].ToString());
                user.UserID = itemObj["user_id"].ToString();

                resultArr.Add(user);
            }
            return resultArr;
        }

    }
}
