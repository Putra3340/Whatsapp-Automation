using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Management;
using Newtonsoft.Json;
using PuppeteerSharp;
using SystemInfo;
using Newtonsoft.Json.Linq;
using DotNetEnv;

namespace Puppeeter_Api
{
#pragma warning disable CS0618
    //just helping me with string

    public static partial class StringExtensions
    {
        
        public static bool IsNotNullOrEmpty(this string pe)
        {
            if(pe == null || pe == "")
            {
                return false;
            }
            return true;
        }
    }
    class Program
    {

        private static readonly string CohereApiUrl = Environment.GetEnvironmentVariable("COHERE_API_URL");
        private static readonly string CohereApiKey = Environment.GetEnvironmentVariable("COHERE_API_KEY");
        private static readonly string HuggingFaceApiUrl = Environment.GetEnvironmentVariable("HUGGINGFACE_API_URL");
        private static readonly string HuggingFaceApiKey = Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY");
        private static readonly string ReplicateApiKey = Environment.GetEnvironmentVariable("REPLICATE_API_KEY");
        private static readonly string ReplicateUrl = Environment.GetEnvironmentVariable("REPLICATE_URL");
        public static SystemInf Os = new SystemInf();
        public static bool work = false;
        public static string FormData = "";
        public static async Task Main(string[] args)
        {
            Env.Load();  // Load the environment variables from the .env file


            Console.WriteLine("Checking Updates!");
            await new BrowserFetcher().DownloadAsync();
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false, // Change to false for visible browser
                UserDataDir = "./Puput",
                Args = new[]
    {
        "--no-sandbox",
        "--disable-setuid-sandbox",
        "--autoplay-policy=no-user-gesture-required",
        "--disable-extensions-except=~/.config/google-chrome-for-testing/Default/Extensions/cmdgdghfledlbkbciggfjblphiafkcgg/1.6.1_0", //this is adblocker
        "--load-extension=~/.config/google-chrome-for-testing/Default/Extensions/cmdgdghfledlbkbciggfjblphiafkcgg/1.6.1_0",
        "--start-maximized" // Launch maximized

    }
            });
            Console.WriteLine("Setup Browser..");

            var page = await browser.NewPageAsync();
            if (File.Exists("cookies.json"))
            {
                Console.WriteLine("Cookie exists.");
                var cookies = JsonConvert.DeserializeObject<CookieParam[]>(File.ReadAllText("cookies.json"));
                await page.SetCookieAsync(cookies);
            }
            else
            {
                Console.WriteLine("Cookie does not exist.");
                var cookies = await page.GetCookiesAsync();
                File.WriteAllText("cookies.json", JsonConvert.SerializeObject(cookies));
            }

