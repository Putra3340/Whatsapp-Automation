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
        public static string FormData = "";
        public static string ChatLogs = "chat.log";

        public static string LastMsg = String.Empty;
        public static string LastMsgTime = String.Empty;
        public static string LastMsgID = string.Empty;
        public static int MaxDuplicateMsgCount = 3;
        public static int LastLength = 100;

        [STAThread] // Required for clipboard operations
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
            isActive = true;
            while (isActive)
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
                        //skip last message if program just opened
                        if (isSkipMsg)
                        {
                            isSkipMsg = false;
                            goto skip;
                        }

                        // Handle Command here
                        Console.WriteLine("New Message Received!!");
                        msg.HandleMsg(LastMsg, page);

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
                    skip:
                    LastLength = newmessage.Length;
                }
                catch (Exception ex)
                {
                    msg.HandleMsg(ex.Message, page);
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

    }
}
