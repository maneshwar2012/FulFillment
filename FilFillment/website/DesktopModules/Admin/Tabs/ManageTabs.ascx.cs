#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
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
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;

using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Users;
using DotNetNuke.Framework;
using DotNetNuke.Security;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.OutputCache;
using DotNetNuke.Services.Social.Notifications;
using DotNetNuke.UI.Skins;
using DotNetNuke.UI.Skins.Controls;
using DotNetNuke.Web.UI;
using DotNetNuke.Web.UI.WebControls;
using DotNetNuke.Web.UI.WebControls.Extensions;

using DataCache = DotNetNuke.Common.Utilities.DataCache;
using Globals = DotNetNuke.Common.Globals;
using Reflection = DotNetNuke.Framework.Reflection;

#endregion

namespace DotNetNuke.Modules.Admin.Tabs
{
    /// <summary>
    ///   The ManageTabs PortalModuleBase is used to manage a Tab/Page
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <history>
    ///   [cnurse]	9/10/2004	Updated to reflect design changes for Help, 508 support
    ///   and localisation
    /// </history>
    public partial class ManageTabs : PortalModuleBase
    {
        private TabInfo _tab;
        private string _strAction = "";

        #region Protected Properties

        protected TabInfo Tab
        {
            get
            {
                if (_tab == null)
                {
                    switch (_strAction)
                    {
                        case "":
                        case "add":
                        case "copy":
                            _tab = new TabInfo { TabID = Null.NullInteger, PortalID = PortalId };
                            break;
                        default:
                            var objTabs = new TabController();
                            _tab = objTabs.GetTab(TabId, PortalId, true);
                            break;
                    }
                }
                return _tab;
            }
        }

        protected string ActiveDnnTab
        {
            get
            {
                var activeTab = Request.QueryString["activeTab"];
                if (!string.IsNullOrEmpty(activeTab))
                {
                    var tabControl = FindControl(activeTab);
                    if (tabControl != null)
                    {
                        return tabControl.ClientID;
                    }
                }

                return string.Empty;
            }
        }

        #endregion

        #region Private Methods

        private void AddTranslationSubmittedNotification(TabInfo tabInfo, UserInfo translator)
        {
            var notificationType = NotificationsController.Instance.GetNotificationType("TranslationSubmitted");

            var subject = Localization.GetString("NewContentMessage.Subject", LocalResourceFile);
            var body = string.Format(Localization.GetString("NewContentMessage.Body", LocalResourceFile),
                tabInfo.TabName,
                Globals.NavigateURL(tabInfo.TabID, false, PortalSettings, Null.NullString, tabInfo.CultureCode, new string[] { }),
                txtTranslationComment.Text);

            var sender = UserController.GetUserById(PortalSettings.PortalId, PortalSettings.AdministratorId);

            var notification = new Notification { NotificationTypeID = notificationType.NotificationTypeId, Subject = subject, Body = body, IncludeDismissAction = true, SenderUserID = sender.UserID};
            
            NotificationsController.Instance.SendNotification(notification, PortalSettings.PortalId, null, new List<UserInfo> { translator });
        }
        
        private void BindBeforeAfterTabControls()
        {
            List<TabInfo> listTabs = null;
            TabInfo parentTab = null;

            if (cboParentTab.SelectedItem != null)
            {
                var parentTabID = Int32.Parse(cboParentTab.SelectedItem.Value);
                var controller = new TabController();
                parentTab = controller.GetTab(parentTabID, -1, false);
            }

            if (parentTab != null)
            {
                var parentTabCulture = parentTab.CultureCode;
                if (string.IsNullOrEmpty(parentTabCulture))
                {
                    parentTabCulture = PortalController.GetActivePortalLanguage(PortalId);
                }
                listTabs = new TabController().GetTabsByPortal(parentTab.PortalID).WithCulture(parentTabCulture, true).WithParentId(parentTab.TabID);
            }
            else
            {
                listTabs = new TabController().GetTabsByPortal(PortalId).WithCulture(PortalController.GetActivePortalLanguage(PortalId), true).WithParentId(Null.NullInteger);
            }
            listTabs = TabController.GetPortalTabs(listTabs, Null.NullInteger, false, Null.NullString, false, false, false, false, true);
            cboPositionTab.DataSource = listTabs;
            cboPositionTab.DataBind();

            rbInsertPosition.Items.Clear();
            rbInsertPosition.Items.Add(new ListItem(Localization.GetString("InsertBefore", LocalResourceFile), "Before"));
            rbInsertPosition.Items.Add(new ListItem(Localization.GetString("InsertAfter", LocalResourceFile), "After"));
            rbInsertPosition.Items.Add(new ListItem(Localization.GetString("InsertAtEnd", LocalResourceFile), "AtEnd"));
            rbInsertPosition.SelectedValue = "After";

            if (parentTab != null && parentTab.IsSuperTab)
            {
                ShowPermissions(false);
            }
            else
            {
                ShowPermissions(true);
            }
        }

        private void BindLocalization(bool rebind)
        {
            cultureLanguageLabel.Language = Tab.CultureCode;

            if (Tab.IsNeutralCulture)
            {
                defaultCultureMessageLabel.Visible = false;
                defaultCultureMessage.Visible = false;
                localizedTabsRow.Visible = false;
                localizedModulesRow.Visible = false;
                defaultCultureRow.Visible = false;
                translatedRow.Visible = false;
                localizePagesButton.Visible = true;
                if (string.IsNullOrEmpty(_strAction) || _strAction == "add" || _strAction == "copy")
                {
                    cultureRow.Visible = false;
                    cultureTypeRow.Visible = true;
                }
            }
            else if (Tab.DefaultLanguageTab != null)
            {
                defaultCultureMessageLabel.Visible = false;
                defaultCultureMessage.Visible = false;
                defaultCultureLanguageLabel.Language = Tab.DefaultLanguageTab.CultureCode;

                viewDefaultCultureButton.CommandArgument = Tab.DefaultLanguageTab.TabID.ToString();
                viewDefaultCultureButton.Visible = TabPermissionController.CanViewPage(Tab.DefaultLanguageTab);
                editDefaultCultureButton.CommandArgument = Tab.DefaultLanguageTab.TabID.ToString();
                editDefaultCultureButton.Visible = TabPermissionController.CanManagePage(Tab.DefaultLanguageTab);

                defaultCultureRow.Visible = true;
                localizedTabsRow.Visible = false;
                translatedRow.Visible = true;
                if (!IsPostBack)
                {
                    translatedCheckbox.Checked = Tab.IsTranslated;
                }
                moduleLocalization.ShowLanguageColumn = false;

                var locale = LocaleController.Instance.GetLocale(PortalId, Tab.CultureCode);
                if (locale != null && locale.IsPublished)
                {
                    //Dim msg As String = Localization.GetString("Publish.Confirm", Me.LocalResourceFile)
                    //publishPageButton.OnClientClick = DotNetNuke.Web.UI.Utilities.GetOnClientClickConfirm(publishPageButton, msg)
                    publishRow.Visible = true;
                }
            }
            else
            {
                defaultCultureMessageLabel.Visible = true;
                defaultCultureMessage.Visible = true;
                tabLocalization.ToLocalizeTabId = TabId;
                if (!IsPostBack)
                {
                    tabLocalization.DataBind();
                }
                localizedTabsRow.Visible = true;
                defaultCultureRow.Visible = false;
                translatedRow.Visible = false;
                readyForTranslationRow.Visible = true;
            }

            if (rebind || (!Page.IsPostBack))
            {
                moduleLocalization.DataBind();
            }
        }

        private void BindPageDetails()
        {
            if (_strAction != "copy")
            {
                txtTabName.Text = Tab.TabName;
                txtTitle.Text = Tab.Title;
                txtDescription.Text = Tab.Description;
                txtKeyWords.Text = Tab.KeyWords;
            }
        }

