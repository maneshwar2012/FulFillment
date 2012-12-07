﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (http://www.specflow.org/).
//      SpecFlow Version:1.8.1.0
//      SpecFlow Generator Version:1.8.0.0
//      Runtime Version:4.0.30319.239
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace DotNetNuke.Website.Specs.AdminFeatures
{
    using TechTalk.SpecFlow;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "1.8.1.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [NUnit.Framework.TestFixtureAttribute()]
    [NUnit.Framework.DescriptionAttribute("File Manager")]
    public partial class FileManagerFeature
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "FileManager.feature"
#line hidden
        
        [NUnit.Framework.TestFixtureSetUpAttribute()]
        public virtual void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "File Manager", "In order to Manage files\r\nAs a administrator\r\nI want to use file manager correctl" +
                    "y", ProgrammingLanguage.CSharp, ((string[])(null)));
            testRunner.OnFeatureStart(featureInfo);
        }
        
        [NUnit.Framework.TestFixtureTearDownAttribute()]
        public virtual void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        [NUnit.Framework.SetUpAttribute()]
        public virtual void TestInitialize()
        {
        }
        
        [NUnit.Framework.TearDownAttribute()]
        public virtual void ScenarioTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioSetup(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioStart(scenarioInfo);
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Synchornize files correctly when there are files without protect extensions exist" +
            " in secure folder")]
        [NUnit.Framework.CategoryAttribute("MustBeDefaultAdminCredentials")]
        public virtual void SynchornizeFilesCorrectlyWhenThereAreFilesWithoutProtectExtensionsExistInSecureFolder()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Synchornize files correctly when there are files without protect extensions exist" +
                    " in secure folder", new string[] {
                        "MustBeDefaultAdminCredentials"});
#line 7
this.ScenarioSetup(scenarioInfo);
#line 8
 testRunner.Given("I am on the site home page");
#line 9
 testRunner.And("I have logged in as the admin");
#line 10
 testRunner.When("I navigate to the admin page File Manager");
#line 11
 testRunner.And("I add a Secure folder called Secure Folder");
#line 12
 testRunner.And("I copy a simple zip file to the secure folder called Secure Folder");
#line 13
 testRunner.And("I try to synchorize folder recursive");
#line 14
 testRunner.And("I select the secure folder called Secure Folder");
#line 15
 testRunner.Then("I should see the file exist in file manager");
#line hidden
            this.ScenarioCleanup();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Moving a file to a folder that already contains a file with the same name")]
        [NUnit.Framework.CategoryAttribute("MustBeDefaultAdminCredentials")]
        public virtual void MovingAFileToAFolderThatAlreadyContainsAFileWithTheSameName()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Moving a file to a folder that already contains a file with the same name", new string[] {
                        "MustBeDefaultAdminCredentials"});
#line 18
this.ScenarioSetup(scenarioInfo);
#line 19
 testRunner.Given("I am on the site home page");
#line 20
 testRunner.And("I have logged in as the admin");
#line 21
 testRunner.When("I navigate to the admin page File Manager");
#line 22
 testRunner.And("I add a Standard folder called folder1");
#line 23
 testRunner.And("I add a Standard folder called folder2");
#line 24
 testRunner.And("I add the file Do Change.doc to the folder folder1");
#line 25
 testRunner.And("I add the file Do Change.doc to the folder folder2");
#line 26
 testRunner.And("Moving the file from folder1 to folder2");
#line 27
 testRunner.Then("The file should moved to folder2 without error");
#line hidden
            this.ScenarioCleanup();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Login Page should appear when accessing a restricted file in a secure folder when" +
            " Logged out")]
        [NUnit.Framework.CategoryAttribute("MustBeDefaultAdminCredentials")]
        public virtual void LoginPageShouldAppearWhenAccessingARestrictedFileInASecureFolderWhenLoggedOut()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Login Page should appear when accessing a restricted file in a secure folder when" +
                    " Logged out", new string[] {
                        "MustBeDefaultAdminCredentials"});
#line 30
this.ScenarioSetup(scenarioInfo);
#line 31
 testRunner.Given("I am on the site home page 300");
#line 32
 testRunner.And("I have logged in as the admin");
#line 33
 testRunner.When("I navigate to the admin page File Manager");
#line 34
 testRunner.And("I add a Secure folder called SecurePermissionsFolder");
#line 35
 testRunner.And("I click the folder SecurePermissionsFolder");
#line 36
 testRunner.And("I Uncheck the Browse Folder permission for the role All Users");
#line 37
 testRunner.And("I Uncheck the View permission for the role All Users");
#line 38
 testRunner.And("I click Update File Manager");
#line 39
 testRunner.And("I add the file Do Change.doc to the folder SecurePermissionsFolder");
#line 40
 testRunner.And("I have created the default page called Secure File from the Ribbon Bar");