            await page.GoToAsync("https://putrartx.my.id");
            await page.EvaluateExpressionAsync("window.moveTo(0, 0); window.resizeTo(screen.width, screen.height);");
            // Set the viewport to emulate full screen
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 1366,
                Height = 654
            });

            await page.GoToAsync("https://web.whatsapp.com/");
            await Task.Delay(3000);

            Console.WriteLine("Waiting Profile");
            GoToChat(page);
            Console.WriteLine("Goto Profile Done!");
            work = true;

            //extract latest message
            string LastMsg = String.Empty;
            string LastMsgTime = String.Empty;
            string LastMsgID = string.Empty;
            int MaxDuplicateMsgCount = 3;
            int LastLength = 0;
            while (work)
            {
                try
                {
                    string TimeBak = LastMsgTime;
                    await Task.Delay(1000);
                    //max 3 repeated command per minute
                    var newmessage = await page.XPathAsync("//div[@class='copyable-text' and not(contains(@data-pre-plain-text,' Rahmad Syaputra'))]//span[@dir]/span");
                    if (newmessage.Length > 0)
                    {
                        LastMsg = await newmessage.Last().EvaluateFunctionAsync<string>("e => e.innerText");
                    }
                    var cek = await page.XPathAsync("//div[@class='copyable-text' and not(contains(@data-pre-plain-text,' Rahmad Syaputra'))]/following-sibling::div");
                    if (cek.Length > 0)
                    {
                        LastMsgTime = await cek.Last().EvaluateFunctionAsync<string>("e => e.innerText");

                    }
                    string CurrentDay = DateTime.Now.ToString("yyyy-MM-dd");
                    if (LastLength < newmessage.Length || LastMsgTime != TimeBak)
                    {

                        // Handle Command here
                        Console.WriteLine("It should exec");

                        if (LastMsg.IsNotNullOrEmpty())
                        {
                            if (LastMsg == "/ping")
                            {
                                SendMsg(page, Os.GetSystemInfo());
                            }
                            if(LastMsg.StartsWith("/gen"))
                            {

                                string prompt = LastMsg.Split("/gen ").Last() + ", Give me very simple Answer that is less than 100 lines please!1";
                                var input = new
                                {
                                    prompt = prompt,
                                    max_tokens = 200,  // Max tokens for the response
                                    temperature = 0.7,  // Controls the randomness of the response
                                    top_p = 0.9         // Controls the diversity of the response
                                };

                                var response = await SendRequestToCohere(input);
                                Console.WriteLine(response);
                                // Parse the JSON response
                                JObject responseObject = JObject.Parse(response);

                                // Extract the 'text' field from the 'generations' array
                                string generatedText = responseObject["generations"][0]["text"].ToString();

                                // Output the result
                                Console.WriteLine(generatedText);
                                SendMsg(page, generatedText);
                            }
                        }

                        if (LastMsg.IsNotNullOrEmpty() && LastMsgTime.IsNotNullOrEmpty())
                        {
                            LastMsgID = LastMsg + "-" + LastMsgTime + "-" + CurrentDay + "\n";
                            System.IO.File.AppendAllText("chat.log", LastMsgID);
                            //if (CheckLastThreeLinesAreSame("chat.log"))
                            //{
                            //    Console.WriteLine("The last 3 lines are the same.");
                            //}
                            //else
                            //{
                            //    Console.WriteLine("The last 3 lines are not the same.");
                            //}
                        }
                    }
                    else
                    {
                        Console.WriteLine("Nope Same msg");
                    }
                    skip:
                    LastLength = newmessage.Length;
                }
                catch (Exception ex)
                {
                    SendMsg(page,ex.Message);
                }
                
            }
        }

        //setup
        public async static void GoToChat(IPage page)
        {
            while (true)
            {
                var mycontact = await page.XPathAsync("//span[@title='Aku I' and contains(text(),'Aku I')]");
                if (mycontact.Length > 0)
                {
                    await mycontact[0].ClickAsync();

                    break;
                }
            }
        }

        public async static void SendMsg(IPage page, string msg = "Error: Unhandled Message!\n")
        {
            while (true)
            {
                var input = await page.XPathAsync("//div[@aria-placeholder='Ketik pesan']");
                if (input.Length > 0)
                {
                    // Split the message by \r\n (newline with carriage return) to handle each line
                    msg = msg.Replace("\r", "");
                    var lines = msg.Split(new[] { "\n" }, StringSplitOptions.None);

                    foreach (var line in lines)
                    {
                        // Type the line in the input field
                        await input[0].TypeAsync(line);
                        if(Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            // Simulate Shift + Enter (to create a line break without submitting)
                            await page.Keyboard.DownAsync("Shift");
                            await page.Keyboard.PressAsync("Enter");
                            await page.Keyboard.UpAsync("Shift");
                        }
                        else
                        {
                            // Simulate Shift + Enter using JavaScript (for Linux compatibility)
                            await page.EvaluateFunctionAsync(@"(element) => {
                const shiftEnterEvent = new KeyboardEvent('keydown', {
                    key: 'Enter',
                    shiftKey: true
                });
                element.dispatchEvent(shiftEnterEvent);
            }", input[0]);
                        }
                        
                    }
                    break;
                }

            }

            await Task.Delay(1000);
            while (true)
            {
                var send = await page.XPathAsync("//span[@data-icon='send']/..");
                if (send.Length > 0)
                {
                    await send[0].ClickAsync();
                    break;
                }
            }
        }
        public static bool CheckLastThreeLinesAreSame(string filePath)
        {
            // Read all lines from the file
            var lines = File.ReadLines(filePath).ToList();

            // Ensure the file has at least 3 lines
            if (lines.Count < 3)
            {
                return false;
            }

            // Get the last 3 lines
            var lastThreeLines = lines.Skip(Math.Max(0, lines.Count - 3)).ToList();

            // Check if the last three lines are the same
            return lastThreeLines[0] == lastThreeLines[1] && lastThreeLines[1] == lastThreeLines[2];
        }
        private static async Task<string> SendRequestToReplicate(object input)
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
        private static async Task<string> SendRequestToCohere(object input)
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
    }
}
