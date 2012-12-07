using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;

namespace DotNetNuke.Tests.Website.DesktopModules.Admin.Security
{
    public partial class Steps
    {
        [Given(@"I select Add New User from the Users Action Menu")]
        public void GivenISelectAddNewUserFromTheUsersActionMenu()
        {
            UI.AddNewUserActionMenuItem(Driver).Click();
        }
    }
}
