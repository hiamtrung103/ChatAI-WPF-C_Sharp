using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using WPF_UI;
using MongoDB.Driver;
using System.Configuration;
using System.Windows.Media.Imaging;

namespace WPF_UI
{
    public partial class Dashboard : UserControl
    {
        private readonly OpenAIService openAIService;
        private readonly IMongoCollection<Thread> _threadsCollection;
        private List<Thread> chuDe = new List<Thread>();
        private int demChuDe = 0;
        private int chiSoChuDeHienTai = -1;

        public Dashboard()
        {
            InitializeComponent();

            var apiKey = "sk-proj-JMbRhiyE0ryWB3c3983tT3BlbkFJ9jnRYECRfHgxr89mDksK";
            openAIService = new OpenAIService(apiKey);

            var mongoClient = new MongoClient(ConfigurationManager.AppSettings["MongoDbConnectionString"]);
            var mongoDatabase = mongoClient.GetDatabase(ConfigurationManager.AppSettings["MongoDbDatabaseName"]);
            _threadsCollection = mongoDatabase.GetCollection<Thread>(ConfigurationManager.AppSettings["MongoDbThreadsCollectionName"]);

            TaiChuDeTuMongo(); // Tải các chủ đề từ MongoDB
            CapNhatTieuDeHienTai(); // Cập nhật tiêu đề hiện tại

            ChatWebView.CoreWebView2InitializationCompleted += ChatWebView_CoreWebView2InitializationCompleted;
        }

        private async void ChatWebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            ChatWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            if (chiSoChuDeHienTai >= 0)
            {
                TaiChuDe(chuDe[chiSoChuDeHienTai]); // Hiển thị lại chủ đề hiện tại sau khi khởi tạo WebView
            }
        }

        private async void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string tinNhanNguoiDung = e.TryGetWebMessageAsString();
            if (!string.IsNullOrEmpty(tinNhanNguoiDung) && chiSoChuDeHienTai >= 0)
            {
                DateTime thoiGian = DateTime.Now;
                ThemTinNhanVaoChat(tinNhanNguoiDung, "User", thoiGian);
                try
                {
                    string instructions = InstructionsTextBox.Text;
                    double maxTokens = MaxTokensSlider.Value;
                    double temperature = TemperatureSlider.Value;
                    double topP = TopPSlider.Value;
                    double frequencyPenalty = FrequencyPenaltySlider.Value;
                    double presencePenalty = PresencePenaltySlider.Value;

                    string phanHoiBot = await openAIService.GetChatbotResponse(tinNhanNguoiDung, instructions, maxTokens, temperature, topP, frequencyPenalty, presencePenalty);
                    ThemTinNhanVaoChat(phanHoiBot, "TrungAI", DateTime.Now);
                    LuuTinNhanVaoChuDe(tinNhanNguoiDung, phanHoiBot, thoiGian);
                }
                catch (HttpRequestException httpEx)
                {
                    ThemTinNhanVaoChat("Error: Không nhận được phản hồi từ máy chủ.", "System", DateTime.Now);
                    Console.WriteLine(httpEx.Message);
                }
                catch (Exception ex)
                {
                    ThemTinNhanVaoChat("Error: Đã xảy ra lỗi không mong muốn.", "System", DateTime.Now);
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void CapNhatTieuDeHienTai()
        {
            if (chiSoChuDeHienTai >= 0 && chiSoChuDeHienTai < chuDe.Count)
            {
                CurrentTopicTextBlock.Text = "Chủ đề hiện tại: " + chuDe[chiSoChuDeHienTai].Title;
            }
            else
            {
                CurrentTopicTextBlock.Text = "Chưa có chủ đề";
            }
        }

        private void ChinhSuaTieuDe_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var chuDe = menuItem.Tag as Thread;
            var inputDialog = new InputDialog("Chỉnh sửa tiêu đề", chuDe.Title);
            if (inputDialog.ShowDialog() == true)
            {
                chuDe.Title = inputDialog.ResponseText;
                CapNhatDanhSachChuDe();
                CapNhatTieuDeHienTai();
                LuuChuDeVaoMongo();
            }
        }

        private void XoaLichSuChuDe_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var chuDe = menuItem.Tag as Thread;
            chuDe.Messages.Clear();
            if (chuDe == this.chuDe[chiSoChuDeHienTai])
            {
                ChatStackPanel.Children.Clear();
            }
            LuuChuDeVaoMongo();
        }

