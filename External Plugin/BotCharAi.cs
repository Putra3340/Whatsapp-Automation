using PuppeteerExt;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsApp;

namespace BotPress
{
    public static class BotCharAi
    {
#pragma warning disable CS0618 // Type or member is obsolete

        public static bool Ready = false;

        public static IPage chat;

        public static async Task<bool> Create()
        {
            if(Program.browser == null)
            {
                return false;
            }

            //create a page
            chat = await Program.browser.NewPageAsync();
            //await chat.GoToAsync("https://cdn.botpress.cloud/webchat/v2.2/shareable.html?configUrl=https://files.bpcontent.cloud/2025/01/07/04/20250107040936-52K8CXJ4.json");
            await chat.GoToAsync("https://character.ai/chat/kfo-xqFBH6feYwWo9ar8q3grQKqGXzC-83a7ySyHpec");
            while (true)
            {
                var cekinput = await chat.XPathAsync("//textarea[contains(@placeholder,'Elysia Wife...')]");
                if(cekinput.Length > 0)
                {
                    break;
                }
            }
            Ready = true;
            return true;
        }
        public static async Task<string> AskBot(string askmsg)
        {
            Program.LastMethod = "BotCharAi.cs AskBot()";
            await chat.BringToFrontAsync();
            await Task.Delay(2000);
            var cekinput = await chat.WaitElementsSafeAsync("//textarea[contains(@placeholder,'Elysia Wife...')]");
            if (cekinput.Length > 0)
            {
                await cekinput[0].TypeAsync(askmsg);
            }

            var send = await chat.FindElementsSafeAsync("//button[contains(@aria-label,'Send a message')]");
            if (send.Length > 0)
            {
                await send[0].ClickAsync();
            }
            Console.WriteLine("wait");
            await Task.Delay(3000);

            while (true)
            {
                var newmsg = await chat.WaitElementsSafeAsync("//div[@data-testid='completed-message']");
                if (newmsg.Length > 0)
                {
                    //return await newmsg.First().EvaluateFunctionAsync<string>("e => e.innerText");
                    var element = newmsg.First();
                    string previousText = " ";
                    string currentText = "";

                    // Wait for the innerText to stabilize
                    do
                    {
                        previousText = currentText;
                        currentText = await element.EvaluateFunctionAsync<string>("e => e.innerText");
                        await Task.Delay(300); // Wait for a short duration before rechecking
                    } while (currentText != previousText);

                    return currentText; // Return the final, stabilized innerText
                }
            }
            
            return "";
        }
    }
}