        private void BindSkins()
        {
            var portalController = new PortalController();
            var portal = portalController.GetPortal(Tab.PortalID);
            var skins = SkinController.GetSkins(portal, SkinController.RootSkin, SkinScope.All)
                                                     .ToDictionary(skin => skin.Key, skin => skin.Value);
            var containers = SkinController.GetSkins(portal, SkinController.RootContainer, SkinScope.All)
                                                    .ToDictionary(skin => skin.Key, skin => skin.Value);
            pageSkinCombo.DataSource = skins;
            pageSkinCombo.DataBind(Tab.SkinSrc);
            pageSkinCombo.InsertItem(0, "<" + Localization.GetString("None_Specified") + ">", "");
            pageSkinCombo.Select(Tab.SkinSrc, false);

            pageContainerCombo.DataSource = containers;
            pageContainerCombo.DataBind();
            pageContainerCombo.InsertItem(0, "<" + Localization.GetString("None_Specified") + ">", "");
            pageContainerCombo.Select(Tab.ContainerSrc, false);
        }

        private void BindTab()
        {
            //Load TabControls
            BindTabControls(Tab);

            if (Tab != null)
            {
                BindPageDetails();

                if (_strAction != "copy")
                {
                    ctlURL.Url = Tab.Url;
                    bool newWindow = false;
                    if(Tab.TabSettings["LinkNewWindow"] != null && Boolean.TryParse((string)Tab.TabSettings["LinkNewWindow"], out newWindow))
                    {
                        ctlURL.NewWindow = newWindow;
                    }
                }
                ctlIcon.Url = Tab.IconFileRaw;
                ctlIconLarge.Url = Tab.IconFileLargeRaw;
                chkMenu.Checked = Tab.IsVisible;

                chkDisableLink.Checked = Tab.DisableLink;
                if (TabId == PortalSettings.AdminTabId || TabId == PortalSettings.SplashTabId || TabId == PortalSettings.HomeTabId || TabId == PortalSettings.LoginTabId ||
                    TabId == PortalSettings.UserTabId || TabId == PortalSettings.SuperTabId)
                {
                    chkDisableLink.Enabled = false;
                }

                BindSkins();

                if (PortalSettings.SSLEnabled)
                {
                    chkSecure.Enabled = true;
                    chkSecure.Checked = Tab.IsSecure;
                }
                else
                {
                    chkSecure.Enabled = false;
                    chkSecure.Checked = Tab.IsSecure;
                }
                txtPriority.Text = Tab.SiteMapPriority.ToString();


                if (!Null.IsNull(Tab.StartDate))
                {
                    datepickerStartDate.SelectedDate = Tab.StartDate;
                }
                if (!Null.IsNull(Tab.EndDate))
                {
                    datepickerEndDate.SelectedDate = Tab.EndDate;
                }

                datepickerStartDate.MinDate = DateTime.Now;
                datepickerEndDate.MinDate = DateTime.Now;

                if (Tab.RefreshInterval != Null.NullInteger)
                {
                    txtRefreshInterval.Text = Tab.RefreshInterval.ToString();
                }

                txtPageHeadText.Text = Tab.PageHeadText;
                chkPermanentRedirect.Checked = Tab.PermanentRedirect;

                ShowPermissions(!Tab.IsSuperTab && TabPermissionController.CanAdminPage());
                ctlAudit.Entity = Tab;

                termsSelector.PortalId = Tab.PortalID;
                termsSelector.Terms = Tab.Terms;
                termsSelector.DataBind();
            }

            if (string.IsNullOrEmpty(_strAction) || _strAction == "add" || _strAction == "copy")
            {
                InitializeTab();
            }

            // copy page options
            cboCopyPage.DataSource = GetTabs(true, false, false, true);
            cboCopyPage.DataBind();
            modulesRow.Visible = false;

            switch (_strAction)
            {
                case "copy":
                    if ((cboCopyPage.FindItemByValue(TabId.ToString()) != null))
                    {
                        cboCopyPage.ClearSelection();
                        cboCopyPage.FindItemByValue(TabId.ToString()).Selected = true;
                        DisplayTabModules();
                    }
                    break;
            }
        }

        private void BindTabControls(TabInfo tab)
        {
            cboParentTab.DataSource = string.IsNullOrEmpty(_strAction) || _strAction == "copy" || _strAction == "add"
                                        ? GetTabs(true, true, false, true)
                                        : GetTabs(false, true, true, false);
            cboParentTab.DataBind();

            if ((string.IsNullOrEmpty(_strAction) || _strAction == "copy" || _strAction == "add")
                    && !UserInfo.IsSuperUser && !UserInfo.IsInRole(PortalSettings.AdministratorRoleName))
            {
                //cboParentTab.Select(PortalSettings.ActiveTab.TabID.ToString(), false, 0);
                if (cboParentTab.FindItemByValue(PortalSettings.ActiveTab.TabID.ToString()) != null)
                {
                    cboParentTab.FindItemByValue(PortalSettings.ActiveTab.TabID.ToString()).Selected = true;
                }
            }
            else
            {
                //cboParentTab.Select(PortalSettings.ActiveTab.ParentId.ToString(), false, 0);
                if (cboParentTab.FindItemByValue(PortalSettings.ActiveTab.ParentId.ToString()) != null)
                {
                    cboParentTab.FindItemByValue(PortalSettings.ActiveTab.ParentId.ToString()).Selected = true;
                }
            }

            // tab administrators can only create children of the current tab
            if (string.IsNullOrEmpty(_strAction) || _strAction == "add" || _strAction == "copy")
            {
                BindBeforeAfterTabControls();
                insertPositionRow.Visible = cboPositionTab.Items.Count > 0;
                cboParentTab.AutoPostBack = true;

                cultureTypeList.SelectedValue = "Localized";
            }
            else
            {
                DisablePositionDropDown();
            }

            // if editing a tab, load tab parent so parent link is not lost
            // parent tab might not be loaded in cbotab if user does not have edit rights on it
            if (!TabPermissionController.CanAdminPage() && (tab != null))
            {
                if (cboParentTab.FindItemByValue(tab.ParentId.ToString()) == null)
                {
                    var objtabs = new TabController();
                    var objparent = objtabs.GetTab(tab.ParentId, tab.PortalID, false);
                    if (objparent != null)
                    {
                        //cboParentTab.Items.Add(new ListItem(objparent.LocalizedTabName, objparent.TabID.ToString()));
                        cboParentTab.AddItem(objparent.LocalizedTabName, objparent.TabID.ToString());
                    }
                }
            }

            cboCacheProvider.DataSource = OutputCachingProvider.GetProviderList();
            cboCacheProvider.DataBind();
            //cboCacheProvider.Items.Insert(0, new ListItem(Localization.GetString("None_Specified"), ""));
            cboCacheProvider.InsertItem(0, Localization.GetString("None_Specified"), "");
            if (tab == null)
            {
                cboCacheProvider.ClearSelection();
                cboCacheProvider.Items[0].Selected = true;
                rblCacheIncludeExclude.ClearSelection();
                rblCacheIncludeExclude.Items[0].Selected = true;
            }

            var tabController = new TabController();
            var tabSettings = tabController.GetTabSettings(TabId);
            SetValue(cboCacheProvider, tabSettings, "CacheProvider");
            SetValue(txtCacheDuration, tabSettings, "CacheDuration");
            SetValue(rblCacheIncludeExclude, tabSettings, "CacheIncludeExclude");
            SetValue(txtIncludeVaryBy, tabSettings, "IncludeVaryBy");
            SetValue(txtExcludeVaryBy, tabSettings, "ExcludeVaryBy");
            SetValue(txtMaxVaryByCount, tabSettings, "MaxVaryByCount");

            ShowCacheRows();
        }

        private void DisablePositionDropDown()
        {
            insertPositionRow.Visible = false;
            cboParentTab.AutoPostBack = false;
        }

