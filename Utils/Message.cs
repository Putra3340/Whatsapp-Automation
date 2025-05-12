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
using Whatsapp;
using PuppeteerExt;

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
        var    newmessage = await Program.WhatsappPage.XPathAsync("//div[@class='copyable-text' and not(contains(@data-pre-plain-text,' Rahmad Syaputra'))]//span[@dir]/span");
            if (newmessage.Length > 0)
            {
                Program.LastMsg = await newmessage.Last().EvaluateFunctionAsync<string>("e => e.innerText");
            }
        

        
        var cek = await Program.WhatsappPage.XPathAsync("//div[@class='copyable-text' and not(contains(@data-pre-plain-text,' Rahmad Syaputra'))]/following-sibling::div");
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
            await HandleMsg(Program.LastMsg);

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


    public async Task<string> GetGroupMsg()
    {
        Program.LastMethod = "GetGroupMsg";
        string Username = String.Empty;
        string LastUsername = String.Empty;
        var GroupMsg = await Program.WhatsappPage.XPathAsync("//div[contains(@data-id,'@c.us') and not(contains(@data-id,'6281334149855')) and not(.//img[@alt='Stiker tanpa label'])]");
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
    public async Task<bool> Queue()
    {
        Program.LastMethod = "Queue";
        if(!Program.isProcesingQueue)
        {
            return false;
        }
        await Task.Delay(500);
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
                        Program.LastMsgID = logEntry;
                        await HandleMsg(loghash.Split("ඞ")[1],isadmin,logEntry);
                        // Append the log entry if MsgHash is not found
                        File.AppendAllText(Program.QueueLogs, logEntry + "\n");
                        Console.WriteLine($"Done => {logEntry}\n{new string('=', 100)}\n\n");
                    }
                }
            }
        }

        return false;
    }

    public async Task<string> HandleMsg(string LastMsg,bool isAdmin = false, string msghash = "")
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
                    await SendMsg(await Bot.AskBot(LastMsg));

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
                    await SendMsg(await BotCharAi.AskBot(LastMsg));

                }
            }
        }
        if (LastMsg.IsNotNullOrEmpty())
        {
            if (true)
            {
                if (LastMsg.StartsWith("ping".AddPrefix()))
                {
                    await SendMsg(Os.GetSystemInfo() + Os.PingWithoutLib(LastMsg.Split("/ping ").Last()));
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
                    await SendMsg(generatedText);
                }
                else if (LastMsg.StartsWith("about".AddPrefix()))
                {
                    await SendMsg($"Hi! I’m a software designed for WhatsApp automation {Program.CurrentVersion}\n" +
                        $"Currently Running on {Operating}\n" +
                        $"I was created using C# on December 15, 2024, by *Putra3340!!*.\n" +
                        $"https://github.com/Putra3340/Whatsapp-Automation" +
                        $"\n\nFeel free to check out /credits for more information!");
                }
                else if (LastMsg.StartsWith("help".AddPrefix()))
                {
                    await SendMsg($"{Program.CurrentVersion}" +
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
                    await Amogus(Program.WhatsappPage);
                }
                else if (LastMsg.StartsWith("botpress on".AddPrefix()) && isAdmin)
                {
                    if (!Bot.Ready)
                    {
                        await SendMsg("Plugins is not enabled!\nContact @085710786509");
                        Program.BotPressMode = false;
                        return "";
                    }
                    else
                    {
                        await SendMsg("Botpress Mode Started!");
                        Program.BotPressMode = true;
                    }
                }
                else if (LastMsg.StartsWith("halu on".AddPrefix()) && isAdmin)
                {
                    if (!BotCharAi.Ready)
                    {
                        await SendMsg("Plugins is not enabled!\nContact @085710786509");
                        Program.BotHaluMode = false;
                        return "";
                    }
                    else
                    {
                        Program.BotHaluMode = true;
                        await SendMsg("Halu Mode Started!");
                    }
                }
                else if(LastMsg.StartsWith("kill".AddPrefix()) && isAdmin)
                {
                    Program.isMuted = true;
                    await SendMsg("RIP");
                }
                else if(LastMsg.StartsWith("start".AddPrefix()))
                {
                    if (isAdmin)
                    {
                        Program.isMuted = false;
                        await SendMsg("Bot Activated!");
                    }
                    
                }
                else if (LastMsg.StartsWith("setprefix".AddPrefix()) && isAdmin)
                {
                    Prefix = LastMsg.Split(" ").Last();
                    Console.WriteLine("Current Prefix is" + Prefix);
                }
                else if (LastMsg.StartsWith("debug".AddPrefix()) && isAdmin)
                {
                    await SendMsg(GetAllVariablesAsString());
                }else if (LastMsg.StartsWith("admincheck".AddPrefix()) && isAdmin)
                {
                    await SendMsg("You are Admin yey");
                }else if(LastMsg.StartsWith("admincheck".AddPrefix()))
                {
                    await SendMsg("You are not Admin fuck u");
                }else if(LastMsg.StartsWith("reset".AddPrefix()) && isAdmin)
                {
                    await SendMsg("Resetting all configuration");
                    Program.isActive = true;
                    Program.isSkipMsg = true;
                    Program.isBusy = false;
                    Program.isMuted = false;
                    Program.BotPressMode = false;
                    Program.BotHaluMode = false;
                    Program.ChatLogs = "chat.log";
                    Program.QueueLogs = "queue.log";
                    Prefix = "/";
                    await HandleMsg("/debug", isAdmin);
                }
                else if (LastMsg.StartsWith("test".AddPrefix()))
                {
                    //await SendImg(page, "");
                    await SendMsg("test",msghash);
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
                        await SendMsg("You are Gay, why are you ge?");
                    }
                    else
                    {
                        await SendMsg("You are normal sir");

                    }
                }
                else if (LastMsg.Contains("niga"))
                {
                    await SendMsg("Rasis dontol",msghash);
                }
                else if (LastMsg.StartsWith("brat".AddPrefix()))
                {
                    await CreateSticker(await Brat.AskBot(LastMsg.GetArgs()),msghash);
                }
                else if (LastMsg.StartsWith("sticker".AddPrefix()))
                {
                    await CreateSticker(await GetImageAttachmentLink(Program.WhatsappPage), msghash);
                }
            }

        }
        return "";
    }
    public async Task<string> SendMsg(string msg = "Error: Unhandled Message!\n",string msghash = "")
    {
        
        Console.WriteLine($"Sending => {msg}");
        if (Program.isMuted)
        {
            Console.WriteLine("Bot is Muted!!");
            return "";
        }
        await Program.WhatsappPage.BringToFrontAsync();
        await Task.Delay(1000);
        if (msghash.IsNotNullOrEmpty())
        {
            await ReplyMessage(msghash);
        }
        while (true)
        {
            var input = await Program.WhatsappPage.XPathAsync("//div[@aria-placeholder='Ketik pesan']");
            if (input.Length > 0)
            {
                await PuppeteerExtensions.FastInput(Program.WhatsappPage,input, msg);
                break;
            }

        }

        await Task.Delay(1000);
        while (true)
        {
            var send = await Program.WhatsappPage.XPathAsync("//span[@data-icon='send']/..");
            if (send.Length > 0)
            {
                await send[0].ClickAsync();
                break;
            }
        }
        return "";
    }


    private async Task<string> SendImg(string path)
    {
        Program.LastMethod = "SendImg";
        string UploadPath = path;

        // Check Path is it from url?
        if (path.IsNotNullOrEmpty())
        {
            if (path.StartsWith("http"))
            {
                UploadPath = await DownloadImg(Program.WhatsappPage,path);
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
        await Program.WhatsappPage.BringToFrontAsync();
        await Task.Delay(1000);


        while (true)
        {
            var plus = await Program.WhatsappPage.XPathAsync("//span[@data-icon='plus']");
            if(plus.Length > 0)
            {
                await plus[0].ClickAsync();
                await Task.Delay(500);
                break;
            }
        }
        while (true)
        {
            var inputimg = await Program.WhatsappPage.XPathAsync("//input[contains(@accept,'video') and @type='file']");
            if(inputimg.Length > 0)
            {
                await inputimg[0].UploadFileAsync(@$"{UploadPath}");
            }
            break;
        }
        await Task.Delay(1000);
        while (true)
        {
            var send = await Program.WhatsappPage.XPathAsync("//span[@data-icon='send']/..");
            if (send.Length > 0)
            {
                await send[0].ClickAsync();
                break;
            }
        }
        return "";
    }
    private async Task<string> DownloadImg(IPage page, string url)
    {
        Program.LastMethod = "DownloadImg";
        string localFilePath = "";

        if (!string.IsNullOrEmpty(url))
        {
            string imagesDir = Program.DefaultImagePath;
            localFilePath = Path.Combine(imagesDir, $"image-{EpochUtils.GetCurrentEpoch()}.png");

            if (url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Processing data URL...");
                // Extract base64 data
                var base64Data = url.Substring(url.IndexOf(",") + 1);
                var imageBytes = Convert.FromBase64String(base64Data);
                await File.WriteAllBytesAsync(localFilePath, imageBytes);
                Console.WriteLine($"Image processed and saved to {localFilePath}");
            }
            else if (url.StartsWith("D:"))
            {
                return url; // Local path provided directly
            }
            else if (url.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Processing blob URL...");
                // JavaScript to fetch and convert Blob data to a base64 string
                var jsCode = $@"
                (async () => {{
                    const response = await fetch('{url}');
                    const buffer = await response.arrayBuffer();
                    const base64String = btoa(
                        String.fromCharCode(...new Uint8Array(buffer))
                    );
                    return base64String;
                }})()";

                // Evaluate JavaScript to get the base64 string
                var base64Data = await page.EvaluateExpressionAsync<string>(jsCode);

                // Decode the base64 string into a byte array
                var imageBytes = Convert.FromBase64String(base64Data);
                await File.WriteAllBytesAsync(localFilePath, imageBytes);
                Console.WriteLine($"Blob data processed and saved to {localFilePath}");
            }
            else
            {
                Console.WriteLine("Downloading... " + url);
                // Download the image from a URL
                using (var httpClient = new HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(localFilePath, imageBytes);
                    Console.WriteLine($"Image downloaded and saved to {localFilePath}");
                }
            }
        }

        return localFilePath;
    }




    private async Task<string> CreateSticker(string path,string msghash)
    {
        Program.LastMethod = "CreateSticker";
        string UploadPath = path;
        UploadPath = await DownloadImg(Program.WhatsappPage, path);

        Console.WriteLine($"Sending => {UploadPath}");
        if (Program.isMuted)
        {
            Console.WriteLine("Bot is Muted!!");
            return "";
        }
        await Program.WhatsappPage.BringToFrontAsync();
        await Task.Delay(1000);
        if (msghash.IsNotNullOrEmpty())
        {
            await ReplyMessage(msghash);
        }

        while (true)
        {
            var plus = await Program.WhatsappPage.XPathAsync("//span[@data-icon='plus']");
            if (plus.Length > 0)
            {
                await plus[0].ClickAsync();
                await Task.Delay(500);
                break;
            }
        }
        while (true)
        {
            var inputimg = await Program.WhatsappPage.XPathAsync("//span[text()='Stiker baru']/following-sibling::input");
            if (inputimg.Length > 0)
            {
                await inputimg[0].UploadFileAsync(@$"{UploadPath}");
            }
            break;
        }
        await Task.Delay(1000);
        while (true)
        {
            var send = await Program.WhatsappPage.XPathAsync("//span[@data-icon='send']/..");
            if (send.Length > 0)
            {
                await send[0].ClickAsync();
                break;
            }
        }
        return "";
    }
    private async static Task ReplyMessage(string msghash)
    {
        var msgbubble = await Program.WhatsappPage.FindElementSafeAsync($"//div[contains(@data-id,'{msghash}') and not(contains(@data-id,'6281334149855')) and not(.//img[@alt='Stiker tanpa label'])]//div[@data-pre-plain-text]");
        if(msgbubble != null)
        {
            msgbubble.HoldHoverFor(1000);
            await Task.Delay(100);
            var dropdown = await msgbubble.XPathAsync(".//span[@data-icon='down-context']");
            if(dropdown.Length > 0)
            {
                await dropdown[0].ClickAsync();
            }
            await Task.Delay(100);
            var reply = await Program.WhatsappPage.FindElementsSafeAsync("//div[@aria-label='Balas']");
            if (reply.Length > 0)
            {
                await reply[0].ClickAsync();
            }
        }

    
    }

    public async static Task<bool> Amogus(IPage page)
    {
        while (true)
        {
            var stikbutton = await Program.WhatsappPage.XPathAsync("//span[@data-icon='expressions']/..");
            if (stikbutton.Length > 0)
            {
                await stikbutton[0].ClickAsync();
                break;
            }
        }
        while (true)
        {
            var klikamogus = await Program.WhatsappPage.XPathAsync("//span[contains(text(),'Buat')]/../../../..//../following-sibling::div//img/../..");
            if (klikamogus.Length > 0)
            {
                await klikamogus[0].ClickAsync();
                break;
            }
        }
        while (true)
        {
            var stikbutton = await Program.WhatsappPage.XPathAsync("//span[@data-icon='expressions']/..");
            if (stikbutton.Length > 0)
            {
                await stikbutton[0].ClickAsync();
                break;
            }
        }
        return true;
    }

    public static async Task<string> GetImageAttachmentLink(IPage page)
    {
        Program.LastMethod = "GetImageAttachmentLink";
            var imagelink = await Program.WhatsappPage.XPathAsync($"//div[contains(@data-id,'{Program.LastMsgID}')]//div[not(@aria-label)]/img");
            if (imagelink.Length > 0)
            {
                //var localFilePath = Path.Combine(imagesDir, $"image-{EpochUtils.GetCurrentEpoch()}.png");
                //await imagelink.Last().ScreenshotAsync(localFilePath);
                //return localFilePath;
                return await imagelink.Last().EvaluateFunctionAsync<string>("e => e.src");
            }

            return "https://media.tenor.com/x8v1oNUOmg4AAAAM/rickroll-roll.gif";
    }

    public static string GetAllVariablesAsString()
    {
        return $"Program.isActive: {Program.isActive}\n" +
               $"Program.isSkipMsg: {Program.isSkipMsg}\n" +
               $"Program.isBusy: {Program.isBusy}\n" +
               $"Program.isMuted: {Program.isMuted}\n" +
               $"Program.BotPressMode: {Program.BotPressMode}\n" +
               $"Program.BotHaluMode: {Program.BotHaluMode}\n" +
               $"Program.FormData: \"{Program.LastMethod}\"\n" +
               $"Program.ChatLogs: \"{Program.ChatLogs}\"\n" +
               $"Program.QueueLogs: \"{Program.QueueLogs}\"\n" +
               $"Program.LastMsg: \"{Program.LastMsg}\"\n" +
               $"Program.LastMsgTime: \"{Program.LastMsgTime}\"\n" +
               $"Program.LastMsgID: \"{Program.LastMsgID}\"\n" +
               $"Program.MaxDuplicateMsgCount: {Program.MaxDuplicateMsgCount}\n" +
               $"Program.State: {Program.State}\n" +
               $"Program.LastLength: {Program.LastLength}\n" +
               $"Program.LastLength2: {Program.LastLength2}\n" +
               $"Program.Current: \"{Program.CurrentVersion}\"\n" +
               $"Prefix: \"{Prefix}\"\n" +
               $"Os: \"{Os}\"\n";
               
               
               ;
    }



}
