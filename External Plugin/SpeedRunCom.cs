using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsApp;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Whatsapp
{
    public static class SpeedrunApi
    {
        public static bool Ready = false;
        public static IPage page = null;

        public async static Task<bool> Setup()
        {
            if (Program.browser == null)
            {
                return false;
            }

            //create a page
            page = await Program.browser.NewPageAsync();
            while (true)
            {
                var cekinput = await page.XPathAsync("//textarea[contains(@placeholder,'Elysia Wife...')]");
                if (cekinput.Length > 0)
                {
                    break;
                }
            }
            Ready = true;
            return true;
        }
    }
}