        private void CheckLocalizationVisibility()
        {
            if (PortalSettings.ContentLocalizationEnabled && LocaleController.Instance.GetLocales(PortalId).Count > 1)
            {
                localizationTab.Visible = true;
                localizationPanel.Visible = true;
            }
            else
            {
                localizationTab.Visible = false;
                localizationPanel.Visible = false;
            }
        }

        private void CheckQuota()
        {
            if (PortalSettings.Pages < PortalSettings.PageQuota || UserInfo.IsSuperUser || PortalSettings.PageQuota == 0)
            {
                cmdUpdate.Enabled = true;
            }
            else
            {
                cmdUpdate.Enabled = false;
                UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("ExceededQuota", LocalResourceFile), ModuleMessage.ModuleMessageType.YellowWarning);
            }
        }

        private bool DeleteTab(int deleteTabId)
        {
            var bDeleted = Null.NullBoolean;
            if (TabPermissionController.CanDeletePage())
            {
                var tabController = new TabController();
                bDeleted = tabController.SoftDeleteTab(deleteTabId, PortalSettings);
                if (!bDeleted)
                {
                    UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("DeleteSpecialPage", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                }
            }
            else
            {
                UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("DeletePermissionError", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
            }

            return bDeleted;
        }

        private void DisplayTabModules()
        {
            switch (cboCopyPage.SelectedIndex)
            {
                case 0:
                    modulesRow.Visible = false;
                    break;
                default:
                    // selected tab
                    if (TabPermissionController.CanAddContentToPage())
                    {
                        grdModules.DataSource = LoadTabModules(int.Parse(cboCopyPage.SelectedItem.Value));
                        grdModules.DataBind();
                        modulesRow.Visible = true;
                    }
                    else
                    {
                        modulesRow.Visible = false;
                    }
                    break;
            }
        }

        private void GetHostTabs(List<TabInfo> tabs)
        {
            foreach (var kvp in new TabController().GetTabsByPortal(Null.NullInteger))
            {
                tabs.Add(kvp.Value);
            }
        }

        private List<TabInfo> GetTabs(bool includeCurrent, bool includeURL, bool includeParent, bool includeDescendants)
        {
            var noneSpecified = "<" + Localization.GetString("None_Specified") + ">";
            var tabs = new List<TabInfo>();
            var controller = new TabController();

            var excludeTabId = Null.NullInteger;
            if (!includeCurrent)
            {
                excludeTabId = PortalSettings.ActiveTab.TabID;
            }

            if (PortalSettings.ActiveTab.IsSuperTab)
            {
                var objTab = new TabInfo { TabID = -1, TabName = noneSpecified, TabOrder = 0, ParentId = -2 };
                tabs.Add(objTab);

                GetHostTabs(tabs);
            }
            else
            {
                //Add Non Specified if user is Admin or if current tab is already on the top level
                var addNoneSpecified = PortalSecurity.IsInRole("Administrators") || PortalSettings.ActiveTab.ParentId == Null.NullInteger;

                tabs = TabController.GetPortalTabs(PortalId, excludeTabId, addNoneSpecified, noneSpecified, true, false, includeURL, false, true);

                var parentTab = (from tab in tabs where tab.TabID == PortalSettings.ActiveTab.ParentId select tab).FirstOrDefault();

                //Need to include the Parent Tab if its not already in the list of tabs
                if (includeParent && PortalSettings.ActiveTab.ParentId != Null.NullInteger && parentTab == null)
                {
                    tabs.Add(controller.GetTab(PortalSettings.ActiveTab.ParentId, PortalId, false));
                }

                if (UserInfo.IsSuperUser && TabId == Null.NullInteger)
                {
                    GetHostTabs(tabs);
                }

                if (!includeDescendants)
                {
                    tabs = (from t in tabs where !t.TabPath.StartsWith(PortalSettings.ActiveTab.TabPath) && !t.TabPath.Equals(PortalSettings.ActiveTab.TabPath) select t).ToList();
                }
            }

            return tabs;
        }

        /// <summary>
        ///   InitializeTab loads the Controls with default Tab Data
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///   [cnurse]	9/10/2004	Updated to reflect design changes for Help, 508 support
        ///   and localisation
        /// </history>
        private void InitializeTab()
        {
            if ((cboPositionTab.FindItemByValue(TabId.ToString()) != null))
            {
                cboPositionTab.ClearSelection();
                cboPositionTab.FindItemByValue(TabId.ToString()).Selected = true;
            }

            //cboFolders.Items.Insert(0, new ListItem("<" + Localization.GetString("None_Specified") + ">", "-"));
            cboFolders.InsertItem(0, "<" + Localization.GetString("None_Specified") + ">", "-");
            var user = UserController.GetCurrentUserInfo();
            var folders = FolderManager.Instance.GetFileSystemFolders(user, "BROWSE, ADD");
            foreach (FolderInfo folder in folders)
            {
                var folderItem = new ListItem();
                if (folder.FolderPath == Null.NullString)
                {
                    folderItem.Text = Localization.GetString("Root", LocalResourceFile);
                }
                else
                {
                    folderItem.Text = folder.DisplayPath;
                }
                folderItem.Value = folder.FolderPath;
                //cboFolders.Items.Add(folderItem);
                cboFolders.AddItem(folderItem.Text, folderItem.Value);
                if (folderItem.Value == "Templates/")
                {
                    cboFolders.FindItemByValue("Templates/").Selected = true;
                    LoadTemplates();
                }
            }
        }

        /// <summary>
        ///   Checks if parent tab will cause a circular reference
        /// </summary>
        /// <param name = "intTabId">Tabid</param>
        /// <returns></returns>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///   [VMasanas]	28/11/2004	Created
        /// </history>
        private bool IsCircularReference(int intTabId, int portalId)
        {
            if (intTabId != -1)
            {
                var tabController = new TabController();
                var tabInfo = tabController.GetTab(intTabId, portalId, false);

                if (tabInfo.Level == 0)
                {
                    return false;
                }
                return TabId == tabInfo.ParentId || IsCircularReference(tabInfo.ParentId, portalId);
            }
            return false;
        }

        private List<ModuleInfo> LoadTabModules(int TabID)
        {
            var moduleCtl = new ModuleController();
            var moduleList = new List<ModuleInfo>();

            foreach (var m in moduleCtl.GetTabModules(TabID).Values)
            {
                if (TabPermissionController.CanAddContentToPage() && !m.IsDeleted && !m.AllTabs)
                {
                    moduleList.Add(m);
                }
            }

            return moduleList;
        }

        private void LoadTemplates()
        {
            cboTemplate.Items.Clear();
            if (cboFolders.SelectedIndex != 0)
            {
                var arrFiles = Globals.GetFileList(PortalId, "page.template", false, cboFolders.SelectedItem.Value);
                foreach (FileItem objFile in arrFiles)
                {
                    var fileItem = new ListItem { Text = objFile.Text.Replace(".page.template", ""), Value = objFile.Text };
                    //cboTemplate.Items.Add(fileItem);
                    cboTemplate.AddItem(fileItem.Text, fileItem.Value);
                    if (!Page.IsPostBack && fileItem.Text == "Default")
                    {
                        cboTemplate.ClearSelection();
                        cboTemplate.FindItemByText("Default").Selected = true;
                    }
                }
                
                //cboTemplate.Items.Insert(0, new ListItem(Localization.GetString("None_Specified"), "-1"));
                cboTemplate.InsertItem(0, Localization.GetString("None_Specified"), "-1");
                if (cboTemplate.SelectedIndex == -1)
                {
                    cboTemplate.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        ///   SaveTabData saves the Tab to the Database
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name = "strAction">The action to perform "edit" or "add"</param>
        /// <history>
        ///   [cnurse]	9/10/2004	Updated to reflect design changes for Help, 508 support
        ///   and localisation
        ///   [jlucarino]	2/26/2009	Added CreatedByUserID and LastModifiedByUserID
        /// </history>
        private int SaveTabData(string strAction)
        {
            string strIcon;
            string strIconLarge;
            strIcon = ctlIcon.Url;
            strIconLarge = ctlIconLarge.Url;

            var objTabs = new TabController();

            Tab.TabName = txtTabName.Text;
            Tab.Title = txtTitle.Text;
            Tab.Description = txtDescription.Text;
            Tab.KeyWords = txtKeyWords.Text;
            Tab.IsVisible = chkMenu.Checked;
            Tab.DisableLink = chkDisableLink.Checked;

            TabInfo parentTab = null;
            if (cboParentTab.SelectedItem != null)
            {
                var parentTabID = Int32.Parse(cboParentTab.SelectedItem.Value);
                var controller = new TabController();
                parentTab = controller.GetTab(parentTabID, -1, false);
            }

            if (parentTab != null)
            {
                Tab.PortalID = parentTab.PortalID;
                Tab.ParentId = parentTab.TabID;
            }
            else
            {
                Tab.ParentId = Null.NullInteger;
            }
            Tab.IconFile = strIcon;
            Tab.IconFileLarge = strIconLarge;
            Tab.IsDeleted = false;
            Tab.Url = ctlURL.Url;

            Tab.TabPermissions.Clear();
            if (Tab.PortalID != Null.NullInteger)
            {
                Tab.TabPermissions.AddRange(dgPermissions.Permissions);
            }

            Tab.Terms.Clear();
            Tab.Terms.AddRange(termsSelector.Terms);

            Tab.SkinSrc = pageSkinCombo.SelectedValue;
            Tab.ContainerSrc = pageContainerCombo.SelectedValue;
            Tab.TabPath = Globals.GenerateTabPath(Tab.ParentId, Tab.TabName);

            //Check for invalid 
            if (!IsValidTabName(Tab.TabName))
            {
                return Null.NullInteger;
            }

            //Validate Tab Path
            if (!IsValidTabPath(Tab, Tab.TabPath))
            {
                return Null.NullInteger;
            }

            //Set Culture Code
            var positionTabID = Null.NullInteger;
            if (cboPositionTab.SelectedItem != null)
            {
                positionTabID = Int32.Parse(cboPositionTab.SelectedItem.Value);
            }

            if (strAction != "edit")
            {
                if (PortalSettings.ContentLocalizationEnabled)
                {
                    switch (cultureTypeList.SelectedValue)
                    {
                        case "Localized":
                            var defaultLocale = LocaleController.Instance.GetDefaultLocale(PortalId);
                            Tab.CultureCode = defaultLocale.Code;
                            break;
                        case "Culture":
                            Tab.CultureCode = PortalSettings.CultureCode;
                            break;
                        default:
                            Tab.CultureCode = Null.NullString;
                            break;
                    }

                    var tabLocale = LocaleController.Instance.GetLocale(Tab.CultureCode) ?? LocaleController.Instance.GetDefaultLocale(PortalId);

                    //Fix parent 
                    if (Tab.ParentId > Null.NullInteger)
                    {
                        parentTab = objTabs.GetTab(Tab.ParentId, PortalId, false);
                        if (parentTab.CultureCode != Tab.CultureCode)
                        {
                            parentTab = objTabs.GetTabByCulture(Tab.ParentId, PortalId, tabLocale);
                        }
                        Tab.ParentId = parentTab.TabID;
                    }

                    //Fix position TabId
                    if (positionTabID > Null.NullInteger)
                    {
                        var positionTab = objTabs.GetTab(positionTabID, PortalId, false);
                        if (positionTab.CultureCode != Tab.CultureCode)
                        {
                            positionTab = objTabs.GetTabByCulture(positionTabID, PortalId, tabLocale);
                        }
                        positionTabID = positionTab.TabID;
                    }
                }
                else
                {
                    Tab.CultureCode = Null.NullString;
                }
            }

            //Validate Tab Path
            if (string.IsNullOrEmpty(strAction))
            {
                var tabID = TabController.GetTabByTabPath(Tab.PortalID, Tab.TabPath, Tab.CultureCode);

                if (tabID != Null.NullInteger)
                {
                    var existingTab = objTabs.GetTab(tabID, PortalId, false);
                    if (existingTab != null && existingTab.IsDeleted)
                    {
                        UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("TabRecycled", LocalResourceFile), ModuleMessage.ModuleMessageType.YellowWarning);
                    }
                    else
                    {
                        UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("TabExists", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                    }
                    return Null.NullInteger;
                }
            }

            Tab.StartDate = datepickerStartDate.SelectedDate != null ? datepickerStartDate.SelectedDate.Value : Null.NullDate;
            Tab.EndDate = datepickerEndDate.SelectedDate != null ? datepickerEndDate.SelectedDate.Value : Null.NullDate;

            if (Tab.StartDate > Null.NullDate && Tab.EndDate > Null.NullDate && Tab.StartDate.AddDays(1) >= Tab.EndDate)
            {
                UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("InvalidTabDates", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                return Null.NullInteger;
            }
            if (txtRefreshInterval.Text.Length > 0 && Regex.IsMatch(txtRefreshInterval.Text, "^\\d+$"))
            {
                Tab.RefreshInterval = Convert.ToInt32(txtRefreshInterval.Text);
            }

            Tab.SiteMapPriority = float.Parse(txtPriority.Text);
            Tab.PageHeadText = txtPageHeadText.Text;
            Tab.IsSecure = chkSecure.Checked;
            Tab.PermanentRedirect = chkPermanentRedirect.Checked;

            if (strAction == "edit")
            {
                // trap circular tab reference
                if (cboParentTab.SelectedItem != null && Tab.TabID != Int32.Parse(cboParentTab.SelectedItem.Value) && !IsCircularReference(Int32.Parse(cboParentTab.SelectedItem.Value), Tab.PortalID))
                {
                    objTabs.UpdateTab(Tab);
                    if (IsHostMenu && Tab.PortalID != Null.NullInteger)
                    {
                        //Host Tab moved to Portal so clear Host cache
                        objTabs.ClearCache(Null.NullInteger);
                    }
                    if (!IsHostMenu && Tab.PortalID == Null.NullInteger)
                    {
                        //Portal Tab moved to Host so clear portal cache
                        objTabs.ClearCache(PortalId);
                    }
                    UpdateTabSettings(Tab.TabID);
                }
                // add or copy
            }
            else
            {
                if (positionTabID == Null.NullInteger)
                {
                    Tab.TabID = objTabs.AddTab(Tab);
                }
                else
                {
                    if (rbInsertPosition.SelectedValue == "After" && positionTabID > Null.NullInteger)
                    {
                        Tab.TabID = objTabs.AddTabAfter(Tab, positionTabID);
                    }
                    else if (rbInsertPosition.SelectedValue == "Before" && positionTabID > Null.NullInteger)
                    {
                        Tab.TabID = objTabs.AddTabBefore(Tab, positionTabID);
                    }
                    else
                    {
                        Tab.TabID = objTabs.AddTab(Tab);
                    }
                }

                UpdateTabSettings(Tab.TabID);

                //Create Localized versions
                if (PortalSettings.ContentLocalizationEnabled && cultureTypeList.SelectedValue == "Localized")
                {
                    objTabs.CreateLocalizedCopies(Tab);
                    //Refresh tab
                    _tab = objTabs.GetTab(Tab.TabID, Tab.PortalID, true);
                }

                var copyTabId = Int32.Parse(cboCopyPage.SelectedItem.Value);
                if (copyTabId != -1)
                {
                    var objModules = new ModuleController();
                    ModuleInfo objModule;
                    CheckBox chkModule;
                    RadioButton optCopy;
                    RadioButton optReference;
                    TextBox txtCopyTitle;

                    foreach (DataGridItem objDataGridItem in grdModules.Items)
                    {
                        chkModule = (CheckBox)objDataGridItem.FindControl("chkModule");
                        if (chkModule.Checked)
                        {
                            var intModuleID = Convert.ToInt32(grdModules.DataKeys[objDataGridItem.ItemIndex]);
                            optCopy = (RadioButton)objDataGridItem.FindControl("optCopy");
                            optReference = (RadioButton)objDataGridItem.FindControl("optReference");
                            txtCopyTitle = (TextBox)objDataGridItem.FindControl("txtCopyTitle");

                            objModule = objModules.GetModule(intModuleID, copyTabId, false);
                            ModuleInfo newModule = null;
                            if ((objModule != null))
                            {
                                //Clone module as it exists in the cache and changes we make will update the cached object
                                newModule = objModule.Clone();

                                if (!optReference.Checked)
                                {
                                    newModule.ModuleID = Null.NullInteger;
                                }

                                newModule.TabID = Tab.TabID;
                                newModule.DefaultLanguageGuid = Null.NullGuid;
                                newModule.CultureCode = Tab.CultureCode;
                                newModule.ModuleTitle = txtCopyTitle.Text;
                                newModule.ModuleID = objModules.AddModule(newModule);

                                if (optCopy.Checked)
                                {
                                    if (!string.IsNullOrEmpty(newModule.DesktopModule.BusinessControllerClass))
                                    {
                                        var objObject = Reflection.CreateObject(newModule.DesktopModule.BusinessControllerClass, newModule.DesktopModule.BusinessControllerClass);
                                        if (objObject is IPortable)
                                        {
                                            var content = Convert.ToString(((IPortable)objObject).ExportModule(intModuleID));
                                            if (!string.IsNullOrEmpty(content))
                                            {
                                                ((IPortable)objObject).ImportModule(newModule.ModuleID, content, newModule.DesktopModule.Version, UserInfo.UserID);
                                            }
                                        }
                                    }
                                }
                            }

                            if (optReference.Checked)
                            {
                                //Make reference copies on secondary language
                                foreach (var m in objModule.LocalizedModules.Values)
                                {
                                    var newLocalizedModule = m.Clone();
                                    var localizedTab = Tab.LocalizedTabs[m.CultureCode];
                                    newLocalizedModule.TabID = localizedTab.TabID;
                                    newLocalizedModule.CultureCode = localizedTab.CultureCode;
                                    newLocalizedModule.ModuleTitle = txtCopyTitle.Text;
                                    newLocalizedModule.DefaultLanguageGuid = newModule.UniqueId;
                                    newLocalizedModule.ModuleID = objModules.AddModule(newLocalizedModule);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // create the page from a template
                    if (cboTemplate.SelectedItem != null && cboTemplate.SelectedItem.Value != Null.NullInteger.ToString())
                    {
                        var xmlDoc = new XmlDocument();
                        try
                        {
                            // open the XML file
                            xmlDoc.Load(PortalSettings.HomeDirectoryMapPath + cboFolders.SelectedValue + cboTemplate.SelectedValue);
                        }
                        catch (Exception ex)
                        {
                            Exceptions.LogException(ex);

                            UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("BadTemplate", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                            return Null.NullInteger;
                        }
                        TabController.DeserializePanes(xmlDoc.SelectSingleNode("//portal/tabs/tab/panes"), Tab.PortalID, Tab.TabID, PortalTemplateModuleAction.Ignore, new Hashtable());
                        //save tab permissions
                        RibbonBarManager.DeserializeTabPermissions(xmlDoc.SelectNodes("//portal/tabs/tab/tabpermissions/permission"), Tab);
                        
                        var tabIndex = 0;
                        var exceptions = string.Empty;
                        foreach (XmlNode tabNode in xmlDoc.SelectSingleNode("//portal/tabs").ChildNodes)
                        {
                            //Create second tab onward tabs. Note first tab is already created above.
                            if(tabIndex > 0)
                            {
                                try
                                {
                                    TabController.DeserializeTab(tabNode, null, PortalId, PortalTemplateModuleAction.Replace);
                                }
                                catch (Exception ex)
                                {
                                    Exceptions.LogException(ex);
                                    exceptions += string.Format("Template Tab # {0}. Error {1}<br/>", tabIndex + 1, ex.Message);
                                }                                 
                            }                            
                            tabIndex++;
                        }

                        if (!string.IsNullOrEmpty(exceptions))
                        {
                            UI.Skins.Skin.AddModuleMessage(this, exceptions, ModuleMessage.ModuleMessageType.RedError);
                            return Null.NullInteger;
                        }                        
                    }
                }
            }

            // url tracking
            var objUrls = new UrlController();
            objUrls.UpdateUrl(PortalId, ctlURL.Url, ctlURL.UrlType, 0, Null.NullDate, Null.NullDate, ctlURL.Log, ctlURL.Track, Null.NullInteger, ctlURL.NewWindow);

            //Clear the Tab's Cached modules
            DataCache.ClearModuleCache(TabId);

            //Update Cached Tabs as TabPath may be needed before cache is cleared
            TabInfo tempTab;
            if (new TabController().GetTabsByPortal(PortalId).TryGetValue(Tab.TabID, out tempTab))
            {
                tempTab.TabPath = Tab.TabPath;
            }

            //Update Translation Status
            objTabs.UpdateTranslationStatus(Tab, translatedCheckbox.Checked);

            return Tab.TabID;
        }

        private static void SetValue(Control control, Hashtable tabSettings, string tabSettingsKey)
        {
            if (ReferenceEquals(control.GetType(), typeof(TextBox)))
            {
                ((TextBox)control).Text = string.IsNullOrEmpty(Convert.ToString(tabSettings[tabSettingsKey])) ? "" : tabSettings[tabSettingsKey].ToString();
            }
			else if (ReferenceEquals(control.GetType(), typeof(DnnComboBox)))
            {
                if (!string.IsNullOrEmpty(Convert.ToString(tabSettings[tabSettingsKey])))
                {
                    //((DropDownList)control).ClearSelection();
                    //((DropDownList)control).Items.FindByValue(tabSettings[tabSettingsKey].ToString()).Selected = true;
                    ((DnnComboBox)control).ClearSelection();
                    ((DnnComboBox)control).FindItemByValue(tabSettings[tabSettingsKey].ToString()).Selected = true;
                }
                else
                {
                    //((DropDownList)control).ClearSelection();
                    //((DropDownList)control).Items.FindByValue("").Selected = true;
                    ((DnnComboBox)control).ClearSelection();
                    ((DnnComboBox)control).FindItemByValue("").Selected = true;

                }
            }
        }

        private void ShowCacheRows()
        {
            if (!string.IsNullOrEmpty(cboCacheProvider.SelectedValue))
            {
                CacheDurationRow.Visible = true;
                CacheIncludeExcludeRow.Visible = true;
                MaxVaryByCountRow.Visible = true;
                cmdClearAllPageCache.Visible = true;
                cmdClearPageCache.Visible = true;
                ShowCacheIncludeExcludeRows();
                CacheStatusRow.Visible = true;
                var cachedItemCount = OutputCachingProvider.Instance(cboCacheProvider.SelectedValue).GetItemCount(TabId);
                if (cachedItemCount == 0)
                {
                    cmdClearAllPageCache.Enabled = false;
                    cmdClearPageCache.Enabled = false;
                }
                else
                {
                    cmdClearAllPageCache.Enabled = true;
                    cmdClearPageCache.Enabled = true;
                }
                lblCachedItemCount.Text = string.Format(Localization.GetString("lblCachedItemCount.Text", LocalResourceFile), cachedItemCount);
            }
            else
            {
                CacheStatusRow.Visible = false;
                CacheDurationRow.Visible = false;
                CacheIncludeExcludeRow.Visible = false;
                MaxVaryByCountRow.Visible = false;
                ExcludeVaryByRow.Visible = false;
                IncludeVaryByRow.Visible = false;
            }
        }

        private void ShowCacheIncludeExcludeRows()
        {
            if (rblCacheIncludeExclude.SelectedItem == null)
            {
                rblCacheIncludeExclude.Items[0].Selected = true;
            }
            if (rblCacheIncludeExclude.SelectedValue == "0")
            {
                ExcludeVaryByRow.Visible = false;
                IncludeVaryByRow.Visible = true;
            }
            else
            {
                ExcludeVaryByRow.Visible = true;
                IncludeVaryByRow.Visible = false;
            }
        }

        private void ShowPermissions(bool show)
        {
            permissionsTab.Visible = show;
            permissionRow.Visible = show;
        }

        private bool IsValidTabName(string tabName)
        {
            var valid = true;

            if (string.IsNullOrEmpty(tabName.Trim()))
            {
                ShowWarningMessage(Localization.GetString("EmptyTabName", LocalResourceFile));
                valid = false;
            }
            else if ((Regex.IsMatch(tabName, "^LPT[1-9]$|^COM[1-9]$", RegexOptions.IgnoreCase)))
            {
                valid = false;
                ShowWarningMessage(string.Format(Localization.GetString("InvalidTabName", LocalResourceFile), tabName));
            }
            else if ((Regex.IsMatch(HtmlUtils.StripNonWord(tabName, false), "^AUX$|^CON$|^NUL$|^SITEMAP$|^LINKCLICK$|^KEEPALIVE$|^DEFAULT$|^ERRORPAGE$", RegexOptions.IgnoreCase)))
            {
                valid = false;
                ShowWarningMessage(string.Format(Localization.GetString("InvalidTabName", LocalResourceFile), tabName));
            }

            return valid;
        }

        private bool IsValidTabPath(TabInfo tab, string newTabPath)
        {
            var valid = true;

            //get default culture if the tab's culture is null
            var cultureCode = tab.CultureCode;
            if (string.IsNullOrEmpty(cultureCode))
            {
                cultureCode = PortalSettings.DefaultLanguage;
            }

            //Validate Tab Path
            var tabID = TabController.GetTabByTabPath(tab.PortalID, newTabPath, cultureCode);
            if (tabID != Null.NullInteger && tabID != tab.TabID)
            {
                var controller = new TabController();
                var existingTab = controller.GetTab(tabID, tab.PortalID, false);
                if (existingTab != null && existingTab.IsDeleted)
                    ShowWarningMessage(Localization.GetString("TabRecycled", LocalResourceFile));
                else
                    ShowWarningMessage(Localization.GetString("TabExists", LocalResourceFile));

                valid = false;
            }

            //check whether have conflict between tab path and portal alias.
            if (TabController.IsDuplicateWithPortalAlias(tab.PortalID, newTabPath))
            {
                ShowWarningMessage(Localization.GetString("PathDuplicateWithAlias", LocalResourceFile));
                valid = false;
            }

            return valid;
        }

        private void ShowWarningMessage(string message)
        {
            UI.Skins.Skin.AddModuleMessage(this, message, ModuleMessage.ModuleMessageType.YellowWarning);
        }

        private void UpdateTabSettings(int tabId)
        {
            var t = new TabController();
            t.UpdateTabSetting(tabId, "CacheProvider", cboCacheProvider.SelectedValue);
            t.UpdateTabSetting(tabId, "CacheDuration", txtCacheDuration.Text);
            t.UpdateTabSetting(tabId, "CacheIncludeExclude", rblCacheIncludeExclude.SelectedValue);
            t.UpdateTabSetting(tabId, "IncludeVaryBy", txtIncludeVaryBy.Text);
            t.UpdateTabSetting(tabId, "ExcludeVaryBy", txtExcludeVaryBy.Text);
            t.UpdateTabSetting(tabId, "MaxVaryByCount", txtMaxVaryByCount.Text);
            t.UpdateTabSetting(tabId, "LinkNewWindow", ctlURL.NewWindow.ToString());
        }

        #endregion

        #region Protected Methods

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            jQuery.RequestDnnPluginsRegistration();

            cboCacheProvider.SelectedIndexChanged += cboCacheProvider_Change;
            cboCopyPage.SelectedIndexChanged += cboCopyPage_SelectedIndexChanged;
            cboFolders.SelectedIndexChanged += cboFolders_SelectedIndexChanged;
            cboParentTab.SelectedIndexChanged += cboParentTab_SelectedIndexChanged;
            cmdClearAllPageCache.Click += cmdClearAllPageCache_Click;
            cmdClearPageCache.Click += cmdClearPageCache_Click;
            cmdCopyPerm.Click += cmdCopyPerm_Click;
            cmdCopySkin.Click += cmdCopySkin_Click;
            cmdDelete.Click += cmdDelete_Click;
            cmdUpdate.Click += cmdUpdate_Click;
            editDefaultCultureButton.Command += editDefaultCultureButton_Command;
            localizePagesButton.Click += localizePagesButton_Click;
            moduleLocalization.ModuleLocalizationChanged += moduleLocalization_ModuleLocalizationChanged;
            tabLocalization.TabLocalizationChanged += tabLocalization_ModuleLocalizationChanged;
            publishPageButton.Click += publishPageButton_Click;
            rbInsertPosition.SelectedIndexChanged += rbInsertPosition_SelectedIndexChanged;
            rblCacheIncludeExclude.SelectedIndexChanged += rblCacheIncludeExclude_Change;
            readyForTranslationButton.Click += readyForTranslationButton_Click;
            cmdSubmitTranslation.Click += submitTranslation_Click;
            cmdCancelTranslation.Click += cancelTranslation_Click;
            translatedCheckbox.CheckedChanged += translatedCheckbox_CheckedChanged;
            viewDefaultCultureButton.Command += viewDefaultCultureButton_Command;
            // Verify that the current user has access to edit this module
            if (!TabPermissionController.HasTabPermission("ADD,EDIT,COPY,DELETE,MANAGE"))
            {
                Response.Redirect(Globals.AccessDeniedURL(), true);
            }
            if ((Request.QueryString["action"] != null))
            {
                _strAction = Request.QueryString["action"].ToLower();
            }

            if (Tab.ContentItemId == Null.NullInteger && Tab.TabID != Null.NullInteger)
            {
                var tabCtl = new TabController();
                //This tab does not have a valid ContentItem
                tabCtl.CreateContentItem(Tab);
                tabCtl.UpdateTab(Tab);
            }

            moduleLocalization.TabId = TabId;
            moduleLocalization.ShowEditColumn = false;
            DisableHostAdminFunctions();
        }

        private void DisableHostAdminFunctions()
        {
            var children = TabController.GetTabsByParent(PortalSettings.ActiveTab.TabID, PortalSettings.ActiveTab.PortalID);

            if (children == null || children.Count < 1 || PortalSettings.ActiveTab.IsSuperTab)
            {
                cmdCopySkin.Enabled = false;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {

                if (Page.IsPostBack == false)
                {
                    //ClientAPI.AddButtonConfirm(cmdDelete, Localization.GetString("DeleteItem"));

                    // load the list of files found in the upload directory
                    ctlIcon.ShowFiles = true;
                    ctlIcon.ShowImages = true;
                    ctlIcon.ShowTabs = false;
                    ctlIcon.ShowUrls = false;
                    ctlIcon.Required = false;

                    ctlIcon.ShowLog = false;
                    ctlIcon.ShowNewWindow = false;
                    ctlIcon.ShowTrack = false;
                    ctlIcon.FileFilter = Globals.glbImageFileTypes;
                    ctlIcon.Width = "275px";

                    ctlIconLarge.ShowFiles = ctlIcon.ShowFiles;
                    ctlIconLarge.ShowImages = ctlIcon.ShowImages;
                    ctlIconLarge.ShowTabs = ctlIcon.ShowTabs;
                    ctlIconLarge.ShowUrls = ctlIcon.ShowUrls;
                    ctlIconLarge.Required = ctlIcon.Required;

                    ctlIconLarge.ShowLog = ctlIcon.ShowLog;
                    ctlIconLarge.ShowNewWindow = ctlIcon.ShowNewWindow;
                    ctlIconLarge.ShowTrack = ctlIcon.ShowTrack;
                    ctlIconLarge.FileFilter = ctlIcon.FileFilter;
                    ctlIconLarge.Width = ctlIcon.Width;

                    // tab administrators can only manage their own tab
                    if (!UserInfo.IsSuperUser && !UserInfo.IsInRole(PortalSettings.AdministratorRoleName))
                    {
                        cboParentTab.Enabled = false;
                    }

                    ctlURL.Width = "275px";

                    rowCopySkin.Visible = false;
                    copyPermissionRow.Visible = false;
                    cboCopyPage.ClearSelection();
                    cmdUpdate.Visible = TabPermissionController.HasTabPermission("ADD,EDIT,COPY,MANAGE");
                    cmdUpdate.Text = Localization.GetString(_strAction == "edit" ? "Update" : "Add", LocalResourceFile);

                    bool usingDefaultLocale = LocaleController.Instance.IsDefaultLanguage(LocaleController.Instance.GetCurrentLocale(PortalId).Code);
                    switch (_strAction)
                    {
                        case "":
                        case "add":
                            // add
                            CheckQuota();
                            templateRow1.Visible = true;
                            templateRow2.Visible = true;
                            copyPanel.Visible = TabPermissionController.CanCopyPage() && usingDefaultLocale;
                            cboCopyPage.SelectedIndex = 0;
                            cmdDelete.Visible = false;
                            ctlURL.IncludeActiveTab = true;
                            ctlAudit.Visible = false;
                            break;
                        case "edit":
                            var tabCtrl = new TabController();
                            copyPermissionRow.Visible = (TabPermissionController.CanAdminPage() && tabCtrl.GetTabsByPortal(PortalId).DescendentsOf(TabId).Count > 0);
                            rowCopySkin.Visible = true;
                            copyPanel.Visible = false;
                            cmdDelete.Visible = TabPermissionController.CanDeletePage() && !TabController.IsSpecialTab(TabId, PortalSettings);
                            ctlURL.IncludeActiveTab = false;
                            ctlAudit.Visible = true;
                            break;
                        case "copy":
                            CheckQuota();
                            copyPanel.Visible = TabPermissionController.CanCopyPage() && usingDefaultLocale;
                            cmdDelete.Visible = false;
                            ctlURL.IncludeActiveTab = true;
                            ctlAudit.Visible = false;
                            break;
                        case "delete":
                            if (DeleteTab(TabId))
                            {
                                Response.Redirect(Globals.AddHTTP(PortalAlias.HTTPAlias), true);
                            }
                            else
                            {
                                _strAction = "edit";
                                copyPanel.Visible = false;
                                cmdDelete.Visible = TabPermissionController.CanDeletePage();
                            }
                            ctlURL.IncludeActiveTab = false;
                            ctlAudit.Visible = true;
                            break;
                    }

                    BindTab();

                    //Set the tab id of the permissions grid to the TabId (Note If in add mode
                    //this means that the default permissions inherit from the parent)
                    if (_strAction == "edit" || _strAction == "delete" || !TabPermissionController.CanAdminPage())
                    {
                        dgPermissions.TabID = TabId;
                    }
                    else
                    {
                        dgPermissions.TabID = cboParentTab.SelectedItem != null ? Convert.ToInt32(cboParentTab.SelectedValue) : TabController.CurrentPage.TabID;
                    }
                }

                if (_strAction == "edit")
                {
                    copyTab.Visible = false;
                }

                CheckLocalizationVisibility();

                BindLocalization(false);

                cancelHyperLink.NavigateUrl = Globals.NavigateURL();

                if (Request.QueryString["returntabid"] != null)
                {
                    // return to admin tab
                    var navigateUrl = Globals.NavigateURL(Convert.ToInt32(Request.QueryString["returntabid"]));
                    // add localtion hash to let it select in admin tab intially
                    var hash = "#" + (Tab.PortalID == Null.NullInteger ? "H" : "P") + "&" + Tab.TabID;
                    cancelHyperLink.NavigateUrl = navigateUrl + hash;
                }
                else if (!string.IsNullOrEmpty(Request.QueryString["returnurl"]))
                {
                    cancelHyperLink.NavigateUrl = Request.QueryString["returnurl"];
                }


            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            redirectRow.Visible = ctlURL.UrlType != "N";
        }


        #endregion

        #region EventHandlers

        private void cboCacheProvider_Change(object sender, EventArgs e)
        {
            ShowCacheRows();
        }

        private void cboCopyPage_SelectedIndexChanged(object sender, EventArgs e)
        {
            DisplayTabModules();
        }

        private void cboFolders_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTemplates();
        }

        private void cboParentTab_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindBeforeAfterTabControls();

            insertPositionRow.Visible = cboPositionTab.Items.Count > 0;
        }

        private void cmdClearAllPageCache_Click(object sender, EventArgs e)
        {
            OutputCachingProvider.Instance(cboCacheProvider.SelectedValue).PurgeCache(PortalId);
            ShowCacheRows();
        }

        private void cmdClearPageCache_Click(object sender, EventArgs e)
        {
            OutputCachingProvider.Instance(cboCacheProvider.SelectedValue).Remove(TabId);
            ShowCacheRows();
        }

        private void cmdCopyPerm_Click(object sender, EventArgs e)
        {
            try
            {
                TabController.CopyPermissionsToChildren(new TabController().GetTab(TabId, PortalId, false), dgPermissions.Permissions);
                UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("PermissionsCopied", LocalResourceFile), ModuleMessage.ModuleMessageType.GreenSuccess);
            }
            catch (Exception ex)
            {
                UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("PermissionCopyError", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                Exceptions.ProcessModuleLoadException(this, ex);
            }
        }

        private void cmdCopySkin_Click(object sender, EventArgs e)
        {
            try
            {
                TabController.CopyDesignToChildren(new TabController().GetTab(TabId, PortalId, false), pageSkinCombo.SelectedValue, pageContainerCombo.SelectedValue);
                UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("DesignCopied", LocalResourceFile), ModuleMessage.ModuleMessageType.GreenSuccess);
            }
            catch (Exception ex)
            {
                UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("DesignCopyError", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                Exceptions.ProcessModuleLoadException(this, ex);
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        ///   cmdDelete_Click runs when the Delete Button is clicked
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///   [cnurse]	9/10/2004	Updated to reflect design changes for Help, 508 support
        ///   and localisation
        ///   [VMasanas]  30/09/2004  When a parent tab is deleted all child are also marked as deleted.
        /// </history>
        /// -----------------------------------------------------------------------------
        private void cmdDelete_Click(object Sender, EventArgs e)
        {
            try
            {
                if (DeleteTab(TabId) && TabPermissionController.CanDeletePage())
                {
                    string strURL = Globals.GetPortalDomainName(PortalAlias.HTTPAlias, Request, true);

                    if ((Request.QueryString["returntabid"] != null))
                    {
                        // return to admin tab
                        strURL = Globals.NavigateURL(Convert.ToInt32(Request.QueryString["returntabid"]));
                    }

                    Response.Redirect(strURL, true);
                }
                //Module failed to load
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        ///   cmdUpdate_Click runs when the Update Button is clicked
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///   [cnurse]	9/10/2004	Updated to reflect design changes for Help, 508 support
        ///   [aprasad]	3/21/2011	DNN-14685. Modified Redirect behavior after Save. Stays to the same page
        ///                         if more than one langugae is present, else redirects to the updated/new page
        ///   and localisation
        /// </history>
        /// -----------------------------------------------------------------------------
        private void cmdUpdate_Click(object Sender, EventArgs e)
        {
            try
            {
                if (Page.IsValid && TabPermissionController.HasTabPermission("ADD,EDIT,COPY,MANAGE"))
                {
                    int tabId = SaveTabData(_strAction);

                    if (tabId != Null.NullInteger)
                    {
                        var redirectUrl = Globals.NavigateURL(tabId);

                        if ((Request.QueryString["returntabid"] != null))
                        {
                            // return to admin tab
                            redirectUrl = Globals.NavigateURL(Convert.ToInt32(Request.QueryString["returntabid"]));
                        }
                        else if (!string.IsNullOrEmpty(Request.QueryString["returnurl"]))
                        {
                            redirectUrl = Request.QueryString["returnurl"];
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(ctlURL.Url) || chkDisableLink.Checked)
                            {
                                // redirect to current tab if URL was specified ( add or copy )
                                redirectUrl = Globals.NavigateURL(TabId);
                            }
                        }

                        Response.Redirect(redirectUrl, true);
                    }
                }
                //Module failed to load
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        protected void editDefaultCultureButton_Command(object sender, CommandEventArgs e)
        {
            int tabId = int.Parse(e.CommandArgument.ToString());
            Response.Redirect(Globals.NavigateURL(tabId, Null.NullBoolean, PortalSettings, "Tab", PortalSettings.DefaultLanguage, "action=edit"), true);
        }

        protected void languageTranslatedCheckbox_CheckChanged(object sender, EventArgs e)
        {
            try
            {
                if ((sender) is DnnCheckBox)
                {
                    var translatedCheckbox = (DnnCheckBox)sender;
                    int tabId = int.Parse(translatedCheckbox.CommandArgument);
                    var tabCtrl = new TabController();
                    TabInfo localizedTab = tabCtrl.GetTab(tabId, PortalId, false);

                    tabCtrl.UpdateTranslationStatus(localizedTab, translatedCheckbox.Checked);

                    tabLocalization.DataBind();
                }
            }
            catch (Exception ex)
            {
                Exceptions.ProcessModuleLoadException(this, ex);
            }
        }

        protected void localizePagesButton_Click(object sender, EventArgs e)
        {
            var tabCtrl = new TabController();
            tabCtrl.CreateLocalizedCopies(Tab);

            //Redirect to refresh page (and skinobjects)
            Response.Redirect(Request.RawUrl, true);
        }

        protected void moduleLocalization_ModuleLocalizationChanged(object sender, EventArgs e)
        {
            //Module Localization Chaged so we need tor efresh the TabLocalization control
            tabLocalization.DataBind();
            //moduleLocalization.DataBind();
        }

        protected void tabLocalization_ModuleLocalizationChanged(object sender, EventArgs e)
        {
            //Tab Localization Chaged so we need to refresh the ModuleLocalization control
            //tabLocalization.DataBind();
            moduleLocalization.DataBind();
        }

        protected void publishPageButton_Click(object sender, EventArgs e)
        {
            var tabCtrl = new TabController();
            var modCtrl = new ModuleController();

            //First mark all modules as translated
            foreach (ModuleInfo m in modCtrl.GetTabModules(Tab.TabID).Values)
            {
                modCtrl.UpdateTranslationStatus(m, true);
            }

            //First mark tab as translated
            tabCtrl.UpdateTranslationStatus(Tab, true);

            //Next publish Tab (update Permissions)
            tabCtrl.PublishTab(Tab);

            //Redirect to refresh page (and skinobjects)
            Response.Redirect(Request.RawUrl, true);
        }

        private void rblCacheIncludeExclude_Change(object sender, EventArgs e)
        {
            ShowCacheIncludeExcludeRows();
        }

        protected void rbInsertPosition_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (rbInsertPosition.SelectedValue == "AtEnd")
            {
                cboPositionTab.Visible = false;
                cboPositionTab.SelectedIndex = -1;
            }
            else
            {
                cboPositionTab.Visible = true;
            }
        }

        protected void cancelTranslation_Click(object sender, EventArgs e)
        {
            Response.Redirect(Request.RawUrl, true);
        }

        protected void readyForTranslationButton_Click(object sender, EventArgs e)
        {
            readyForTranslationRow.Visible = false;
            sendTranslationMessageRow.Visible = true;

            tabLocalization.DataBind();
            moduleLocalization.DataBind();
        }

        protected void submitTranslation_Click(object sender, EventArgs e)
        {
            var modCtrl = new ModuleController();
            var tabCtrl = new TabController();

            foreach (TabInfo localizedTab in Tab.LocalizedTabs.Values)
            {
                //Make Deep copies of all modules
                var moduleCtrl = new ModuleController();
                foreach (KeyValuePair<int, ModuleInfo> kvp in moduleCtrl.GetTabModules(Tab.TabID))
                {
                    ModuleInfo sourceModule = kvp.Value;
                    ModuleInfo localizedModule = null;

                    //Make sure module has the correct culture code
                    if (string.IsNullOrEmpty(sourceModule.CultureCode))
                    {
                        sourceModule.CultureCode = Tab.CultureCode;
                        moduleCtrl.UpdateModule(sourceModule);
                    }

                    if (!sourceModule.LocalizedModules.TryGetValue(localizedTab.CultureCode, out localizedModule))
                    {
                        if (!sourceModule.IsDeleted)
                        {
                            //Shallow (Reference Copy)

                            {
                                if (sourceModule.AllTabs)
                                {
                                    foreach (ModuleInfo m in moduleCtrl.GetModuleTabs(sourceModule.ModuleID))
                                    {
                                        //Get the tab
                                        TabInfo allTabsTab = tabCtrl.GetTab(m.TabID, m.PortalID, false);
                                        TabInfo localizedAllTabsTab = null;
                                        if (allTabsTab.LocalizedTabs.TryGetValue(localizedTab.CultureCode, out localizedAllTabsTab))
                                        {
                                            moduleCtrl.CopyModule(m, localizedAllTabsTab, Null.NullString, true);
                                        }
                                    }
                                }
                                else
                                {
                                    moduleCtrl.CopyModule(sourceModule, localizedTab, Null.NullString, true);
                                }
                            }

                            //Fetch new module
                            localizedModule = moduleCtrl.GetModule(sourceModule.ModuleID, localizedTab.TabID);

                            //Convert to deep copy
                            moduleCtrl.LocalizeModule(localizedModule, LocaleController.Instance.GetLocale(localizedTab.CultureCode));
                        }
                    }
                }

                var users = new Dictionary<int, UserInfo>();

                //Give default translators for this language and administrators permissions
                tabCtrl.GiveTranslatorRoleEditRights(localizedTab, users);

                //Send Messages to all the translators of new content
                foreach (var translator in users.Values.Where(user => user.UserID != PortalSettings.AdministratorId))
                {
                    AddTranslationSubmittedNotification(localizedTab, translator);
                }
            }

            //Redirect to refresh page (and skinobjects)
            Response.Redirect(Request.RawUrl, true);
        }

        protected void translatedCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            var tabCtrl = new TabController();
            TabInfo localizedTab = tabCtrl.GetTab(TabId, PortalId, false);

            tabCtrl.UpdateTranslationStatus(localizedTab, translatedCheckbox.Checked);

            //Rebind Tab
            _tab = null;
            BindLocalization(true);
        }

        protected void viewDefaultCultureButton_Command(object sender, CommandEventArgs e)
        {
            int tabId = int.Parse(e.CommandArgument.ToString());
            Response.Redirect(Globals.NavigateURL(tabId, Null.NullBoolean, PortalSettings, "", PortalSettings.DefaultLanguage, new string[] { }), true);
        }

        #endregion
    }
}