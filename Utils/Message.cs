using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StringExt;
using System.Threading.Tasks;
using SystemInfo;
using RestSharp;
using System.Windows; // Requires PresentationCore assembly
using static System.Net.Mime.MediaTypeNames;
using BotPress;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;

namespace WhatsApp;

public class Messages
{
    public static SystemInf Os = new SystemInf();
    public static string Operating = "";
    public static string Prefix = "/";
#pragma warning disable CS0618 // Type or member is obsolete

    [STAThread] // Required for clipboard operations

    public async Task<string> GetLatestDM(IPage page)
    {
        string TimeBak = Program.LastMsgTime;
        await Task.Delay(1000);
        var    newmessage = await page.XPathAsync("//div[@class='copyable-text' and not(contains(@data-pre-plain-text,' Rahmad Syaputra'))]//span[@dir]/span");
            if (newmessage.Length > 0)
            {
                Program.LastMsg = await newmessage.Last().EvaluateFunctionAsync<string>("e => e.innerText");
            }
        

        
        var cek = await page.XPathAsync("//div[@class='copyable-text' and not(contains(@data-pre-plain-text,' Rahmad Syaputra'))]/following-sibling::div");
        if (cek.Length > 0)
        {
            Program.LastMsgTime = await cek.Last().EvaluateFunctionAsync<string>("e => e.innerText");

        }
        string CurrentDay = DateTime.Now.ToString("yyyy-MM-dd");
        if (Program.LastLength < newmessage.Length || Program.LastMsgTime != TimeBak)
        {
            //skip last message if program just opened
            if (Program.isSkipMsg)
            {
                Program.isSkipMsg = false;
                goto skip;
            }

            // Handle Command here
            Console.WriteLine("New Message Received!!");
            HandleMsg(Program.LastMsg, page);

            if (Program.LastMsg.IsNotNullOrEmpty() && Program.LastMsgTime.IsNotNullOrEmpty())
            {
                Program.LastMsgID = Program.LastMsg + "-" + Program.LastMsgTime + "-" + CurrentDay + "\n";
                System.IO.File.AppendAllText("chat.log", Program.LastMsgID);
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
        Program.LastLength = newmessage.Length;
        return "";
    }


    public async Task<string> GetGroupMsg(IPage page)
    {
        string Username = String.Empty;
        string LastUsername = String.Empty;
        var GroupMsg = await page.XPathAsync("//div[contains(@data-id,'@c.us') and not(contains(@data-id,'6281334149855')) and not(.//img[@alt='Stiker tanpa label'])]");
        if (GroupMsg.Length > 0)
        {
            // make foreach loop to get msg id and save to queue
            foreach (var msg in GroupMsg)
            {
                // initial
                string CurrentMsg = await msg.EvaluateFunctionAsync<string>("e => e.innerText");
                string MsgId = await msg.EvaluateFunctionAsync<string>("e => e.getAttribute('data-id')");

                string MsgHash = MsgId.Split("@g.us_").Last().Split("_").First();

                string[] splitmsg = CurrentMsg.Split("\n");

                if (splitmsg.Length > 2)
                {
                    Username = splitmsg[0];
                    if(Username.Length == 1)
                    {
                        Username = splitmsg[1];
                    }
                    Program.LastMsg = splitmsg[splitmsg.Length - 2];
                    Program.LastMsgTime = splitmsg.Last();
                }
                else if (splitmsg.Length > 1)
                {
                    Username = LastUsername;
                    Program.LastMsg = splitmsg[0];
                    Program.LastMsgTime = splitmsg[1];
                }
                if (MsgId.Contains("6285710786509"))
                {
                    Username = "Admin";
                }
                if (!Username.IsNotNullOrEmpty())
                {
                    Username = "Unknown";
                }

                // dynamic data save
                // Construct the log entry
                string logEntry = $"{Username}ඞ{Program.LastMsg}ඞ{Program.LastMsgTime}ඞ{EpochUtils.GetCurrentEpochMillis()}ඞ{MsgHash}";

                // Read all lines from the file to avoid duplicates
                if (!File.Exists(Program.ChatLogs))
                {
                    // Create the file if it doesn't exist
                    File.WriteAllText(Program.ChatLogs, logEntry + "\n");
                }
                else
                {
                    var fileContent = File.ReadAllText(Program.ChatLogs);

                    // Check if the MsgHash already exists
                    if (!fileContent.Contains(MsgHash))
                    {
                        // Append the log entry if MsgHash is not found
                        File.AppendAllText(Program.ChatLogs, logEntry + "\n");
                    }
                }

            }

        }
        // do queue process


        Program.LastLength2 = GroupMsg.Length;

        return "";
    }
    public async Task<bool> Queue(IPage page)
    {
        await Task.Delay(1000);
        foreach (string loghash in File.ReadLines(Program.ChatLogs))
        {
            string Executed = System.IO.File.ReadAllText(Program.QueueLogs);
            if (!Executed.Contains(loghash.Split("ඞ").Last()))
            {
                // Construct the log entry
                string logEntry = loghash.Split("ඞ").Last();
                Console.WriteLine($"{new string('=', 100)}\nProcessing..\n{logEntry} => {loghash.Split("ඞ")[1]}");

                // Read all lines from the file to avoid duplicates
                if (!File.Exists(Program.QueueLogs))
                {
                    // Create the file if it doesn't exist
                    File.WriteAllText(Program.QueueLogs, logEntry + "\n");
                }
                else
                {
                    var fileContent = File.ReadAllText(Program.QueueLogs);
                    bool isadmin = false;
                    
                    // Check if the MsgHash already exists
                    if (!fileContent.Contains(logEntry))
                    {
                        if(loghash.Split("ඞ").First() == "Admin")
                        {
                            isadmin = true;
                        }
                        await HandleMsg(loghash.Split("ඞ")[1],page,isadmin);
                        // Append the log entry if MsgHash is not found
                        File.AppendAllText(Program.QueueLogs, logEntry + "\n");
                        Console.WriteLine($"Done => {logEntry}\n{new string('=', 100)}\n\n");
                    }
                }
            }
        }

        return false;
    }

    public async Task<string> HandleMsg(string LastMsg, IPage page,bool isAdmin = false)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            Operating = "Windows";
        }
        else
        {
            Operating = "Linux";
        }
        if (Program.ReadMode)
        {
            return "";
        }
        if (Program.BotPressMode)
        {

            if (LastMsg.IsNotNullOrEmpty())
            {
                //disable botpress mode
                if (LastMsg.StartsWith(Prefix))
                {
                    if (LastMsg.StartsWith($"{Prefix}botpress off") && isAdmin)
                    {
                        Program.BotPressMode = false;
                    }
                    
                }
                else
                {
                    //ask to bot
                    await SendMsg(page, await Bot.AskBot(LastMsg));

                }
            }
        }
        if (Program.BotHaluMode)
        {
            
            if (LastMsg.IsNotNullOrEmpty())
            {
                //disable botpress mode
                if (LastMsg.StartsWith(Prefix))
                {
                    if (LastMsg.StartsWith($"{Prefix}halu off") && isAdmin)
                    {
                        Program.BotHaluMode = false;
                    }

                }
                else
                {
                    //ask to bot
                    await SendMsg(page, await BotCharAi.AskBot(LastMsg));

                }
            }
        }
        if (LastMsg.IsNotNullOrEmpty())
        {
            if (LastMsg.StartsWith(Prefix))
            {
                if (LastMsg.StartsWith("ping".AddPrefix()))
                {
                    await SendMsg(page, Os.GetSystemInfo() + Os.PingWithoutLib(LastMsg.Split("/ping ").Last()));
                }
                else
                if (LastMsg.StartsWith("ask ".AddPrefix()))
                {
                    string prompt = LastMsg.Split("ask ".AddPrefix()).Last() + ", Give me very simple Answer that is less than 100 lines please!1";
                    var input = new
                    {
                        prompt = prompt,
                        max_tokens = 500,  // Max tokens for the response
                        temperature = 0.7,  // Controls the randomness of the response
                        top_p = 0.9         // Controls the diversity of the response
                    };

                    var response = await Program.SendRequestToCohere(input);
                    Console.WriteLine(response);
                    JObject responseObject = JObject.Parse(response);
                    string generatedText = responseObject["generations"][0]["text"].ToString();
                    Console.WriteLine(generatedText);
                    await SendMsg(page, generatedText);
                }
                else if (LastMsg.StartsWith("about".AddPrefix()))
                {
                    await SendMsg(page, $"Hi! I’m a software designed for WhatsApp automation {Program.Current}\n" +
                        $"Currently Running on {Operating}\n" +
                        $"I was created using C# on December 15, 2024, by *Putra3340!!*.\n" +
                        $"https://github.com/Putra3340/Whatsapp-Automation" +
                        $"\n\nFeel free to check out /credits for more information!");
                }
                else if (LastMsg.StartsWith("help".AddPrefix()))
                {
                    await SendMsg(page, $"{Program.Current}" +
                        "List All Available Commands\n" +
                        "/about\n" +
                        "/ask\n" +
                        "/ping\n" +
                        "" +
                        "" +
                        "" +
                        "" +
                        "" +
                        "" +
                        "" +
                        "" +
                        "" +
                        "" +
                        "" +
                        "" +
                        "" +
                        "");
                }
                else if (LastMsg.StartsWith("amogus".AddPrefix()))
                {
                    await Amogus(page);
                }
                else if (LastMsg.StartsWith("botpress on".AddPrefix()) && isAdmin)
                {
                    if (!Bot.Ready)
                    {
                        await SendMsg(page, "Plugins is not enabled!\nContact @085710786509");
                        Program.BotPressMode = false;
                        return "";
                    }
                    else
                    {
                        await SendMsg(page, "Botpress Mode Started!");
                        Program.BotPressMode = true;
                    }
                }
                else if (LastMsg.StartsWith("halu on".AddPrefix()) && isAdmin)
                {
                    if (!BotCharAi.Ready)
                    {
                        await SendMsg(page, "Plugins is not enabled!\nContact @085710786509");
                        Program.BotHaluMode = false;
                        return "";
                    }
                    else
                    {
                        Program.BotHaluMode = true;
                        await SendMsg(page, "Halu Mode Started!");
                    }
                }
                else if(LastMsg.StartsWith("kill".AddPrefix()) && isAdmin)
                {
                    Program.isMuted = true;
                    await SendMsg(page, "RIP");
                }
                else if(LastMsg.StartsWith("start".AddPrefix()))
                {
                    if (isAdmin)
                    {
                        Program.isMuted = false;
                        await SendMsg(page, "Bot Activated!");
                    }
                    
                }
                else if (LastMsg.StartsWith("setprefix".AddPrefix()) && isAdmin)
                {
                    Prefix = LastMsg.Split(" ").Last();
                    Console.WriteLine("Current Prefix is" + Prefix);
                }
                else if (LastMsg.StartsWith("debug".AddPrefix()) && isAdmin)
                {
                    await SendMsg(page,GetAllVariablesAsString());
                }else if (LastMsg.StartsWith("admincheck".AddPrefix()) && isAdmin)
                {
                    await SendMsg(page,"You are Admin yey");
                }else if(LastMsg.StartsWith("admincheck".AddPrefix()))
                {
                    await SendMsg(page, "You are not Admin fuck u");
                }else if(LastMsg.StartsWith("reset".AddPrefix()) && isAdmin)
                {
                    await SendMsg(page, "Resetting all configuration");
                    Program.isActive = true;
                    Program.isSkipMsg = true;
                    Program.isBusy = false;
                    Program.isMuted = false;
                    Program.BotPressMode = false;
                    Program.BotHaluMode = false;
                    Program.FormData = "";
                    Program.ChatLogs = "chat.log";
                    Program.QueueLogs = "queue.log";
                    Prefix = "/";
                    await HandleMsg("/debug", page, isAdmin);
                }
                else if (LastMsg.StartsWith("test".AddPrefix()))
                {
                    //await SendImg(page, "");
                    await SendImg(page, LastMsg.GetArgs());
                }
                else if (LastMsg.StartsWith("gaycheck".AddPrefix()))
                {
                    // Create a Random instance
                    Random random = new Random();

                    // Generate a random true/false value
                    bool result = random.Next(2) == 0;

                    // Output the result
                    Console.WriteLine($"Random result: {result}");

                    if (result)
                    {
                        await SendMsg(page, "You are Gay, why are you ge?");
                    }
                    else
                    {
                        await SendMsg(page, "You are normal sir");

                    }
                }
            }

        }
        return "";
    }
    public async Task<string> SendMsg(IPage page, string msg = "Error: Unhandled Message!\n")
    {
        Console.WriteLine($"Sending => {msg}");
        if (Program.isMuted)
        {
            Console.WriteLine("Bot is Muted!!");
            return "";
        }
        await page.BringToFrontAsync();
        await Task.Delay(1000);
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
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
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
        return "";
    }


