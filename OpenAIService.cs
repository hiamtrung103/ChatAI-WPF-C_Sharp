using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WPF_UI
{
    public class OpenAIService
    {
        private readonly HttpClient httpClient;

        public OpenAIService(string apiKey)
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
        }

        public async Task<string> GetChatbotResponse(string message, string instructions, double maxTokens, double temperature, double topP, double frequencyPenalty, double presencePenalty)
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo-16k",
                messages = new[]
                {
                    new { role = "system", content = "Bạn là TrungAI. Bạn sẽ tư vấn tuyển sinh, chọn ngành nghề và một bạn đặc biệt phải các lưu ý sau: \"" + instructions + "\"" },
                    new { role = "user", content = message }
                },
                max_tokens = (int)maxTokens,
                temperature = temperature,
                top_p = topP,
                frequency_penalty = frequencyPenalty,
                presence_penalty = presencePenalty
            };

            var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
            response.EnsureSuccessStatusCode(); // phản hồi nếu không thành công
            string responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseString);
            return responseJson["choices"][0]["message"]["content"].ToString();
        }
    }
}
