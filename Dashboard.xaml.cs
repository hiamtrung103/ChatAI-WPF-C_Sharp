using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Net.Http.Json;
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

namespace WPF_UI
{
    public partial class Dashboard : UserControl
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private List<Thread> chuDe = new List<Thread>();
        private int demChuDe = 0;
        private int chiSoChuDeHienTai = -1;

        private string duongDanLichSuChat = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ChatHistoryTrungAI.json");

        public Dashboard()
        {
            InitializeComponent();
            TaiChuDeTuFile();
            if (chuDe.Count == 0)
            {
                TaoChuDeMoi();
            }
            CapNhatTieuDeHienTai();
            ChatWebView.CoreWebView2InitializationCompleted += ChatWebView_CoreWebView2InitializationCompleted;
        }

        private async void ChatWebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            ChatWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
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
                    string phanHoiBot = await LayPhanHoiChatbot(tinNhanNguoiDung);
                    ThemTinNhanVaoChat(phanHoiBot, "Bot", DateTime.Now);
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
                LuuChuDeVaoFile();
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
            LuuChuDeVaoFile();
        }

        private void XoaChuDe_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var chuDe = menuItem.Tag as Thread;
            this.chuDe.Remove(chuDe);
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
            LuuChuDeVaoFile();
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
                HorizontalAlignment = tenNguoiGui == "User" ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            // Tạo một StackPanel để chứa nội dung tin nhắn
            StackPanel messagePanel = new StackPanel();

            // Tạo TextBlock để hiển thị tên người gửi
            TextBlock nameBlock = new TextBlock
            {
                Text = tenNguoiGui,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 2)
            };

            // Tạo TextBlock để hiển thị nội dung tin nhắn
            TextBlock messageBlock = new TextBlock
            {
                Text = tinNhan,
                TextWrapping = TextWrapping.Wrap
            };

            // Tạo TextBlock để hiển thị thời gian gửi tin nhắn
            TextBlock timeBlock = new TextBlock
            {
                Text = thoiGian.ToString("HH:mm"),
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Thêm các TextBlock vào StackPanel
            messagePanel.Children.Add(nameBlock);
            messagePanel.Children.Add(messageBlock);
            messagePanel.Children.Add(timeBlock);

            // Thêm StackPanel vào Border
            border.Child = messagePanel;
            ChatStackPanel.Children.Add(border);
            ChatScrollViewer.ScrollToEnd();
        }

        // Gửi yêu cầu đến chatbot và nhận phản hồi
        private async Task<string> LayPhanHoiChatbot(string tinNhan)
        {
            string apiKey = "sk-proj-JMbRhiyE0ryWB3c3983tT3BlbkFJ9jnRYECRfHgxr89mDksK";
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

            var requestBody = new
            {
                model = "gpt-3.5-turbo-16k",
                messages = new[]
                {
                    new { role = "system", content = InstructionsTextBox.Text },
                    new { role = "user", content = tinNhan }
                },
                max_tokens = (int)MaxTokensSlider.Value,
                temperature = TemperatureSlider.Value,
                top_p = TopPSlider.Value,
                frequency_penalty = FrequencyPenaltySlider.Value,
                presence_penalty = PresencePenaltySlider.Value
            };

            var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
            response.EnsureSuccessStatusCode(); // phản hồi nếu không thành công
            string responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseString);
            return responseJson["choices"][0]["message"]["content"].ToString();
        }

        // Lưu tin nhắn vào chủ đề
        private void LuuTinNhanVaoChuDe(string tinNhanNguoiDung, string phanHoiBot, DateTime thoiGian)
        {
            var chuDeHienTai = chuDe[chiSoChuDeHienTai];
            chuDeHienTai.Messages.Add(new Message { Content = tinNhanNguoiDung, Sender = "User", Timestamp = thoiGian });
            chuDeHienTai.Messages.Add(new Message { Content = phanHoiBot, Sender = "Bot", Timestamp = DateTime.Now });
            LuuChuDeVaoFile();
        }

        // Tạo chủ đề mới
        private void TaoChuDeMoi()
        {
            var chuDeMoi = new Thread { Title = "Chủ đề " + (++demChuDe) };
            chuDe.Add(chuDeMoi);
            chiSoChuDeHienTai = chuDe.Count - 1;
            TaiChuDe(chuDeMoi);
            CapNhatDanhSachChuDe();
            LuuChuDeVaoFile();
        }

        // Tải chủ đề hiện tại
        private void TaiChuDe(Thread chuDe)
        {
            ChatStackPanel.Children.Clear();
            foreach (var message in chuDe.Messages)
            {
                ThemTinNhanVaoChat(message.Content, message.Sender, message.Timestamp);
            }
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

                // Thêm sự kiện khi chọn chủ đề
                listViewItem.Selected += (s, e) =>
                {
                    var chuDeDuocChon = listViewItem.Tag as Thread;
                    chiSoChuDeHienTai = this.chuDe.IndexOf(chuDeDuocChon);
                    TaiChuDe(chuDeDuocChon);
                };

                // Tạo menu context
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

        // Lưu các chủ đề vào file
        private void LuuChuDeVaoFile()
        {
            var json = JsonConvert.SerializeObject(chuDe);
            File.WriteAllText(duongDanLichSuChat, json);
        }

        // Tải các chủ đề từ file
        private void TaiChuDeTuFile()
        {
            if (File.Exists(duongDanLichSuChat))
            {
                var json = File.ReadAllText(duongDanLichSuChat);
                chuDe = JsonConvert.DeserializeObject<List<Thread>>(json);
                if (chuDe != null && chuDe.Count > 0)
                {
                    demChuDe = chuDe.Count;
                    chiSoChuDeHienTai = 0;
                    TaiChuDe(chuDe[chiSoChuDeHienTai]);
                    CapNhatDanhSachChuDe();
                }
            }
        }

        public class Thread
        {
            public string Title { get; set; }
            public List<Message> Messages { get; set; } = new List<Message>();
        }

        public class Message
        {
            public string Content { get; set; }
            public string Sender { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
