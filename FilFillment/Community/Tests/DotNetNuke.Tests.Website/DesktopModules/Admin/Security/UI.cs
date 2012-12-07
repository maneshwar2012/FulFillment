using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;

namespace DotNetNuke.Tests.Website.DesktopModules.Admin.Security
{
    public class UI
    {
        public static IWebElement AddNewUserActionMenuItem(IWebDriver driver)
        {
            var userMenuUserLink = driver.FindDnnElementByXpath(driver, "//*[@id='ControlActionMenu']/li[3]/a");
            var builder = new Actions(driver);
            var hoverOverUserMenuUserLink = builder.MoveToElement(userMenuUserLink).ClickAndHold();
            //hoverOverUserMenuUserLink.Perform();
            return driver.FindDnnElementByXpath(driver, "//*[@id='ControlActionMenu']/li[3]/ul/li[1]/a");
        }

        public static IWebElement UserNameTextbox(IWebDriver driver) { return driver.FindDnnElementById(driver, "dnn_ctr_Login_Login_DNN_txtUsername"); }
        public static IWebElement FirstNameTextbox(IWebDriver driver) { return driver.FindDnnElementById(driver, "dnn_ctr_Login_Login_DNN_txtUsername"); }
        public static IWebElement LastNameTextbox(IWebDriver driver) { return driver.FindDnnElementById(driver, "dnn_ctr_Login_Login_DNN_txtUsername"); }
        public static IWebElement DisplayNameTextbox(IWebDriver driver) { return driver.FindDnnElementById(driver, "dnn_ctr_Login_Login_DNN_txtUsername"); }
        public static IWebElement EmailTextbox(IWebDriver driver) { return driver.FindDnnElementById(driver, "dnn_ctr_Login_Login_DNN_txtUsername"); }
        public static IWebElement PasswordTextbox(IWebDriver driver) { return driver.FindDnnElementById(driver, "dnn_ctr_Login_Login_DNN_txtUsername"); }
        public static IWebElement ConfirmPasswordTextbox(IWebDriver driver) { return driver.FindDnnElementById(driver, "dnn_ctr_Login_Login_DNN_txtUsername"); }
        public static IWebElement AuthorizeCheckbox(IWebDriver driver) { return driver.FindDnnElementById(driver, "dnn_ctr_Login_Login_DNN_txtUsername"); }
        public static IWebElement NotifyCheckbox(IWebDriver driver) { return driver.FindDnnElementById(driver, "dnn_ctr_Login_Login_DNN_txtUsername"); }
        public static IWebElement RandomPasswordCheckbox(IWebDriver driver) { return driver.FindDnnElementById(driver, "dnn_ctr_Login_Login_DNN_txtUsername"); }
    }
}

