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
using System.Net;

namespace WhatsApp;

public class Messages
{
    public static SystemInf Os = new SystemInf();
    public static string Operating = "";

    [STAThread] // Required for clipboard operations

    public async Task<string> GetLatestDM(IPage page)
    {
        string TimeBak = Program.LastMsgTime;
        await Task.Delay(1000);
        IElementHandle[] newmessage = await page.XPathAsync("/html");

        var newmessage2 = await page.XPathAsync("//div[@role='row']/div[contains(@data-id,'@c.us') and not(contains(@data-id,'6281334149855'))]/div[contains(@class,'message')]/../..");
        if (newmessage2.Length > 0)
        {
            string rawmsg = await newmessage2.Last().EvaluateFunctionAsync<string>("e => e.innerText");
            string[] splitmsg = rawmsg.Split("\n");

            if (splitmsg.Length > 2)
            {
                string USername = splitmsg[0];
                Program.LastMsg = splitmsg[1];
                Program.LastMsgTime = splitmsg[2];
            }
            else if (splitmsg.Length > 1)
            {

                Program.LastMsg = splitmsg[0];
                Program.LastMsgTime = splitmsg[1];
            }
        }
        else
        {
            newmessage = await page.XPathAsync("//div[@class='copyable-text' and not(contains(@data-pre-plain-text,' Rahmad Syaputra'))]//span[@dir]/span");
            if (newmessage.Length > 0)
            {
                Program.LastMsg = await newmessage.Last().EvaluateFunctionAsync<string>("e => e.innerText");
            }
        }

        
        var cek = await page.XPathAsync("//div[@class='copyable-text' and not(contains(@data-pre-plain-text,' Rahmad Syaputra'))]/following-sibling::div");
        if (cek.Length > 0)
        {
            Program.LastMsgTime = await cek.Last().EvaluateFunctionAsync<string>("e => e.innerText");

        }
        string CurrentDay = DateTime.Now.ToString("yyyy-MM-dd");
        if (Program.LastLength < newmessage.Length || Program.LastMsgTime != TimeBak || Program.LastLength2 < newmessage2.Length)
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
        Program.LastLength2 = newmessage2.Length;
        return "";
    }
    




    public async void GetLatest()
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

                Program.LatestVer = responseBody;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }






    public async void HandleMsg(string LastMsg, IPage page)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            Operating = "Windows";
        }
        else
        {
            Operating = "Linux";
        }

        if (LastMsg.IsNotNullOrEmpty())
        {
            if (LastMsg.StartsWith("/"))
            {
                if (LastMsg == "/ping")
                {

                    SendMsg(page, Os.GetSystemInfo() + Os.PingWithoutLib("8.8.8.8"));

                }
                else
                if (Program.LastMsg.StartsWith("/ask "))
                {

                    string prompt = Program.LastMsg.Split("/ask ").Last() + ", Give me very simple Answer that is less than 100 lines please!1";
                    var input = new
                    {
                        prompt = prompt,
                        max_tokens = 500,  // Max tokens for the response
                        temperature = 0.7,  // Controls the randomness of the response
                        top_p = 0.9         // Controls the diversity of the response
                    };

                    var response = await Program.SendRequestToCohere(input);
                    Console.WriteLine(response);
                    // Parse the JSON response
                    JObject responseObject = JObject.Parse(response);

                    // Extract the 'text' field from the 'generations' array
                    string generatedText = responseObject["generations"][0]["text"].ToString();

                    // Output the result
                    Console.WriteLine(generatedText);
                    SendMsg(page, generatedText);
                }
                else if (Program.LastMsg.StartsWith("/about"))
                {
                    SendMsg(page, $"Hi! I’m a software designed for WhatsApp automation {Program.Current}\n" +
                        $"Currently Running on {Operating}\n" +
                        $"I was created using C# on December 15, 2024, by *Putra3340!!*.\n" +
                        $"https://github.com/Putra3340/Whatsapp-Automation" +
                        $"\n\nFeel free to check out /credits for more information!");
                }
                else if (Program.LastMsg.StartsWith("/text2img"))
                {
                    SendMsg(page, "This Features is in Development!!, You can help suggest Generative API..\n Reach out /dev");
                }
                else if (Program.LastMsg.StartsWith("/help"))
                {
                    SendMsg(page, "" +
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
                else if (Program.LastMsg.StartsWith("/ayank"))
                {
                    SendImg(page);
                }
                else if (Program.LastMsg.StartsWith("/whoami"))
                {
                    GetUserProfile(page);
                }
                else if (Program.LastMsg.StartsWith("/amogus"))
                {
                    Amogus(page);
                }
                else
                {
                    SendMsg(page, "The command is not valid, Fuck YOU!");
                }
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
    }


    private async void SendImg(IPage page)
    {
        string imageUrl = "https://i.pinimg.com/736x/e9/61/ca/e961ca42412a80ccef492d9b4f09bbb4.jpg"; // Replace with your image URL

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

    public async static void Amogus(IPage page)
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
    }
}