        private void XoaChuDe_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var chuDe = menuItem.Tag as Thread;

            // Xóa chủ đề khỏi danh sách chủ đề trong bộ nhớ
            this.chuDe.Remove(chuDe);

            // Xóa chủ đề khỏi MongoDB
            var filter = Builders<Thread>.Filter.Eq(t => t.Id, chuDe.Id);
            _threadsCollection.DeleteOne(filter);

            // Cập nhật giao diện
            if (this.chuDe.Count == 0)
            {
                TaoChuDeMoi();
            }
            else
            {
                if (chiSoChuDeHienTai >= this.chuDe.Count)
                {
                    chiSoChuDeHienTai = this.chuDe.Count - 1;
                }
                TaiChuDe(this.chuDe[chiSoChuDeHienTai]);
            }
            CapNhatDanhSachChuDe();
            CapNhatTieuDeHienTai();
        }

        // Thêm tin nhắn vào giao diện chat
        private void ThemTinNhanVaoChat(string tinNhan, string tenNguoiGui, DateTime thoiGian)
        {
            // Tạo một Border để chứa tin nhắn
            Border border = new Border
            {
                Background = Brushes.LightGray,
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(10),
                Margin = new Thickness(5),
                MaxWidth = ChatStackPanel.ActualWidth * 0.9, // Giới hạn chiều rộng tối đa 80% của ChatStackPanel
                HorizontalAlignment = tenNguoiGui == "User" ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            // Tạo một Grid để chứa icon và tin nhắn
            Grid containerGrid = new Grid();
            containerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            containerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            containerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Tạo Image để hiển thị icon
            Image icon = new Image
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // Thiết lập icon dựa trên người gửi
            if (tenNguoiGui == "User")
            {
                icon.Source = new BitmapImage(new Uri("C:\\Users\\trung\\Documents\\GitHub\\ChatAI-WPF-C_Sharp\\Images\\user.png"));
            }
            else if (tenNguoiGui == "TrungAI")
            {
                icon.Source = new BitmapImage(new Uri("C:\\Users\\trung\\Documents\\GitHub\\ChatAI-WPF-C_Sharp\\Images\\ai.png"));
            }

            // Tạo một StackPanel để chứa nội dung tin nhắn
            StackPanel messagePanel = new StackPanel();

            // Tạo TextBlock để hiển thị tên người gửi
            TextBlock nameBlock = new TextBlock
            {
                Text = tenNguoiGui,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 2),
            };

            // Tạo TextBlock để hiển thị nội dung tin nhắn
            TextBlock messageBlock = new TextBlock
            {
                Text = tinNhan,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 16
            };

            // Tạo TextBlock để hiển thị thời gian gửi tin nhắn
            TextBlock timeBlock = new TextBlock
            {
                Text = thoiGian.ToString("HH:mm"),
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Thêm các TextBlock vào StackPanel messagePanel
            messagePanel.Children.Add(nameBlock);
            messagePanel.Children.Add(messageBlock);
            messagePanel.Children.Add(timeBlock);

            // Thêm icon vào containerGrid
            Grid.SetRow(icon, 0);
            Grid.SetColumn(icon, 0);
            containerGrid.Children.Add(icon);

            // Thêm messagePanel vào containerGrid
            Grid.SetRow(messagePanel, 0);
            Grid.SetColumn(messagePanel, 1);
            containerGrid.Children.Add(messagePanel);

            // Thêm containerGrid vào Border
            border.Child = containerGrid;
            ChatStackPanel.Children.Add(border);
            ChatScrollViewer.ScrollToEnd();
        }

        // Lưu tin nhắn vào chủ đề
        private void LuuTinNhanVaoChuDe(string tinNhanNguoiDung, string phanHoiBot, DateTime thoiGian)
        {
            var chuDeHienTai = chuDe[chiSoChuDeHienTai];
            chuDeHienTai.Messages.Add(new Message { Content = tinNhanNguoiDung, Sender = "User", Timestamp = thoiGian });
            chuDeHienTai.Messages.Add(new Message { Content = phanHoiBot, Sender = "TrungAI", Timestamp = DateTime.Now });
            LuuChuDeVaoMongo();
        }

        // Tạo chủ đề mới
        private void TaoChuDeMoi()
        {
            var chuDeMoi = new Thread { Title = "Chủ đề " + (++demChuDe) };
            chuDe.Add(chuDeMoi);
            chiSoChuDeHienTai = chuDe.Count - 1;
            TaiChuDe(chuDeMoi);
            CapNhatDanhSachChuDe();
            LuuChuDeVaoMongo();
        }

        // Tải chủ đề hiện tại
        private void TaiChuDe(Thread chuDe)
        {
            ChatStackPanel.Children.Clear();
            foreach (var message in chuDe.Messages)
            {
                ThemTinNhanVaoChat(message.Content, message.Sender, message.Timestamp);
            }
            ChatScrollViewer.ScrollToEnd(); // Cuộn đến cuối cùng sau khi thêm tất cả tin nhắn
        }

        // Cập nhật danh sách chủ đề
        private void CapNhatDanhSachChuDe()
        {
            MessageListView.Items.Clear();
            foreach (var chuDe in this.chuDe)
            {
                var listViewItem = new ListViewItem
                {
                    Content = chuDe.Title,
                    Tag = chuDe
                };

                listViewItem.Selected += (s, e) =>
                {
                    var chuDeDuocChon = listViewItem.Tag as Thread;
                    chiSoChuDeHienTai = this.chuDe.IndexOf(chuDeDuocChon);
                    TaiChuDe(chuDeDuocChon);
                    CapNhatTieuDeHienTai(); // Gọi phương thức cập nhật tiêu đề hiện tại
                };

                var contextMenu = new ContextMenu();
                var editTitleMenuItem = new MenuItem { Header = "Chỉnh sửa tiêu đề", Tag = chuDe };
                editTitleMenuItem.Click += ChinhSuaTieuDe_Click;
                contextMenu.Items.Add(editTitleMenuItem);

                var cleanThreadMenuItem = new MenuItem { Header = "Xoá tin nhắn", Tag = chuDe };
                cleanThreadMenuItem.Click += XoaLichSuChuDe_Click;
                contextMenu.Items.Add(cleanThreadMenuItem);

                var deleteThreadMenuItem = new MenuItem { Header = "Xoá lịch sử", Tag = chuDe };
                deleteThreadMenuItem.Click += XoaChuDe_Click;
                contextMenu.Items.Add(deleteThreadMenuItem);

                listViewItem.ContextMenu = contextMenu;
                MessageListView.Items.Add(listViewItem);
            }
        }

        private void ChuDeMoi_Click(object sender, RoutedEventArgs e)
        {
            TaoChuDeMoi();
        }

        private void ThanhTruot_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MaxTokensSlider != null && MaxTokensValue != null)
            {
                MaxTokensValue.Text = MaxTokensSlider.Value.ToString();
            }
            if (TemperatureSlider != null && TemperatureValue != null)
            {
                TemperatureValue.Text = TemperatureSlider.Value.ToString("0.0");
            }
            if (TopPSlider != null && TopPValue != null)
            {
                TopPValue.Text = TopPSlider.Value.ToString("0.00");
            }
            if (FrequencyPenaltySlider != null && FrequencyPenaltyValue != null)
            {
                FrequencyPenaltyValue.Text = FrequencyPenaltySlider.Value.ToString("0.00");
            }
            if (PresencePenaltySlider != null && PresencePenaltyValue != null)
            {
                PresencePenaltyValue.Text = PresencePenaltySlider.Value.ToString("0.00");
            }
        }

        // Lưu các chủ đề vào MongoDB
        private void LuuChuDeVaoMongo()
        {
            foreach (var thread in chuDe)
            {
                if (thread.Id == null)
                {
                    _threadsCollection.InsertOne(thread);
                }
                else
                {
                    var filter = Builders<Thread>.Filter.Eq(t => t.Id, thread.Id);
                    _threadsCollection.ReplaceOne(filter, thread);
                }
            }
        }

        // Tải các chủ đề từ MongoDB
        private void TaiChuDeTuMongo()
        {
            chuDe = _threadsCollection.Find(_ => true).ToList();
            if (chuDe != null && chuDe.Count > 0)
            {
                demChuDe = chuDe.Count;
                chiSoChuDeHienTai = 0; // Chọn chủ đề đầu tiên
                CapNhatDanhSachChuDe(); // Cập nhật danh sách chủ đề
                TaiChuDe(chuDe[chiSoChuDeHienTai]); // Hiển thị chủ đề đầu tiên
                CapNhatTieuDeHienTai(); // Cập nhật tiêu đề hiện tại
            }
            else
            {
                TaoChuDeMoi(); // Tạo chủ đề mới nếu không có chủ đề nào
            }
        }
    }
}
