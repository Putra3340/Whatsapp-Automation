using DotNetEnv;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TextCopy;

namespace PuppeteerExt
{
    public static class PuppeteerExtensions
    {

        public static async Task<IElementHandle> WaitElementSafeAsync(this IPage page, string query, bool isUseXPath = true, int timeInMs = 30000)
        {
            IElementHandle element = null;
        redo:
            try
            {
                for (int i = 0; i < timeInMs / 100; i++)
                {
                    element = await FindElementSafeAsync(page, query, isUseXPath);
                    if (element != null)
                    {
                        return element;
                    }

                    await Task.Delay(100);
                }
            }
            catch
            {
                // Handle exceptions here if necessary
            }
            if (element == null)
            {
                goto redo;
            }
            return element;
        }

        public static async Task<IElementHandle> FindElementSafeAsync(this IPage page, string query, bool isUseXPath = true)
        {
            redo:
            try
            {
                if (isUseXPath)
                {
                    var elements = await page.XPathAsync(query);
                    return elements.FirstOrDefault(); // Get the first matched element
                }
                else
                {
                    return await page.QuerySelectorAsync(query); // Use CSS selectors if needed
                }
            }catch {
                goto redo;
            }
            
        }


        public static async Task<IElementHandle[]> WaitElementsSafeAsync(this IPage page, string query, bool isUseXPath = true, int timeInMs = 30000)
        {
            IElementHandle[] elements = null;
        redo:

            try
            {
                for (int i = 0; i < timeInMs / 200; i++)
                {
                    elements = await FindElementsSafeAsync(page, query, isUseXPath);
                    if (elements != null && elements.Length > 0)
                    {
                        return elements;
                    }

                    await Task.Delay(200);
                }
            }
            catch
            {
                goto redo;
            }
            if (elements == null)
            {
                goto redo;
            }
            return elements;
        }

        public static async Task<IElementHandle[]> FindElementsSafeAsync(this IPage page, string query, bool isUseXPath = true)
        {
            if (isUseXPath)
            {
                var elements = await page.XPathAsync(query);
                return elements.ToArray(); // Convert the list to an array
            }
            else
            {
                var elements = await page.QuerySelectorAllAsync(query);
                return elements.ToArray(); // Convert the list to an array
            }
        }
        public static async Task<bool> NavigateAndWaitAsync(this IPage page, string url, int timeInMs = 30000)
        {
            if (!string.IsNullOrEmpty(url))
            {
                bool isNavigationCompleted = false;

                // Start navigation with the correct WaitUntil parameter as an array
                var navigationTask = page.GoToAsync(url, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } });

                // Wait for the navigation to complete or timeout
                var timeoutTask = Task.Delay(timeInMs);

                // Wait for either navigation to complete or timeout
                var completedTask = await Task.WhenAny(navigationTask, timeoutTask);

                if (completedTask == navigationTask)
                {
                    isNavigationCompleted = true;
                    await Task.Delay(1000);  // Ensure the page is fully loaded after navigation (optional, depending on the page).
                }

                return isNavigationCompleted;
            }

            return false;
        }

        public static async Task FastInput(IPage page,IElementHandle[] input, string textToCopy)
        {

            new Clipboard().SetText(textToCopy); // Clipboard operation

            // Focus on the input field (replace with XPath if needed)
            await input[0].FocusAsync();
            await input[0].ClickAsync();

            // Paste using Ctrl + V
            await page.Keyboard.DownAsync("Control");
            await page.Keyboard.PressAsync("V");
            await page.Keyboard.UpAsync("Control");


        }
        public static void HoldHoverFor(this IElementHandle element, int delay = 1000)
        {
            Task.Run(async () => await HoldHover(element, delay));
        }

        private static async Task HoldHover(this IElementHandle element,int delay = 1000)
        {
            while (delay != 0)
            {
                await element.FocusAsync();
                await element.HoverAsync();
                delay--;
                await Task.Delay(1);
            }
        }
    }
}