#line 41
 testRunner.And("The page Secure File has View permission set to Grant for the role All Users");
#line 42
 testRunner.And("I am viewing the page called Secure File");
#line 43
 testRunner.And("I edit one of the html module content");
#line 44
 testRunner.And("I enter Secure File Content and click hyper link manager button in rad text edito" +
                    "r");
#line 45
 testRunner.And("I click the Telerik Editor HyperLink button");
#line 46
 testRunner.And("I select the folder SecurePermissionsFolder");
#line 47
 testRunner.And("I link to the document Do Change.doc");
#line 48
 testRunner.And("I click Save on the Html Module");
#line 49
 testRunner.And("I log off");
#line 50
 testRunner.And("I am viewing the page called Secure File");
#line 51
 testRunner.And("I click the link Secure File Content");
#line 52
 testRunner.Then("I should see the Login screen");
#line hidden
            this.ScenarioCleanup();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Login Page and Access Denied message should appear when accessing a restricted fi" +
            "le in a secure folder when Logged in")]
        [NUnit.Framework.CategoryAttribute("MustBeDefaultAdminCredentials")]
        [NUnit.Framework.CategoryAttribute("MustHaveAUserWithFullProfile")]
        public virtual void LoginPageAndAccessDeniedMessageShouldAppearWhenAccessingARestrictedFileInASecureFolderWhenLoggedIn()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Login Page and Access Denied message should appear when accessing a restricted fi" +
                    "le in a secure folder when Logged in", new string[] {
                        "MustBeDefaultAdminCredentials",
                        "MustHaveAUserWithFullProfile"});
#line 56
this.ScenarioSetup(scenarioInfo);
#line 57
 testRunner.Given("I am on the site home page 300");
#line 58
 testRunner.And("I have logged in as the admin");
#line 59
 testRunner.When("I navigate to the admin page File Manager");
#line 60
 testRunner.And("I add a Secure folder called SecurePermissionsFolderScenario2");
#line 61
 testRunner.And("I click the folder SecurePermissionsFolderScenario2");
#line 62
 testRunner.And("I Uncheck the Browse Folder permission for the role All Users");
#line 63
 testRunner.And("I Uncheck the View permission for the role All Users");
#line 64
 testRunner.And("I click Update File Manager");
#line 65
 testRunner.And("I add the file Do Change.doc to the folder SecurePermissionsFolderScenario2");
#line 66
 testRunner.And("I have created the default page called Secure File Scenario 2 from the Ribbon Bar" +
                    "");
#line 67
 testRunner.And("The page Secure File Scenario 2 has View permission set to Grant for the role All" +
                    " Users");
#line 68
 testRunner.And("I am viewing the page called Secure File Scenario 2");
#line 69
 testRunner.And("I edit one of the html module content");
#line 70
 testRunner.And("I enter Secure File Content Scenario 2 and click hyper link manager button in rad" +
                    " text editor");
#line 71
 testRunner.And("I click the Telerik Editor HyperLink button");
#line 72
 testRunner.And("I select the folder SecurePermissionsFolderScenario2");
#line 73
 testRunner.And("I link to the document Do Change.doc");
#line 74
 testRunner.And("I click Save on the Html Module");
#line 75
 testRunner.And("I log off");
#line 76
 testRunner.And("I have logged in as the user MichaelWoods password1234");
#line 77
 testRunner.And("I am viewing the page called Secure File Scenario 2");
#line 78
 testRunner.And("I click the link Secure File Content");
#line 79
 testRunner.Then("I should see the File Access Error message");
#line hidden
            this.ScenarioCleanup();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("UNC Folder Provider shouldn\'t be able to create under medium trust")]
        [NUnit.Framework.CategoryAttribute("MustBeDefaultAdminCredentials")]
        [NUnit.Framework.CategoryAttribute("SiteMustRunInMediumTrust")]
        public virtual void UNCFolderProviderShouldnTBeAbleToCreateUnderMediumTrust()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("UNC Folder Provider shouldn\'t be able to create under medium trust", new string[] {
                        "MustBeDefaultAdminCredentials",
                        "SiteMustRunInMediumTrust"});
#line 83
this.ScenarioSetup(scenarioInfo);
#line 84
 testRunner.Given("I am on the site home page");
#line 85
 testRunner.And("I have logged in as the admin");
#line 86
 testRunner.When("I navigate to the admin page File Manager");
#line 87
 testRunner.And("I clik Manage Folder Types button from action menu");
#line 88
 testRunner.And("I Click Add New Type Button");
#line 89
 testRunner.And("I input in folder type name field");
#line 90
 testRunner.And("I select Folder type as UNCFolderProvider");
#line 91
 testRunner.Then("the UNC settings should be disabled");
#line hidden
            this.ScenarioCleanup();
        }
    }
}
#pragma warning restore
#endregion
