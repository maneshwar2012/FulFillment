#region Copyright
// 
// DotNetNukeŽ - http://www.dotnetnuke.com
// Copyright (c) 2002-2012
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion
#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Users;
using DotNetNuke.Instrumentation;
using DotNetNuke.Security;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Security.Roles;
using DotNetNuke.Security.Roles.Internal;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;


#endregion

namespace DotNetNuke.Web.UI
{
    public class RibbonBarManager
    {
        public static TabInfo InitTabInfoObject()
        {
            return InitTabInfoObject(null, TabRelativeLocation.AFTER);
        }

        public static TabInfo InitTabInfoObject(TabInfo relativeToTab)
        {
            return InitTabInfoObject(relativeToTab, TabRelativeLocation.AFTER);
        }

        public static TabInfo InitTabInfoObject(TabInfo relativeToTab, TabRelativeLocation location)
        {
            TabController tabCtrl = new TabController();
            if (((relativeToTab == null)))
            {
                if (((PortalSettings.Current != null) && (PortalSettings.Current.ActiveTab != null)))
                {
                    relativeToTab = PortalSettings.Current.ActiveTab;
                }
            }

            TabInfo newTab = new TabInfo();
            newTab.TabID = Null.NullInteger;
            newTab.TabName = "";
            newTab.Title = "";
            newTab.IsVisible = false;
            newTab.DisableLink = false;
            newTab.IsDeleted = false;
            newTab.IsSecure = false;
            newTab.PermanentRedirect = false;

            TabInfo parentTab = GetParentTab(relativeToTab, location);

            if (((parentTab != null)))
            {
                newTab.PortalID = parentTab.PortalID;
                newTab.ParentId = parentTab.TabID;
                newTab.Level = parentTab.Level + 1;
                if ((PortalSettings.Current.SSLEnabled))
                {
                    newTab.IsSecure = parentTab.IsSecure;
                    //Inherit from parent
                }
            }
            else
            {
                newTab.PortalID = PortalSettings.Current.PortalId;
                newTab.ParentId = Null.NullInteger;
                newTab.Level = 0;
            }

            //Inherit permissions from parent
            newTab.TabPermissions.Clear();
            if ((newTab.PortalID != Null.NullInteger && (parentTab != null)))
            {
                newTab.TabPermissions.AddRange(parentTab.TabPermissions);
            }
            else if ((newTab.PortalID != Null.NullInteger))
            {
                //Give admin full permission
                ArrayList permissions = PermissionController.GetPermissionsByTab();

                foreach (PermissionInfo permission in permissions)
                {
                    TabPermissionInfo newTabPermission = new TabPermissionInfo();
                    newTabPermission.PermissionID = permission.PermissionID;
                    newTabPermission.PermissionKey = permission.PermissionKey;
                    newTabPermission.PermissionName = permission.PermissionName;
                    newTabPermission.AllowAccess = true;
                    newTabPermission.RoleID = PortalSettings.Current.AdministratorRoleId;
                    newTab.TabPermissions.Add(newTabPermission);
                }
            }

            return newTab;
        }

        public static TabInfo GetParentTab(TabInfo relativeToTab, TabRelativeLocation location)
        {
            if (((relativeToTab == null)))
            {
                return null;
            }

            TabController tabCtrl = new TabController();
            TabInfo parentTab = null;
            if ((location == TabRelativeLocation.CHILD))
            {
                parentTab = relativeToTab;
            }
            else if (((relativeToTab != null) && relativeToTab.ParentId != Null.NullInteger))
            {
                parentTab = tabCtrl.GetTab(relativeToTab.ParentId, relativeToTab.PortalID, false);
            }

            return parentTab;
        }

        public static IList<TabInfo> GetPagesList()
        {
            IList<TabInfo> portalTabs = null;
            UserInfo userInfo = UserController.GetCurrentUserInfo();
            if (((userInfo != null) && userInfo.UserID != Null.NullInteger))
            {
                TabController tabCtrl = new TabController();
                if ((userInfo.IsSuperUser && PortalSettings.Current.ActiveTab.IsSuperTab))
                {
                    portalTabs = tabCtrl.GetTabsByPortal(Null.NullInteger).AsList();
                }
                else
                {
                    portalTabs = TabController.GetPortalTabs(PortalSettings.Current.PortalId, Null.NullInteger, false, Null.NullString, true, false, true, false, true);
                }
            }

            if (((portalTabs == null)))
            {
                portalTabs = new List<TabInfo>();
            }

            return portalTabs;
        }

        public static bool IsHostConsolePage()
        {
            return (PortalSettings.Current.ActiveTab.IsSuperTab && PortalSettings.Current.ActiveTab.TabPath == "//Host");
        }