    private async Task<string> SendImg(IPage page, string path)
    {
        string UploadPath = path;

        // Check Path is it from url?
        if (path.IsNotNullOrEmpty())
        {
            if (path.StartsWith("http"))
            {
                UploadPath = await DownloadImg(path);
            }
        }
        else
        {
            return "";
        }
        Console.WriteLine($"Sending => {UploadPath}");
        if (Program.isMuted)
        {
            Console.WriteLine("Bot is Muted!!");
            return "";
        }
        await page.BringToFrontAsync();
        await Task.Delay(1000);


        while (true)
        {
            var plus = await page.XPathAsync("//span[@data-icon='plus']");
            if(plus.Length > 0)
            {
                await plus[0].ClickAsync();
                await Task.Delay(500);
                break;
            }
        }
        while (true)
        {
            var inputimg = await page.XPathAsync("//input[contains(@accept,'video') and @type='file']");
            if(inputimg.Length > 0)
            {
                await inputimg[0].UploadFileAsync(@$"{UploadPath}");
            }
            break;
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
        return "";
    }

    private async Task<string> DownloadImg(string url)
    {
        string localFilePath = "";
        if (url.IsNotNullOrEmpty())
        {
            var currentDir = Directory.GetCurrentDirectory();
            var imagesDir = Path.Combine(currentDir, "Images");
            if (!Directory.Exists(imagesDir))
            {
                Directory.CreateDirectory(imagesDir);
                Console.WriteLine($"Created directory: {imagesDir}");
            }

            // Path to save the downloaded image
            localFilePath = Path.Combine(imagesDir, $"image-{EpochUtils.GetCurrentEpoch()}.png");
            Console.WriteLine("Downloading...  " + url);
            // Download the image
            using (var httpClient = new HttpClient())
            {
                var imageBytes = await httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(localFilePath, imageBytes);
                Console.WriteLine($"Image downloaded and saved to {localFilePath}");
            }
        }
        return localFilePath;
    }

    private async void GetUserProfile(IPage page)
    {
        string Profil = "";
        await Task.Delay(2000);
        while (true)
        {
            var perofil = await page.XPathAsync("//header/div[@title='Detail Profil']");
            if (perofil.Length > 0)
            {
                await perofil[0].ClickAsync();
                break;
            }

        }
        await Task.Delay(2000);
        while (true)
        {
            var namakontak = await page.XPathAsync("//h2");
            if (namakontak.Length > 0)
            {
                Profil = "Name : " + await namakontak[0].EvaluateFunctionAsync<string>("e => e.innerText");
                break;
            }

        }
        while (true)
        {
            var nomer = await page.XPathAsync("//h2/following-sibling::div/span");
            if (nomer.Length > 0)
            {
                Profil += "\nPhone Number : " + await nomer[0].EvaluateFunctionAsync<string>("e => e.innerText");
                break;
            }
        }

        while (true)
        {
            var inpo = await page.XPathAsync("//span[contains(text(),'Info')]/../../../following-sibling::span");
            if (inpo.Length > 0)
            {
                Profil += "\n Bio : " + await inpo[0].EvaluateFunctionAsync<string>("e => e.innerText");
                break;
            }
        }
        SendMsg(page, Profil);
    }

    public async static Task<bool> Amogus(IPage page)
    {
        while (true)
        {
            var stikbutton = await page.XPathAsync("//span[@data-icon='expressions']/..");
            if (stikbutton.Length > 0)
            {
                await stikbutton[0].ClickAsync();
                break;
            }
        }
        while (true)
        {
            var klikamogus = await page.XPathAsync("//span[contains(text(),'Buat')]/../../../..//../following-sibling::div//img/../..");
            if (klikamogus.Length > 0)
            {
                await klikamogus[0].ClickAsync();
                break;
            }
        }
        while (true)
        {
            var stikbutton = await page.XPathAsync("//span[@data-icon='expressions']/..");
            if (stikbutton.Length > 0)
            {
                await stikbutton[0].ClickAsync();
                break;
            }
        }
        return true;
    }

    public static string GetAllVariablesAsString()
    {
        return $"Program.isActive: {Program.isActive}\n" +
               $"Program.isSkipMsg: {Program.isSkipMsg}\n" +
               $"Program.isBusy: {Program.isBusy}\n" +
               $"Program.isMuted: {Program.isMuted}\n" +
               $"Program.BotPressMode: {Program.BotPressMode}\n" +
               $"Program.BotHaluMode: {Program.BotHaluMode}\n" +
               $"Program.FormData: \"{Program.FormData}\"\n" +
               $"Program.ChatLogs: \"{Program.ChatLogs}\"\n" +
               $"Program.QueueLogs: \"{Program.QueueLogs}\"\n" +
               $"Program.LastMsg: \"{Program.LastMsg}\"\n" +
               $"Program.LastMsgTime: \"{Program.LastMsgTime}\"\n" +
               $"Program.LastMsgID: \"{Program.LastMsgID}\"\n" +
               $"Program.MaxDuplicateMsgCount: {Program.MaxDuplicateMsgCount}\n" +
               $"Program.State: {Program.State}\n" +
               $"Program.LastLength: {Program.LastLength}\n" +
               $"Program.LastLength2: {Program.LastLength2}\n" +
               $"Program.ServerVer: \"{Program.ServerVer}\"\n" +
               $"Program.Current: \"{Program.Current}\"\n" +
               $"Prefix: \"{Prefix}\"\n" +
               $"Os: \"{Os}\"\n";
               
               
               ;
    }



}
