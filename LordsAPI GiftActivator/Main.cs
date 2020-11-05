﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Threading;

namespace LordsAPI_GiftActivator
{
    public class LordsMobileGift
    {
        public enum Methods
        {
            IGG_ID,
            Nickname,
        }
        public static bool Activate(Methods method, string igg_id, string promo)
        {
            if (method == Methods.IGG_ID)
            {
                try
                {
                    ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                    service.HideCommandPromptWindow = true;

                    var options = new ChromeOptions();
                    options.AddArgument("--window-position=-32000,-32000");

                    IWebDriver driver = new OpenQA.Selenium.Chrome.ChromeDriver(service, options);
                    driver.Manage().Window.Minimize();
                    driver.Navigate().GoToUrl("https://lordsmobile.igg.com/gifts/");

                    By igg_idInputElement = By.XPath("//input[@class='myname']");
                    By codeInputElement = By.XPath("//input[@class='mycode']");
                    By claimButton = By.Id("btn_claim_1");
                    By doneMessageElement = By.Id("msg");

                    var igg = driver.FindElement(igg_idInputElement);
                    var code = driver.FindElement(codeInputElement);
                    var submit = driver.FindElement(claimButton);

                    igg.Click();
                    igg.SendKeys(igg_id);
                    code.Click();
                    code.SendKeys(promo);
                    submit.Click();

                    var donemsg = driver.FindElement(doneMessageElement);
                    driver.Close();
                    driver.Quit();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else if (method == Methods.Nickname)
            {
                try
                {
                    ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                    service.HideCommandPromptWindow = true;

                    var options = new ChromeOptions();
                    options.AddArgument("--window-position=-32000,-32000");

                    IWebDriver driver = new OpenQA.Selenium.Chrome.ChromeDriver(service, options);
                    driver.Manage().Window.Minimize();
                    driver.Navigate().GoToUrl("https://lordsmobile.igg.com/gifts/");

                    var methodfind = driver.FindElement(By.ClassName("tab-list-2"));
                    methodfind.Click();
                    var nicknameInputfind = driver.FindElement(By.XPath("//input[@id='charname']"));
                    nicknameInputfind.Click();
                    nicknameInputfind.SendKeys(igg_id);
                    var code = driver.FindElement(By.XPath("//input[@id='cdkey_2']"));
                    code.Click();
                    code.SendKeys(promo);
                    var submit = driver.FindElement(By.Id("btn_claim_2"));
                    submit.Click();
                    var donemsg = driver.FindElement(By.Id("msg"));
                    if (donemsg.Text != "Игрок с таким именем не существует")
                    {
                        var submit2 = driver.FindElement(By.Id("btn_confirm"));
                        submit2.Click();

                        var donemsg2 = driver.FindElement(By.Id("msg"));
                        driver.Close();
                        driver.Quit();
                        return true;
                    }
                    else
                    {
                        driver.Close();
                        driver.Quit();
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            else return false;
        }
    }
}