        public static bool IsHostConsolePage(TabInfo tab)
        {
            return (tab.IsSuperTab && tab.TabPath == "//Host");
        }

        public static bool CanMovePage()
        {
            //Cannot move the host console page
            if ((IsHostConsolePage()))
            {
                return false;
            }

            //Page Editors - Can only move children they have 'Manage' permission to, they cannot move the top level page
            if ((!PortalSecurity.IsInRole("Administrators")))
            {
                int parentTabID = PortalSettings.Current.ActiveTab.ParentId;
                if ((parentTabID == Null.NullInteger))
                {
                    return false;
                }

                TabInfo parentTab = new TabController().GetTab(parentTabID, PortalSettings.Current.ActiveTab.PortalID, false);
                string permissionList = "MANAGE";
                if ((!TabPermissionController.HasTabPermission(parentTab.TabPermissions, permissionList)))
                {
                    return false;
                }
            }

            return true;
        }

        //todo: Settings
        //Public Function SaveTabInfoObject(ByVal newTab As DotNetNuke.Entities.Tabs.TabInfo, ByVal relativeToTab As DotNetNuke.Entities.Tabs.TabInfo, ByVal location As TabRelativeLocation, ByVal templateMapPath As String, ByVal tabSettings As Hashtable) As Integer
        public static int SaveTabInfoObject(TabInfo tab, TabInfo relativeToTab, TabRelativeLocation location, string templateMapPath)
        {
            TabController tabCtrl = new TabController();

            //Validation:
            //Tab name is required
            //Tab name is invalid
            if ((tab.TabName == string.Empty))
            {
                throw new DotNetNukeException("Page name is required.", DotNetNukeErrorCode.PageNameRequired);
            }
            else if ((Regex.IsMatch(tab.TabName, "^LPT[1-9]$|^COM[1-9]$", RegexOptions.IgnoreCase)))
            {
                throw new DotNetNukeException("Page name is invalid.", DotNetNukeErrorCode.PageNameInvalid);
            }
            else if ((Regex.IsMatch(HtmlUtils.StripNonWord(tab.TabName, false), "^AUX$|^CON$|^NUL$|^SITEMAP$|^LINKCLICK$|^KEEPALIVE$|^DEFAULT$|^ERRORPAGE$", RegexOptions.IgnoreCase)))
            {
                throw new DotNetNukeException("Page name is invalid.", DotNetNukeErrorCode.PageNameInvalid);
            }
            else if ((Validate_IsCircularReference(tab.PortalID, tab.TabID)))
            {
                throw new DotNetNukeException("Cannot move page to that location.", DotNetNukeErrorCode.PageCircularReference);
            }

            bool usingDefaultLanguage = (tab.CultureCode == PortalSettings.Current.DefaultLanguage) || tab.CultureCode == null;

            if (PortalSettings.Current.ContentLocalizationEnabled)
            {
                if ((!usingDefaultLanguage))
                {
                    TabInfo defaultLanguageSelectedTab = tab.DefaultLanguageTab;

                    if ((defaultLanguageSelectedTab == null))
                    {
                        //get the siblings from the selectedtab and iterate through until you find a sibbling with a corresponding defaultlanguagetab
                        //if none are found get a list of all the tabs from the default language and then select the last one
                        var selectedTabSibblings = tabCtrl.GetTabsByPortal(tab.PortalID).WithCulture(tab.CultureCode, true).AsList();
                        foreach (TabInfo sibling in selectedTabSibblings)
                        {
                            TabInfo siblingDefaultTab = sibling.DefaultLanguageTab;
                            if (((siblingDefaultTab != null)))
                            {
                                defaultLanguageSelectedTab = siblingDefaultTab;
                                break;
                            }
                        }

                        //still haven't found it
                        if ((defaultLanguageSelectedTab == null))
                        {
                            var defaultLanguageTabs = tabCtrl.GetTabsByPortal(tab.PortalID).WithCulture(PortalSettings.Current.DefaultLanguage, true).AsList();
                            defaultLanguageSelectedTab = defaultLanguageTabs[defaultLanguageTabs.Count];
                            //get the last tab
                        }
                    }

                    relativeToTab = defaultLanguageSelectedTab;
                }
            }


            if ((location != TabRelativeLocation.NOTSET))
            {
                //Check Host tab - don't allow adding before or after
                if ((IsHostConsolePage(relativeToTab) && (location == TabRelativeLocation.AFTER || location == TabRelativeLocation.BEFORE)))
                {
                    throw new DotNetNukeException("You cannot add or move pages before or after the Host tab.", DotNetNukeErrorCode.HostBeforeAfterError);
                }

                TabInfo parentTab = GetParentTab(relativeToTab, location);
                string permissionList = "ADD,COPY,EDIT,MANAGE";
                //Check permissions for Page Editors when moving or inserting
                if ((!PortalSecurity.IsInRole("Administrators")))
                {
                    if (((parentTab == null) || !TabPermissionController.HasTabPermission(parentTab.TabPermissions, permissionList)))
                    {
                        throw new DotNetNukeException("You do not have permissions to add or move pages to this location. You can only add or move pages as children of pages you can edit.",
                                                      DotNetNukeErrorCode.PageEditorPermissionError);
                    }
                }

                if (((parentTab != null)))
                {
                    tab.ParentId = parentTab.TabID;
                    tab.Level = parentTab.Level + 1;
                }
                else
                {
                    tab.ParentId = Null.NullInteger;
                    tab.Level = 0;
                }
            }

            if ((tab.TabID > Null.NullInteger && tab.TabID == tab.ParentId))
            {
                throw new DotNetNukeException("Parent page is invalid.", DotNetNukeErrorCode.ParentTabInvalid);
            }

            tab.TabPath = Globals.GenerateTabPath(tab.ParentId, tab.TabName);
            //check whether have conflict between tab path and portal alias.
            if(TabController.IsDuplicateWithPortalAlias(PortalSettings.Current.PortalId, tab.TabPath))
            {
                throw new DotNetNukeException("The page path is duplicate with a site alias", DotNetNukeErrorCode.DuplicateWithAlias);
            }

            try
            {
                if ((tab.TabID < 0))
                {
                    if ((tab.TabPermissions.Count == 0 && tab.PortalID != Null.NullInteger))
                    {
                        //Give admin full permission
                        ArrayList permissions = PermissionController.GetPermissionsByTab();

                        foreach (PermissionInfo permission in permissions)
                        {
                            TabPermissionInfo newTabPermission = new TabPermissionInfo();
                            newTabPermission.PermissionID = permission.PermissionID;
                            newTabPermission.PermissionKey = permission.PermissionKey;
                            newTabPermission.PermissionName = permission.PermissionName;
                            newTabPermission.AllowAccess = true;
                            newTabPermission.RoleID = PortalSettings.Current.AdministratorRoleId;
                            tab.TabPermissions.Add(newTabPermission);
                        }
                    }

                    PortalSettings _PortalSettings = PortalController.GetCurrentPortalSettings();

                    if (_PortalSettings.ContentLocalizationEnabled)
                    {
                        Locale defaultLocale = LocaleController.Instance.GetDefaultLocale(tab.PortalID);
                        tab.CultureCode = defaultLocale.Code;
                    }
                    else
                    {
                        tab.CultureCode = Null.NullString;
                    }

                    if ((location == TabRelativeLocation.AFTER && (relativeToTab != null)))
                    {
                        tab.TabID = tabCtrl.AddTabAfter(tab, relativeToTab.TabID);
                    }
                    else if ((location == TabRelativeLocation.BEFORE && (relativeToTab != null)))
                    {
                        tab.TabID = tabCtrl.AddTabBefore(tab, relativeToTab.TabID);
                    }
                    else
                    {
                        tab.TabID = tabCtrl.AddTab(tab);
                    }

                    if (_PortalSettings.ContentLocalizationEnabled)
                    {
                        tabCtrl.CreateLocalizedCopies(tab);
                    }

                    tabCtrl.UpdateTabSetting(tab.TabID, "CacheProvider", "");
                    tabCtrl.UpdateTabSetting(tab.TabID, "CacheDuration", "");
                    tabCtrl.UpdateTabSetting(tab.TabID, "CacheIncludeExclude", "0");
                    tabCtrl.UpdateTabSetting(tab.TabID, "IncludeVaryBy", "");
                    tabCtrl.UpdateTabSetting(tab.TabID, "ExcludeVaryBy", "");
                    tabCtrl.UpdateTabSetting(tab.TabID, "MaxVaryByCount", "");
                }
                else
                {
                    tabCtrl.UpdateTab(tab);

                    if ((location == TabRelativeLocation.AFTER && (relativeToTab != null)))
                    {
                        tabCtrl.MoveTabAfter(tab, relativeToTab.TabID);
                    }
                    else if ((location == TabRelativeLocation.BEFORE && (relativeToTab != null)))
                    {
                        tabCtrl.MoveTabBefore(tab, relativeToTab.TabID);
                    }
                }
            }
            catch (Exception ex)
            {
                DnnLog.Error(ex);

                if (ex.Message.StartsWith("Page Exists"))
                {
                    throw new DotNetNukeException(ex.Message, DotNetNukeErrorCode.PageExists);
                }
            }

            // create the page from a template
            if ((!string.IsNullOrEmpty(templateMapPath)))
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.Load(templateMapPath);
                    TabController.DeserializePanes(xmlDoc.SelectSingleNode("//portal/tabs/tab/panes"), tab.PortalID, tab.TabID, PortalTemplateModuleAction.Ignore, new Hashtable());
                    
                    //save tab permissions
                    DeserializeTabPermissions(xmlDoc.SelectNodes("//portal/tabs/tab/tabpermissions/permission"), tab);
                }
                catch (Exception ex)
                {
                    Exceptions.LogException(ex);
                    throw new DotNetNukeException("Unable to process page template.", ex, DotNetNukeErrorCode.DeserializePanesFailed);
                }
            }

