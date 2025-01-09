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
        public static bool isActive = false;
        public static bool isSkipMsg = true;
        public static bool isBusy = false;
        public static bool isMuted = true;
        public static bool BotPressMode = false;   
        public static bool BotHaluMode = false;   
        public static string FormData = "";
        public static string ChatLogs = "chat.log";
        public static string QueueLogs = "queue.log";


        public static string LastMsg = String.Empty;
        public static string LastMsgTime = String.Empty;
        public static string LastMsgID = string.Empty;
        public static int MaxDuplicateMsgCount = 3;
        public static char State = 'I';
        public static int LastLength = 100;
        public static int LastLength2 = 100;
        public static string ServerVer = "";
        public static string Current = "0.9b";

        [STAThread] // Required for clipboard operations
        public static async Task Main(string[] args)
        {
            Env.Load();  // Load the environment variables from the .env file


            Console.WriteLine("Checking Updates!");
            await GetLatest();
            if(Current != ServerVer)
            {
                Console.WriteLine("New Update Available!!");
                Current += " => " + ServerVer;
            }
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
            //await Bot.Create(browser);
            BotCharAi.Create(browser);
            
            
            await CreatePage(browser);
            // CreatePage(browser); so the whatsapp cant open 2 pages
            while (true)
            {
                Console.ReadLine();
            }
        }
        public static async Task<string> CreatePage(IBrowser browser)
        {

            //create page instance
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

            await page.EvaluateExpressionAsync("window.moveTo(0, 0); window.resizeTo(screen.width, screen.height);");

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
            isActive = true;
            await Task.Delay(3000);
            while (isActive)
            {
                try
                {
                    //check if in group or dms
                    var ingroup = await page.XPathAsync("//div[@id='main']//span[contains(@title,'Anda')]");
                    if(ingroup.Length > 0)
                    {
                        isBusy = true;
                        // getgroup
                        if (State != 'G')
                        {
                            Console.WriteLine("Currently on Groups");
                            State = 'G';
                        }
                        await msg.GetGroupMsg(page);
                        isBusy = await msg.Queue(page);
                    }
                    else
                    {
                        isBusy = true;
                        if(State != 'D')
                        {
                            Console.WriteLine("Currently on DMs");
                            State = 'D';
                        }
                        //await msg.GetLatestDM(page);
                        isBusy = false;
                    }
                }
                catch (Exception ex)
                {
                    msg.HandleMsg(ex.Message, page);
                }

            }
            return "";
        }

        //setup
        public async static void GoToChat(IPage page)
        {
            while (true)
            {
                var mycontact = await page.XPathAsync("//span[@title='Test bot']");
                if (mycontact.Length > 0)
                {
                    await mycontact[0].ClickAsync();

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

        public static async Task<string> GetLatest()
        {
            string url = "https://putrartx.my.id/Apps/WhatsApp.ver"; // Replace with your desired URL

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Send a GET request to the URL
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Ensure the request was successful
                    response.EnsureSuccessStatusCode();

                    // Read the response content as a string
                    string responseBody = await response.Content.ReadAsStringAsync();

                    Program.ServerVer = responseBody.Replace("\n","");
                    return responseBody;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    ServerVer = Current;
                }
            }
            return url;
        }


    }
}
