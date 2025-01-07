using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotPress
{
    public static class Bot
    {
        public static bool Good = false;
        public static IPage chat;

        public static async Task<bool> Create(IBrowser browser)
        {
            if(browser == null)
            {
                return false;
            }

            //create a page
            chat = await browser.NewPageAsync();
            //await chat.GoToAsync("https://cdn.botpress.cloud/webchat/v2.2/shareable.html?configUrl=https://files.bpcontent.cloud/2025/01/07/04/20250107040936-52K8CXJ4.json");
            await chat.GoToAsync("https://cdn.botpress.cloud/webchat/v2.2/shareable.html?configUrl=https://files.bpcontent.cloud/2025/01/07/04/20250107040936-52K8CXJ4.json");
            while (true)
            {
                var cekinput = await chat.XPathAsync("//textarea[@placeholder='Type your message...']");
                if(cekinput.Length > 0)
                {
                    break;
                }
            }
            return true;
        }
        public static async Task<string> AskBot(string askmsg)
        {
            await chat.BringToFrontAsync();
            await Task.Delay(2000);
            var cekinput = await chat.XPathAsync("//textarea[@placeholder='Type your message...']");
            if (cekinput.Length > 0)
            {
                await cekinput[0].TypeAsync(askmsg);
            }

            var send = await chat.XPathAsync("//button[@aria-label='Send message']");
            if (send.Length > 0)
            {
                await send[0].ClickAsync();
            }
            Console.WriteLine("wait");
            await Task.Delay(2000);
            while (true)
            {
                var loading = await chat.XPathAsync("//div[contains(@class,'TypingIndicatorContainer')]");
                if(loading.Length == 0)
                {
                    break;
                    
                }
                await Task.Delay(20);
            }
            
            var newmsg = await chat.XPathAsync("//div[@messageid]");
            if(newmsg.Length > 0)
            {
                return await newmsg.Last().EvaluateFunctionAsync<string>("e => e.innerText");
            }
            return "";
        }
    }
}