            //todo: reload tab from db or send back tabid instead?
            return tab.TabID;
        }

        public static bool Validate_IsCircularReference(int portalID, int tabID)
        {
            if (tabID != -1)
            {
                TabController objTabs = new TabController();
                TabInfo objtab = objTabs.GetTab(tabID, portalID, false);

                if (((objtab == null)))
                {
                    return false;
                }
                else if (objtab.Level == 0)
                {
                    return false;
                }
                else
                {
                    if (tabID == objtab.ParentId)
                    {
                        return true;
                    }
                    else
                    {
                        return Validate_IsCircularReference(portalID, objtab.ParentId);
                    }
                }
            }
            else
            {
                return false;
            }
        }

        public static void DeserializeTabPermissions(XmlNodeList nodeTabPermissions, TabInfo tab)
        {
            var permissionController = new PermissionController();
            foreach (XmlNode xmlTabPermission in nodeTabPermissions)
            {
                var permissionKey = XmlUtils.GetNodeValue(xmlTabPermission.CreateNavigator(), "permissionkey");
                var permissionCode = XmlUtils.GetNodeValue(xmlTabPermission.CreateNavigator(), "permissioncode");
                var roleName = XmlUtils.GetNodeValue(xmlTabPermission.CreateNavigator(), "rolename");
                var allowAccess = XmlUtils.GetNodeValueBoolean(xmlTabPermission, "allowaccess");
                var permissions = permissionController.GetPermissionByCodeAndKey(permissionCode, permissionKey);
                var permissionId = permissions.Cast<PermissionInfo>().Last().PermissionID;

                var roleId = int.MinValue;
                switch (roleName)
                {
                    case Globals.glbRoleAllUsersName:
                        roleId = Convert.ToInt32(Globals.glbRoleAllUsers);
                        break;
                    case Globals.glbRoleUnauthUserName:
                        roleId = Convert.ToInt32(Globals.glbRoleUnauthUser);
                        break;
                    default:
                        var portalController = new PortalController();
                        var portal = portalController.GetPortal(tab.PortalID);
                        var role = TestableRoleController.Instance.GetRole(portal.PortalID, r => r.RoleName == roleName);
                        if (role != null)
                        {
                            roleId = role.RoleID;
                        }
                        break;
                }
                if (roleId != int.MinValue &&
                        !tab.TabPermissions.Cast<TabPermissionInfo>().Any(p =>
                                                                            p.RoleID == roleId
                                                                            && p.PermissionID == permissionId))
                {
                    var tabPermission = new TabPermissionInfo
                    {
                        TabID = tab.TabID,
                        PermissionID = permissionId,
                        RoleID = roleId,
                        AllowAccess = allowAccess
                    };

                    tab.TabPermissions.Add(tabPermission);
                }
            }

            new TabController().UpdateTab(tab);
        }
    }

    public class DotNetNukeException : Exception
    {
        private readonly DotNetNukeErrorCode _ErrorCode = DotNetNukeErrorCode.NotSet;

        public DotNetNukeException()
        {
        }

        public DotNetNukeException(string message) : base(message)
        {
        }

        public DotNetNukeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public DotNetNukeException(string message, DotNetNukeErrorCode errorCode) : base(message)
        {
            _ErrorCode = errorCode;
        }

        public DotNetNukeException(string message, Exception innerException, DotNetNukeErrorCode errorCode) : base(message, innerException)
        {
            _ErrorCode = errorCode;
        }

        public DotNetNukeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public DotNetNukeErrorCode ErrorCode
        {
            get
            {
                return _ErrorCode;
            }
        }
    }

    public enum DotNetNukeErrorCode
    {
        NotSet,
        PageExists,
        PageNameRequired,
        PageNameInvalid,
        DeserializePanesFailed,
        PageCircularReference,
        ParentTabInvalid,
        PageEditorPermissionError,
        HostBeforeAfterError,
        DuplicateWithAlias
    }

    public enum TabRelativeLocation
    {
        NOTSET,
        BEFORE,
        AFTER,
        CHILD
    }
}