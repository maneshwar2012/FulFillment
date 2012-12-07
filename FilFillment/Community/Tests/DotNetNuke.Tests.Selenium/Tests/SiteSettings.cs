﻿using System;
using NUnit.Framework;
using OpenQA.Selenium;

namespace DotNetNuke.Tests.Selenium.Tests
{
    [TestFixture]
    public class SiteSettings : Base
    {
        public static By Header = By.CssSelector("*[id$='_dnnTITLE_titleLabel']");
        public static By AdvancedTab = By.XPath("//*[@id='dnnSiteSettings']/ul[1]/li[2]/a");
        public static By UsabilitySettings = By.XPath("//*[@id='dnnSitePanel-Usability']/a");

        public static By UsabilitySettingPopUpCheck = By.XPath("//*[@id='ssAdvancedSettings']/div[2]/fieldset[4]/div[1]/span");
        public static By UsabilitySettingPopUps = By.XPath("//*[@id='ssAdvancedSettings']/div[2]/fieldset[4]/div[1]/span/span");

        public static By UpdateButton = By.XPath("//*[@id='dnn_ctr442_SiteSettings_cmdUpdate']");

        public static void DisablePopups(IWebDriver driver)
        {
            Login.AsHost(driver);

            driver.Navigate().GoToUrl(SiteSettingsPage);

            driver.WaitClick(AdvancedTab);
            driver.WaitClick(UsabilitySettings);

            if (driver.FindDnnElement(UsabilitySettingPopUpCheck).GetAttribute("class").Contains("dnnCheckbox-checked"))
            {
                driver.WaitClick(UsabilitySettingPopUps);
                driver.WaitClick(UpdateButton);
            }
        }

        #region VerifyDisablePopups

        [Test]
        public void VerifyDisablePopupsFirefox() { VerifyDisablePopups(Firefox); }
        [Test]
        public void VerifyDisablePopupsChrome() { VerifyDisablePopups(Chrome); }
        [Test]
        public void VerifyDisablePopupsIe() { VerifyDisablePopups(Ie); }

        private static void VerifyDisablePopups(IWebDriver driver)
        {
            DisablePopups(driver);

            Assert.That(driver.FindDnnElement(UsabilitySettingPopUpCheck).GetAttribute("class"), Is.Not.StringContaining("dnnCheckbox-checked"));
        }

        #endregion


        #region VerifySiteSettingsHost

        [Test]
        public void VerifySiteSettingsHostFirefox() { VerifySiteSettingsHost(Firefox); }
        [Test]
        public void VerifySiteSettingsHosthChrome() { VerifySiteSettingsHost(Chrome); }
        [Test]
        public void VerifySiteSettingsHostIe() { VerifySiteSettingsHost(Ie); }

        private static void VerifySiteSettingsHost(IWebDriver driver)
        {
            Logoff(driver);

            Login.AsHost(driver);

            driver.Navigate().GoToUrl(SiteSettingsPage);

            Assert.That(driver.FindDnnElement(Header).Text, Is.StringContaining("Site Settings"), "Site Settings page not accessible");
        }

        #endregion
    }
}
