﻿using System.Web;
using PuppeteerSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CrawlData.Models;

namespace CrawlData
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Shopee Coupon Crawler Started ===");
            
            try
            {
                // Load configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                // Setup database context
                var optionsBuilder = new DbContextOptionsBuilder<CouponDbContext>();
                optionsBuilder.UseSqlServer(connectionString);

                using var dbContext = new CouponDbContext(optionsBuilder.Options);

                // Ensure database is created
                await dbContext.Database.EnsureCreatedAsync();
                Console.WriteLine("Database connection established.");

                // Download browser if not exists
                Console.WriteLine("Checking browser installation...");
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
                Console.WriteLine("Browser ready.");

                // Launch browser
                Console.WriteLine("Launching browser...");
                await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                });

                await using var page = await browser.NewPageAsync();
                
                // Set user agent to avoid blocking
                await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                
                // Navigate to the website
                Console.WriteLine("Navigating to https://bloggiamgia.vn/shopee...");
                await page.GoToAsync("https://bloggiamgia.vn/shopee", new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                    Timeout = 15000 // 15 seconds timeout
                });

                Console.WriteLine("Page loaded. Waiting for content...");
                await Task.Delay(3000); // Wait for dynamic content to load

                // Scroll to load dynamic content
                Console.WriteLine("Scrolling to trigger lazy loading...");
                await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight / 2)");
                await Task.Delay(1000);
                await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight)");
                await Task.Delay(2000);

                // Click "See more Voucher" button to load all coupons
                Console.WriteLine("Clicking 'See more Voucher' to load all coupons...");
                await ClickSeeMoreButton(page);

                // Extract coupon data
                Console.WriteLine("Extracting coupon data...");
                var coupons = await page.EvaluateFunctionAsync<List<CouponData>>(@"() => {
                    const coupons = [];
                    const couponElements = document.querySelectorAll('[data-v-56fdd109].flex.flex-col.justify-between.cursor-pointer');
                    
                    couponElements.forEach(element => {
                        try {
                            const coupon = {};
                            
                            // Extract supplier
                            const supplierElement = element.querySelector('.mini-title-supplier span');
                            coupon.supplier = supplierElement ? supplierElement.textContent.trim() : null;
                            
                            // Extract discount and determine type
                            const discountElement = element.querySelector('.font-bold.text-lg span, .font-bold.text-2xl span');
                            const discountText = discountElement ? discountElement.textContent.trim() : '';
                            
                            if (discountText.includes('%')) {
                                coupon.type = true; // percentage
                                coupon.discount = parseFloat(discountText.replace('%', '').trim());
                            } else if (discountText.includes('K')) {
                                coupon.type = false; // fixed amount
                                coupon.discount = parseFloat(discountText.replace('K', '').trim()) * 1000;
                            } else {
                                coupon.type = false;
                                coupon.discount = parseFloat(discountText.replace(/[^0-9.]/g, ''));
                            }
                            
                            // Extract minimum value
                            const minValueElement = element.querySelector('.text-xs.mb-1 .font-semibold');
                            if (minValueElement) {
                                const minValueText = minValueElement.textContent.trim();
                                coupon.minValueApply = parseFloat(minValueText.replace(/[^0-9]/g, ''));
                            }
                            
                            // Extract available percentage
                            const availableElement = element.querySelector('.text-xs.mb-1 span span.font-semibold');
                            if (availableElement && availableElement.textContent.includes('%')) {
                                coupon.available = parseFloat(availableElement.textContent.replace('%', '').trim());
                            }
                            
                            // Extract description
                            const descElement = element.querySelector('.italic.text-xs.text-left');
                            coupon.description = descElement ? descElement.textContent.trim() : null;
                            
                            // Extract expired date
                            const dateElement = element.querySelector('.expried-date .text-left.italic');
                            coupon.expiredDate = dateElement ? dateElement.textContent.trim() : null;
                            
                            // Extract URL apply list
                            const linkElement = element.querySelector('a[href*=""origin_link""]');
                            coupon.urlApplyList = linkElement ? linkElement.getAttribute('href') : null;
                            
                            coupons.push(coupon);
                        } catch (error) {
                            console.error('Error parsing coupon:', error);
                        }
                    });
                    
                    return coupons;
                }");

                Console.WriteLine($"Found {coupons.Count} coupons. Processing...");

                int savedCount = 0;
                int skippedCount = 0;

                foreach (var couponData in coupons)
                {
                    try
                    {
                        // Parse expired date
                        DateTime expiredDate = ParseExpiredDate(couponData.ExpiredDate);
                        
                        // Extract origin link
                        string? originLink = null;
                        if (!string.IsNullOrEmpty(couponData.UrlApplyList))
                        {
                            originLink = ExtractOriginLink(couponData.UrlApplyList);
                        }

                        // Create coupon entity
                        var coupon = new Coupon
                        {
                            Type = couponData.Type,
                            Supplier = couponData.Supplier,
                            Discount = couponData.Discount,
                            MinValueApply = couponData.MinValueApply,
                            Description = couponData.Description,
                            StartDate = DateTime.Now,
                            Available = couponData.Available,
                            ExpiredDate = expiredDate,
                            UrlApplyList = originLink,
                            Code = null, // Code needs to be extracted via button click
                            Platform = "shopee"
                        };

                        // Check if coupon already exists (avoid duplicates)
                        var exists = await dbContext.Coupons.AnyAsync(c => 
                            c.Supplier == coupon.Supplier &&
                            c.Discount == coupon.Discount &&
                            c.ExpiredDate == coupon.ExpiredDate &&
                            c.Platform == "shopee"
                        );

                        if (!exists)
                        {
                            dbContext.Coupons.Add(coupon);
                            savedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing coupon: {ex.Message}");
                    }
                }

                // Save to database
                if (savedCount > 0)
                {
                    await dbContext.SaveChangesAsync();
                    Console.WriteLine($"Successfully saved {savedCount} new coupons to database.");
                }
                
                if (skippedCount > 0)
                {
                    Console.WriteLine($"Skipped {skippedCount} duplicate coupons.");
                }

                Console.WriteLine("=== Crawling Completed Successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        static async Task AutoScroll(IPage page)
        {
            await page.EvaluateFunctionAsync(@"async () => {
                await new Promise((resolve) => {
                    let totalHeight = 0;
                    const distance = 100;
                    const timer = setInterval(() => {
                        const scrollHeight = document.body.scrollHeight;
                        window.scrollBy(0, distance);
                        totalHeight += distance;

                        if (totalHeight >= scrollHeight) {
                            clearInterval(timer);
                            resolve();
                        }
                    }, 100);
                });
            }");
            await Task.Delay(1000);
        }

        static async Task ClosePopupIfExists(IPage page)
        {
            try
            {
                // Wait 5 seconds for popup to appear
                Console.WriteLine("Waiting for popup to appear (5 seconds)...");
                await Task.Delay(5000);
                
                Console.WriteLine("Checking for popup...");
                
                // Check if popup wrapper exists - Element-UI dialog structure
                var popupWrapper = await page.QuerySelectorAsync(".el-dialog__wrapper");
                
                if (popupWrapper == null)
                {
                    Console.WriteLine("No popup detected.");
                    return;
                }
                
                Console.WriteLine("Popup detected! Attempting to close...");
                
                // Method 1: Click the X icon button in the image area (most reliable based on HTML)
                var closeIconInImage = await page.QuerySelectorAsync("i.el-icon-close.absolute.text-white");
                if (closeIconInImage != null)
                {
                    await closeIconInImage.ClickAsync();
                    Console.WriteLine("Popup closed via X icon in image area.");
                    await Task.Delay(500);
                    return;
                }
                
                // Method 2: Click the dialog header close button
                var headerCloseButton = await page.QuerySelectorAsync(".el-dialog__headerbtn");
                if (headerCloseButton != null)
                {
                    await headerCloseButton.ClickAsync();
                    Console.WriteLine("Popup closed via dialog header close button.");
                    await Task.Delay(500);
                    return;
                }
                
                // Method 3: Click the close icon in header
                var closeIcon = await page.QuerySelectorAsync(".el-dialog__close.el-icon-close");
                if (closeIcon != null)
                {
                    await closeIcon.ClickAsync();
                    Console.WriteLine("Popup closed via header close icon.");
                    await Task.Delay(500);
                    return;
                }
                
                // Method 4: Click outside the popup dialog (on the wrapper)
                // Get the dialog position and click outside it
                var clickedOutside = await page.EvaluateFunctionAsync<bool>(@"() => {
                    const wrapper = document.querySelector('.el-dialog__wrapper');
                    const dialog = document.querySelector('.el-dialog');
                    if (wrapper && dialog) {
                        // Click on the wrapper (dark overlay) outside the dialog
                        const rect = dialog.getBoundingClientRect();
                        // Click at top-left corner of viewport (outside dialog)
                        wrapper.click();
                        return true;
                    }
                    return false;
                }");
                
                if (clickedOutside)
                {
                    Console.WriteLine("Popup closed by clicking outside dialog.");
                    await Task.Delay(500);
                    return;
                }
                
                // Method 5: Hide via JavaScript as last resort
                var hidden = await page.EvaluateFunctionAsync<bool>(@"() => {
                    const wrapper = document.querySelector('.el-dialog__wrapper');
                    if (wrapper) {
                        wrapper.style.display = 'none';
                        return true;
                    }
                    return false;
                }");
                
                if (hidden)
                {
                    Console.WriteLine("Popup hidden via JavaScript.");
                    await Task.Delay(500);
                    return;
                }
                
                Console.WriteLine("Could not close popup with any method.");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while trying to close popup: {ex.Message}");
            }
        }

        static async Task ClickSeeMoreButton(IPage page)
        {
            int loadMoreClicked = 0;
            int maxClicks = 50; // Safety limit to prevent infinite loop
            
            while (loadMoreClicked < maxClicks)
            {
                try
                {
                    // Close popup if it appears before clicking
                    await ClosePopupIfExists(page);
                    
                    // Find the "Xem thêm Voucher" button - try multiple selectors
                    var seeMoreButton = await page.QuerySelectorAsync(".see-more, [class*='see-more']");
                    
                    if (seeMoreButton == null)
                    {
                        Console.WriteLine($"No more 'Xem thêm Voucher' button found. Total clicks: {loadMoreClicked}");
                        break;
                    }
                    
                    // Check if button is visible
                    var isVisible = await page.EvaluateFunctionAsync<bool>(@"el => {
                        const style = window.getComputedStyle(el);
                        const rect = el.getBoundingClientRect();
                        return style.display !== 'none' && 
                               style.visibility !== 'hidden' && 
                               rect.width > 0 && 
                               rect.height > 0;
                    }", seeMoreButton);
                    
                    if (!isVisible)
                    {
                        Console.WriteLine($"'Xem thêm Voucher' button not visible. Total clicks: {loadMoreClicked}");
                        break;
                    }
                    
                    // Scroll to button and click
                    await page.EvaluateFunctionAsync("el => el.scrollIntoView({behavior: 'smooth', block: 'center'})", seeMoreButton);
                    await Task.Delay(500);
                    
                    await seeMoreButton.ClickAsync();
                    loadMoreClicked++;
                    Console.WriteLine($"Clicked 'Xem thêm Voucher' button ({loadMoreClicked} time(s))");
                    
                    // Wait for new content to load
                    await Task.Delay(2000);
                    
                    // Scroll down again to trigger lazy loading
                    await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight)");
                    await Task.Delay(1000);
                }
                catch (Exception)
                {
                    Console.WriteLine($"No more coupons to load. Total clicks: {loadMoreClicked}");
                    break;
                }
            }
            
            if (loadMoreClicked >= maxClicks)
            {
                Console.WriteLine($"Reached maximum click limit ({maxClicks}). Proceeding with extraction...");
            }
            
            Console.WriteLine($"Finished loading coupons (clicked load more {loadMoreClicked} time(s))");
            
            // Final scroll to ensure all content is loaded
            await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight)");
            await Task.Delay(2000);
        }

        static string? ExtractOriginLink(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                var queryParams = HttpUtility.ParseQueryString(uri.Query);
                string? originLink = queryParams["origin_link"];
                if (!string.IsNullOrEmpty(originLink))
                {
                    return HttpUtility.UrlDecode(originLink);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting origin link: {ex.Message}");
            }
            return null;
        }

        static DateTime ParseExpiredDate(string? dateString)
        {
            if (string.IsNullOrEmpty(dateString))
            {
                return DateTime.Now.AddMonths(1); // Default 1 month from now
            }

            try
            {
                // Expected format: dd/MM
                var parts = dateString.Trim().Split('/');
                if (parts.Length == 2)
                {
                    int day = int.Parse(parts[0]);
                    int month = int.Parse(parts[1]);
                    int year = DateTime.Now.Year;

                    // If the date has passed this year, assume next year
                    var date = new DateTime(year, month, day);
                    if (date < DateTime.Now)
                    {
                        date = new DateTime(year + 1, month, day);
                    }

                    return date;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing date '{dateString}': {ex.Message}");
            }

            return DateTime.Now.AddMonths(1);
        }
    }

    // DTO class for coupon data from webpage
    public class CouponData
    {
        public bool Type { get; set; }
        public string? Supplier { get; set; }
        public double Discount { get; set; }
        public double? MinValueApply { get; set; }
        public double? Available { get; set; }
        public string? Description { get; set; }
        public string? ExpiredDate { get; set; }
        public string? UrlApplyList { get; set; }
    }
}
