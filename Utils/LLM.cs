using Newtonsoft.Json;
using RestSharp;
using System.Text;

namespace WhatsApp
{
    public static partial class Program
    {
        public static async Task<string> SendRequestToReplicate(object input)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ReplicateApiKey}");
                client.DefaultRequestHeaders.Add("Prefer", "wait");

                var content = new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(ReplicateUrl, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }
        }
        private static async Task<string> SendRequestToHuggingFace(object input)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {HuggingFaceApiKey}");

                var content = new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(HuggingFaceApiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }
        }
        public static async Task<string> SendRequestToCohere(object input)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {CohereApiKey}");

                var content = new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(CohereApiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }
        }

        public static async Task<string> GetImg()
        {
            var options = new RestClientOptions("https://api.getimg.ai/v1/stable-diffusion/text-to-image");
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Bearer key-4Wfo83PTu8cvJCfmzNL26eiTVRmaz97RcIrLYd8ayPfK294xAJMuYxLbxmkjxm7GcYrZdwIy86sEF9jdBd2V6feaUkapeEln");
            var response = await client.PostAsync(request);

            Console.WriteLine("{0}", response.Content);
            return "See Console";
        }
    }
}