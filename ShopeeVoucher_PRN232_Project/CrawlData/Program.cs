using PuppeteerSharp;
using CrawlData.Data;
using CrawlData.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrawlData
{
    internal static class Program
    {
        public static async Task Main()
        {
            // Setup configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Setup dependency injection
            var serviceProvider = new ServiceCollection()
                .AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")))
                .BuildServiceProvider();

            const string testUrl = "https://bloggiamgia.vn/shopee";
            
            Console.WriteLine("Downloading Chromium browser (if not already installed)...");
            
            try
            {
                // Download Chromium browser
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
                
                Console.WriteLine("Launching browser...");
                
                // Launch browser
                await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { 
                        "--no-sandbox", 
                        "--disable-setuid-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-gpu"
                    }
                });
                
                // Create new page
                await using var page = await browser.NewPageAsync();
                
                // Set user agent to avoid blocking
                await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                
                Console.WriteLine($"Navigating to {testUrl}...");
                
                // Navigate to URL with shorter timeout and simpler wait strategy
                await page.GoToAsync(testUrl, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                    Timeout = 15000 // 15 seconds timeout
                });
                
                // Wait a bit for dynamic content to load
                await Task.Delay(3000);
                
                // Scroll down to trigger lazy loading
                Console.WriteLine("Scrolling to load dynamic content...");
                await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight / 2)");
                await Task.Delay(1000);
                await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight)");
                await Task.Delay(2000);
                
                // Click "Xem thêm Voucher" button to load more coupons
                Console.WriteLine("Looking for 'Load More' button...");
                try
                {
                    var loadMoreButton = await page.QuerySelectorAsync("div:has(svg) >> text=Xem thêm Voucher");
                    if (loadMoreButton == null)
                    {
                        // Try alternative selectors
                        var buttons = await page.QuerySelectorAllAsync("div");
                        foreach (var btn in buttons)
                        {
                            var text = await page.EvaluateFunctionAsync<string>("el => el.textContent", btn);
                            if (text != null && text.Contains("Xem thêm Voucher"))
                            {
                                loadMoreButton = btn;
                                break;
                            }
                        }
                    }
                    
                    if (loadMoreButton != null)
                    {
                        Console.WriteLine("Clicking 'Xem thêm Voucher' button...");
                        await loadMoreButton.ClickAsync();
                        await Task.Delay(3000); // Wait for more coupons to load
                        Console.WriteLine("More coupons loaded!");
                    }
                    else
                    {
                        Console.WriteLine("'Xem thêm Voucher' button not found, proceeding with current coupons.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not click load more button: {ex.Message}");
                }
                
                Console.WriteLine("Page loaded. Checking for data...");
                
                // Find coupon/voucher elements based on the provided HTML structure
                var possibleSelectors = new[]
                {
                    ".ticket-wrap",  // Main coupon container from the HTML
                    ".ticket",
                    "[class*='ticket']",
                    ".item-voucher",
                    ".voucher-item",
                    ".deal-item",
                    ".coupon-item"
                };
                
                bool canCrawl = false;
                string foundSelector = "";
                int elementCount = 0;
                
                foreach (var selector in possibleSelectors)
                {
                    var elements = await page.QuerySelectorAllAsync(selector);
                    
                    // Filter out hidden/modal elements
                    if (elements != null && elements.Length > 0)
                    {
                        // Check if elements are visible (not modals/dialogs)
                        var visibleCount = 0;
                        foreach (var el in elements)
                        {
                            var isVisible = await page.EvaluateFunctionAsync<bool>(
                                @"el => {
                                    const style = window.getComputedStyle(el);
                                    const rect = el.getBoundingClientRect();
                                    return style.display !== 'none' && 
                                           style.visibility !== 'hidden' && 
                                           rect.width > 0 && 
                                           rect.height > 0 &&
                                           !el.closest('.el-dialog__wrapper, .modal, [style*=""display:none""], [style*=""display: none""]');
                                }", el);
                            if (isVisible) visibleCount++;
                        }
                        
                        if (visibleCount > 0)
                        {
                            canCrawl = true;
                            foundSelector = selector;
                            elementCount = visibleCount;
                            break;
                        }
                    }
                }
                
                // Get page title and content length for additional info
                var title = await page.GetTitleAsync();
                var content = await page.GetContentAsync();
                
                Console.WriteLine($"\n=== Crawl Test Results ===");
                Console.WriteLine($"URL: {testUrl}");
                Console.WriteLine($"Page Title: {title}");
                Console.WriteLine($"Page Content Length: {content.Length} characters");
                
                if (canCrawl)
                {
                    Console.WriteLine($"Status: Can crawl data ✓");
                    Console.WriteLine($"Found {elementCount} visible coupon elements using selector: '{foundSelector}'");
                    
                    // Extract coupon data from visible elements
                    Console.WriteLine($"\n=== Extracting Coupon Data ===");
                    var allElements = await page.QuerySelectorAllAsync(foundSelector);
                    var visibleElements = new List<IElementHandle>();
                    
                    foreach (var el in allElements)
                    {
                        var isVisible = await page.EvaluateFunctionAsync<bool>(
                            @"el => {
                                const style = window.getComputedStyle(el);
                                const rect = el.getBoundingClientRect();
                                return style.display !== 'none' && 
                                       style.visibility !== 'hidden' && 
                                       rect.width > 0 && 
                                       rect.height > 0 &&
                                       !el.closest('.el-dialog__wrapper, .modal, [style*=""display:none""], [style*=""display: none""]');
                            }", el);
                        if (isVisible) visibleElements.Add(el);
                    }
                    
                    int sampleCount = Math.Min(10, visibleElements.Count);
                    Console.WriteLine($"Extracting {sampleCount} coupon(s)...\n");
                    
                    var extractedCoupons = new List<CouponData>();
                    
                    for (int i = 0; i < sampleCount; i++)
                    {
                        var element = visibleElements[i];
                        
                        // Extract coupon data based on the HTML structure
                        var coupon = await page.EvaluateFunctionAsync<CouponData>(@"el => {
                            const getText = (sel) => {
                                const elem = el.querySelector(sel);
                                return elem ? elem.textContent.trim() : '';
                            };
                            const getAttr = (sel, attr) => {
                                const elem = el.querySelector(sel);
                                return elem ? elem.getAttribute(attr) : '';
                            };
                            
                            // Extract supplier name
                            const supplierElem = el.querySelector('.logo-supplier .font-semibold, .mini-title-supplier span');
                            const supplier = supplierElem ? supplierElem.textContent.trim() : 'Unknown';
                            
                            // Extract supplier logo
                            const supplierLogo = getAttr('.logo-supplier img, .mini-title-supplier img', 'src');
                            
                            // Extract discount percentage - look for the bold colored text
                            const discountElem = el.querySelector('.font-bold[style*=""color""], .text-lg.font-bold, .text-2xl.font-bold');
                            const discountPercent = discountElem ? discountElem.textContent.trim() : '';
                            
                            // Determine discount type: % = percent, K = money (1K = 1000)
                            const isPercentDiscount = discountPercent.includes('%');
                            const isMoneyDiscount = discountPercent.includes('K') || discountPercent.includes('k');
                            
                            // Parse discount value
                            let discountValue = null;
                            if (isPercentDiscount) {
                                const match = discountPercent.match(/(\d+(?:\.\d+)?)/);
                                discountValue = match ? parseFloat(match[1]) : null;
                            } else if (isMoneyDiscount) {
                                const match = discountPercent.match(/(\d+(?:\.\d+)?)K/i);
                                if (match) {
                                    discountValue = parseFloat(match[1]) * 1000; // Convert K to actual value
                                }
                            }
                            
                            // Extract coupon code (for money discounts)
                            let couponCode = '';
                            if (isMoneyDiscount) {
                                // Look for code in various places
                                const codeElem = el.querySelector('.code, .coupon-code, [class*=""code""]');
                                if (codeElem) {
                                    couponCode = codeElem.textContent.trim();
                                } else {
                                    // Try to find code in text content
                                    const textContent = el.textContent;
                                    const codeMatch = textContent.match(/(?:Mã|Code|MA):\s*([A-Z0-9]+)/i);
                                    if (codeMatch) {
                                        couponCode = codeMatch[1];
                                    }
                                }
                            }
                            
                            // Extract minimum order value - get the text after 'ĐH tối thiểu:'
                            const minOrderContainer = Array.from(el.querySelectorAll('.text-xs.mb-1')).find(e => 
                                e.textContent.includes('ĐH tối thiểu:'));
                            let minimumOrder = '';
                            if (minOrderContainer) {
                                const fullText = minOrderContainer.textContent;
                                const match = fullText.match(/ĐH tối thiểu:\s*(.+)/);
                                minimumOrder = match ? match[1].trim() : '';
                            }
                            
                            // Extract expiry date - look in the expried-date div
                            const expiryContainer = el.querySelector('.expried-date');
                            let expiryDate = '';
                            if (expiryContainer) {
                                const spans = expiryContainer.querySelectorAll('span');
                                if (spans.length > 1) {
                                    expiryDate = spans[spans.length - 1].textContent.trim();
                                }
                            }
                            
                            // Extract note/description - look for italic text with note
                            const noteElem = el.querySelector('.italic.text-xs.text-left');
                            let note = noteElem ? noteElem.textContent.trim() : '';
                            // Remove 'Xem chi tiết' from the end if present
                            note = note.replace(/\s*Xem chi tiết\s*$/, '');
                            
                            // Extract apply link (List áp dụng)
                            const applyLink = getAttr('a.italic.underline[href*=""shopee""]', 'href');
                            
                            // Extract banner link (Đến Banner button)
                            const bannerBtn = el.querySelector('a.bg-\\[\\#FF9900\\]');
                            const bannerLink = bannerBtn ? bannerBtn.getAttribute('href') : applyLink;
                            
                            // Extract category
                            const category = supplier.includes('Toàn Sàn') ? 'Toàn Sàn' : 'Danh Mục Cụ Thể';
                            
                            return {
                                supplier: supplier,
                                supplierLogo: supplierLogo,
                                discountPercent: discountPercent,
                                minimumOrder: minimumOrder,
                                expiryDate: expiryDate,
                                note: note,
                                applyLink: applyLink,
                                bannerLink: bannerLink,
                                category: category,
                                isPercentDiscount: isPercentDiscount,
                                discountValue: discountValue,
                                couponCode: couponCode
                            };
                        }", element);
                        
                        // Add to the list for database saving
                        extractedCoupons.Add(coupon);
                        
                        // Print formatted coupon data
                        Console.WriteLine($"========== COUPON #{i + 1} ==========");
                        Console.WriteLine($"Supplier:        {coupon.Supplier}");
                        Console.WriteLine($"Logo:            {coupon.SupplierLogo}");
                        Console.WriteLine($"Discount:        {coupon.DiscountPercent}");
                        Console.WriteLine($"Discount Type:   {(coupon.IsPercentDiscount ? "Percent (%)" : "Money (K)")}");
                        if (coupon.DiscountValue.HasValue)
                        {
                            Console.WriteLine($"Discount Value:  {coupon.DiscountValue.Value:N0}");
                        }
                        if (!string.IsNullOrWhiteSpace(coupon.CouponCode))
                        {
                            Console.WriteLine($"Coupon Code:     {coupon.CouponCode}");
                        }
                        Console.WriteLine($"Min Order:       {coupon.MinimumOrder}");
                        Console.WriteLine($"Expiry Date:     {coupon.ExpiryDate}");
                        Console.WriteLine($"Category:        {coupon.Category}");
                        
                        if (!string.IsNullOrWhiteSpace(coupon.Note))
                        {
                            var notePreview = coupon.Note.Length > 100 
                                ? coupon.Note.Substring(0, 100) + "..." 
                                : coupon.Note;
                            Console.WriteLine($"Note:            {notePreview}");
                        }
                        
                        if (!string.IsNullOrWhiteSpace(coupon.ApplyLink))
                        {
                            var linkPreview = coupon.ApplyLink.Length > 80 
                                ? coupon.ApplyLink.Substring(0, 80) + "..." 
                                : coupon.ApplyLink;
                            Console.WriteLine($"Apply Link:      {linkPreview}");
                        }
                        
                        Console.WriteLine();
                    }
                    
                    // Save coupons to database
                    Console.WriteLine($"\n=== Saving to Database ===");
                    try
                    {
                        using var scope = serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        
                        // Ensure database is created
                        await dbContext.Database.EnsureCreatedAsync();
                        
                        int newCount = 0;
                        int updatedCount = 0;
                        
                        foreach (var couponData in extractedCoupons)
                        {
                            // Check if coupon already exists (match by ApplyLink as unique identifier)
                            var existingCoupon = await dbContext.Coupons
                                .FirstOrDefaultAsync(c => c.ApplyLink == couponData.ApplyLink && 
                                                         !string.IsNullOrEmpty(couponData.ApplyLink));
                            
                            if (existingCoupon != null)
                            {
                                // Update existing coupon
                                existingCoupon.Supplier = couponData.Supplier;
                                existingCoupon.SupplierLogo = couponData.SupplierLogo;
                                existingCoupon.DiscountPercent = couponData.DiscountPercent;
                                existingCoupon.MinimumOrder = couponData.MinimumOrder;
                                existingCoupon.Note = couponData.Note;
                                existingCoupon.BannerLink = couponData.BannerLink;
                                existingCoupon.Category = couponData.Category;
                                existingCoupon.IsPercentDiscount = couponData.IsPercentDiscount;
                                existingCoupon.DiscountValue = couponData.DiscountValue;
                                existingCoupon.CouponCode = couponData.CouponCode;
                                
                                // Parse expiry date if possible
                                if (!string.IsNullOrWhiteSpace(couponData.ExpiryDate))
                                {
                                    existingCoupon.ExpiredDate = ParseVietnameseDate(couponData.ExpiryDate);
                                }
                                
                                updatedCount++;
                            }
                            else
                            {
                                // Create new coupon
                                var newCoupon = new Coupon
                                {
                                    Platform = 1, // Shopee (from seed data)
                                    Supplier = couponData.Supplier,
                                    SupplierLogo = couponData.SupplierLogo,
                                    DiscountPercent = couponData.DiscountPercent,
                                    MinimumOrder = couponData.MinimumOrder,
                                    Note = couponData.Note,
                                    ApplyLink = couponData.ApplyLink,
                                    BannerLink = couponData.BannerLink,
                                    Category = couponData.Category,
                                    Description = couponData.Note,
                                    StartDate = DateTime.Now,
                                    IsPercentDiscount = couponData.IsPercentDiscount,
                                    DiscountValue = couponData.DiscountValue,
                                    CouponCode = couponData.CouponCode
                                };
                                
                                // Parse expiry date if possible
                                if (!string.IsNullOrWhiteSpace(couponData.ExpiryDate))
                                {
                                    newCoupon.ExpiredDate = ParseVietnameseDate(couponData.ExpiryDate);
                                }
                                
                                await dbContext.Coupons.AddAsync(newCoupon);
                                newCount++;
                            }
                        }
                        
                        // Save changes to database
                        await dbContext.SaveChangesAsync();
                        
                        Console.WriteLine($"✓ Database updated successfully!");
                        Console.WriteLine($"  - New coupons: {newCount}");
                        Console.WriteLine($"  - Updated coupons: {updatedCount}");
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"✗ Database error: {dbEx.Message}");
                        Console.WriteLine($"  Stack trace: {dbEx.StackTrace}");
                    }
                    
                    Console.WriteLine($"\n=== Summary ===");
                    Console.WriteLine($"✓ Successfully extracted {sampleCount} coupon(s)");
                    Console.WriteLine($"✓ Total coupons found: {elementCount}");
                    Console.WriteLine($"✓ Selector used: '{foundSelector}'");
                    
                    Console.WriteLine($"\n=== Data Structure ===");
                    Console.WriteLine($"Each coupon contains:");
                    Console.WriteLine($"  - Supplier (e.g., 'Toàn Sàn', category name)");
                    Console.WriteLine($"  - Supplier Logo URL");
                    Console.WriteLine($"  - Discount Percentage");
                    Console.WriteLine($"  - Minimum Order Value");
                    Console.WriteLine($"  - Expiry Date (HSD)");
                    Console.WriteLine($"  - Note/Description");
                    Console.WriteLine($"  - Apply Link (List áp dụng)");
                    Console.WriteLine($"  - Banner Link");
                    Console.WriteLine($"  - Category");
                    
                    // Save screenshot
                    var screenshotPath = "coupons_crawled.png";
                    await page.ScreenshotAsync(screenshotPath, new ScreenshotOptions { FullPage = true });
                    Console.WriteLine($"\nScreenshot saved to: {screenshotPath}");
                }
                else
                {
                    Console.WriteLine($"Status: Cannot crawl data ✗");
                    Console.WriteLine($"No matching elements found");
                    Console.WriteLine($"Tried selectors: {string.Join(", ", possibleSelectors)}");
                    Console.WriteLine($"\n=== Suggestion ===");
                    Console.WriteLine($"Inspect the page HTML to find the correct selectors.");
                    
                    // Save a screenshot for inspection
                    var screenshotPath = "page_screenshot.png";
                    await page.ScreenshotAsync(screenshotPath);
                    Console.WriteLine($"Screenshot saved to: {screenshotPath}");
                    
                    // Also save HTML for inspection
                    var htmlPath = "page_content.html";
                    await File.WriteAllTextAsync(htmlPath, content);
                    Console.WriteLine($"HTML content saved to: {htmlPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot crawl data: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Parse Vietnamese date format (e.g., "31/12/2024" or "31-12-2024")
        /// </summary>
        private static DateTime? ParseVietnameseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            try
            {
                // Try common Vietnamese date formats
                string[] formats = {
                    "dd/MM/yyyy",
                    "dd-MM-yyyy",
                    "dd/MM/yyyy HH:mm",
                    "dd-MM-yyyy HH:mm",
                    "dd/MM/yyyy HH:mm:ss",
                    "dd-MM-yyyy HH:mm:ss"
                };

                if (DateTime.TryParseExact(dateString.Trim(), formats, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out var result))
                {
                    return result;
                }

                // If exact parsing fails, try general parsing
                if (DateTime.TryParse(dateString, out var generalResult))
                {
                    return generalResult;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// DTO class for crawled coupon data from the website
    /// </summary>
    public class CouponData
    {
        public string Supplier { get; set; } = string.Empty;
        public string SupplierLogo { get; set; } = string.Empty;
        public string DiscountPercent { get; set; } = string.Empty;
        public string MinimumOrder { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string ApplyLink { get; set; } = string.Empty;
        public string BannerLink { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        
        // New fields for discount type and code
        public bool IsPercentDiscount { get; set; } // true = %, false = money (K)
        public double? DiscountValue { get; set; } // Parsed numeric value
        public string CouponCode { get; set; } = string.Empty; // For money discounts
    }
}
