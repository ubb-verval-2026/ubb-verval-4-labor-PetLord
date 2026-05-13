using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class BlazDemoTests
{
    private const string BlazDemoUrl = "https://blazedemo.com";
    private const double PriceThreshold = 500.0;

    private IWebDriver driver;

    [SetUp]
    public void SetUp()
    {
        var options = new ChromeOptions();
        driver = new ChromeDriver(options);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    [TearDown]
    public void TearDown()
    {
        driver?.Quit();
        driver?.Dispose();
    }

    [Test]
    public void FlightSearch_MexicoCityToDublin_ShouldHaveAtLeastThreeFlights()
    {
        driver.Navigate().GoToUrl(BlazDemoUrl);

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

        var fromSelect = new SelectElement(wait.Until(d => d.FindElement(By.Name("fromPort"))));
        fromSelect.SelectByText("Mexico City");

        var toSelect = new SelectElement(driver.FindElement(By.Name("toPort")));
        toSelect.SelectByText("Dublin");

        driver.FindElement(By.CssSelector("input[type='submit']")).Click();

        wait.Until(d => d.FindElement(By.CssSelector("table.table tbody tr")));

        var flightRows = driver.FindElements(By.CssSelector("table.table tbody tr"));
        flightRows.Count.Should().BeGreaterThanOrEqualTo(3,
            $"expected at least 3 flights from Mexico City to Dublin, but found {flightRows.Count}");

        TakeScreenshotIfCheapFlightExists(flightRows);
    }


    private void TakeScreenshotIfCheapFlightExists(IReadOnlyCollection<IWebElement> flightRows)
    {
        string screenshotFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        foreach (var row in flightRows)
        {
            var cells = row.FindElements(By.TagName("td"));
            if (cells.Count < 4)
                continue;

            string priceText = cells[3].Text.Trim().Replace("$", "").Replace(",", ".");
            if (!double.TryParse(priceText, System.Globalization.NumberStyles.Any,
                                  System.Globalization.CultureInfo.InvariantCulture, out double price))
                continue;

            if (price < PriceThreshold)
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = Path.Combine(screenshotFolder, $"cheap_flight_dublin_{timestamp}.png");

                var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                screenshot.SaveAsFile(filePath);

                TestContext.WriteLine($"Cheap flight found (${price}). Screenshot saved to: {filePath}");
                break; // one screenshot is enough
            }
        }
    }
}
