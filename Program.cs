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
        public static SystemInf Os = new SystemInf();
        public static bool work = false;
        public static string FormData = "";
        public static async Task Main(string[] args)
        {
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
                    var lines = msg.Split(new[] { "\n" }, StringSplitOptions.None);

                    foreach (var line in lines)
                    {
                        // Type the line in the input field
                        await input[0].TypeAsync(line);

                        // Simulate Shift + Enter (to create a line break without submitting)
                        await page.Keyboard.DownAsync("Shift");
                        await page.Keyboard.PressAsync("Enter");
                        await page.Keyboard.UpAsync("Shift");
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

    }
}
