using PuppeteerSharp;
using PuppeteerSharp.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsApp;

namespace Whatsapp
{

    public static class Brat
    {
        public static bool Ready = false;
        public static IPage chat;

        public static async Task<bool> Create()
        {
            if (Program.browser == null)
            {
                return false;
            }

            //create a page
            chat = await Program.browser.NewPageAsync();
            //await chat.GoToAsync("https://cdn.botpress.cloud/webchat/v2.2/shareable.html?configUrl=https://files.bpcontent.cloud/2025/01/07/04/20250107040936-52K8CXJ4.json");
            await chat.GoToAsync("https://www.bratgenerator.com/");
            while (true)
            {
                var cekinput = await chat.XPathAsync("//input[@id='textInput']");
                if (cekinput.Length > 0)
                {
                    break;
                }
            }
            Ready = true;
            return true;
        }
        public static async Task<string> AskBot(string askmsg)
        {
            var clearinput = await chat.XPathAsync("//input[@id='textInput']");
            if (clearinput.Length > 0)
            {
                await clearinput[0].FocusAsync();

                // Simulate pressing backspace until the field is empty
                for(int i = 0; i < 100; i++) {
                
                await clearinput[0].PressAsync("Backspace");  // Adjust ClickCount as needed
                }
            }
            var currentDir = Directory.GetCurrentDirectory();
            var imagesDir = Path.Combine(currentDir, "Images/Brat/temp.png");
            await chat.BringToFrontAsync();
            await Task.Delay(2000);
            var cekinput = await chat.XPathAsync("//input[@id='textInput']");
            if (cekinput.Length > 0)
            {
                await cekinput[0].TypeAsync(askmsg);
            }
            await Task.Delay(2000);

            var result = await chat.XPathAsync("//div[@id='textOverlay']");
            if (result.Length > 0)
            {
                await result[0].ScreenshotAsync(imagesDir);
            }
            return imagesDir;
        }
    }
}
