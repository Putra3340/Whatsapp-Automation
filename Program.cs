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
using StringExt;
using BotPress;
using Whatsapp;

namespace WhatsApp
{
#pragma warning disable CS0618
    
    public partial class Program
    {
        // API
        private static readonly string CohereApiUrl = Environment.GetEnvironmentVariable("COHERE_API_URL");
        private static readonly string CohereApiKey = Environment.GetEnvironmentVariable("COHERE_API_KEY");
        private static readonly string HuggingFaceApiUrl = Environment.GetEnvironmentVariable("HUGGINGFACE_API_URL");
        private static readonly string HuggingFaceApiKey = Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY");
        private static readonly string ReplicateApiKey = Environment.GetEnvironmentVariable("REPLICATE_API_KEY");
        private static readonly string ReplicateUrl = Environment.GetEnvironmentVariable("REPLICATE_URL");

        // Others
        public static SystemInf Os = new SystemInf();
        public static Messages msg = new Messages();
        public static bool ReadMode = false;
        public static bool isActive = false;
        public static bool isSkipMsg = true;
        public static bool isBusy = false;
        public static bool isMuted = true;
        public static bool BotPressMode = false;   
        public static bool BotHaluMode = false;   
        public static bool isProcesingQueue = true;
        public static string LastMethod = "";
        public static string ChatLogs = "chat.log";
        public static string QueueLogs = "queue.log";
        public static string DefaultImagePath = $"{Path.Combine(Directory.GetCurrentDirectory(), "Images")}";


        public static string LastMsg = String.Empty;
        public static string LastMsgTime = String.Empty;
        public static string LastMsgID = string.Empty;
        public static int MaxDuplicateMsgCount = 3;
        public static char State = 'I';
        public static int LastLength = 100;
        public static int LastLength2 = 100;
        public static string CurrentVersion = "1.0";

        public static IBrowser browser = null;
        public static IPage WhatsappPage = null;

        [STAThread] // Required for clipboard operations
        public static async Task Main(string[] args)
        {
            Program.LastMethod = "Main";

            Env.Load();  // Load the environment variables from the .env file
            Console.WriteLine("Checking Updates!");
            await GetLatest(); //Check updates

            // Init Client
            // Image Dir
            if (!Directory.Exists(DefaultImagePath))
            {
                Directory.CreateDirectory(DefaultImagePath);
                Console.WriteLine($"Created directory: {DefaultImagePath}");
            }
            if(!File.Exists(ChatLogs))
            {
                File.Create(ChatLogs);
                Console.WriteLine($"Created file: {ChatLogs}");

            }

            if(!File.Exists(QueueLogs))
            {
                File.Create (QueueLogs);
                Console.WriteLine($"Created file: {QueueLogs}");
            }
            // Setup Browser
            Console.WriteLine("Init Browser");

            await new BrowserFetcher().DownloadAsync();
            browser = await Puppeteer.LaunchAsync(new LaunchOptions
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

            //Setup
            Console.WriteLine("Setup Plugins..");


            Bot.Create(); // Setup The Botpress Page
            BotCharAi.Create(); // Setup C.ai Page
            Brat.Create();

            await CreatePage();
            // CreatePage(browser); so the whatsapp cant open 2 pages

            Console.WriteLine("Program Ends..");
            Console.ReadLine();
        }

        // Init Whatsapp page
        public static async Task<string> CreatePage()
        {
            Program.LastMethod = "CreatePage";
            // Create Whatsapp Page

            WhatsappPage = await browser.NewPageAsync();

            if (File.Exists("cookies.json"))
            {
                Console.WriteLine("Cookie exists.");
                var cookies = JsonConvert.DeserializeObject<CookieParam[]>(File.ReadAllText("cookies.json"));
                await WhatsappPage.SetCookieAsync(cookies);
            }
            else
            {
                Console.WriteLine("Cookie does not exist.");
                var cookies = await WhatsappPage.GetCookiesAsync();
                File.WriteAllText("cookies.json", JsonConvert.SerializeObject(cookies));
            }

            await WhatsappPage.EvaluateExpressionAsync("window.moveTo(0, 0); window.resizeTo(screen.width, screen.height);");

            

            await WhatsappPage.SetViewportAsync(new ViewPortOptions
            {
                Width = 1366,
                Height = 654
            });

            await WhatsappPage.GoToAsync("https://web.whatsapp.com/",6969696);
            await Task.Delay(3000);

            Console.WriteLine("Waiting Profile");

            // Go to specific chat
            GoToChat();

            Console.WriteLine("Goto Profile Done!");
            isActive = true;
            await Task.Delay(3000);
            while (isActive)
            {
                try
                {
                    //check if in group or dms
                    var ingroup = await WhatsappPage.XPathAsync("//div[@id='main']//span[contains(@title,'Anda')]");
                    if(ingroup.Length > 0)
                    {
                        // getgroup
                        if (State != 'G')
                        {
                            Console.WriteLine("Currently on Groups");
                            State = 'G';
                        }
                        await msg.GetGroupMsg();
                        await msg.Queue();
                    }
                    else
                    {
                        if(State != 'D')
                        {
                            Console.WriteLine("Currently on DMs");
                            State = 'D';
                        }
                        //await msg.GetLatestDM(page);
                    }
                }
                catch (Exception ex)
                {
                    await msg.SendMsg("Error Occured!!" + ex.Message);
                    await msg.HandleMsg("/debug", true);
                    break;
                }

            }
            return "";
        }

        //setup
        public async static void GoToChat()
        {
            Program.LastMethod = "GoToChat";
            while (true)
            {
                var mycontact = await WhatsappPage.XPathAsync("//span[@title='Test bot']");
                if (mycontact.Length > 0)
                {
                    await mycontact[0].ClickAsync();

                    break;
                }
            }
        }

        public static async Task GetLatest()
        {
            Program.LastMethod = "GetLatest";
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://putrartx.my.id/Api/Versions.php");
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("7FpYX9LrkTNm4qV8CJ3ZD6sKh1tRXvBWAgMP52QLowUdyEzHpGiOkjf0aubnce7VY"), "key_id");
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string temp = await response.Content.ReadAsStringAsync();
            temp = temp.Split("value\":\"").Last().Split("\"").First().ExtractNumbers();
            int version = int.Parse(temp);
            int clientversion = int.Parse(CurrentVersion.ExtractNumbers());
            
            if(version > clientversion) {
                Console.WriteLine($"Currenly on {CurrentVersion}. There is an update to {temp}");
            }else if(version < clientversion)
            {
                Console.WriteLine($"This version is ahead of the server, Assuming you are the dev");
            }else { Console.WriteLine("Currently on Latest Version"); }
        }


    }
}
