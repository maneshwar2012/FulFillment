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
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;

using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;
using DotNetNuke.ComponentModel;
using DotNetNuke.Modules.HTMLEditorProvider;
using DotNetNuke.Modules.NavigationProvider;
using DotNetNuke.Security.Membership;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Security.Profile;
using DotNetNuke.Security.Roles;
using DotNetNuke.Services.Cache;
using DotNetNuke.Services.ClientCapability;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Log.EventLog;
using DotNetNuke.Services.ModuleCache;
using DotNetNuke.Services.OutputCache;
using DotNetNuke.Services.Scheduling;
using DotNetNuke.Services.Search;
using DotNetNuke.Services.Sitemap;
using DotNetNuke.Services.Url.FriendlyUrl;

using MembershipProvider = DotNetNuke.Security.Membership.MembershipProvider;
using RoleProvider = DotNetNuke.Security.Roles.RoleProvider;

namespace DotNetNuke.Tests.Instance.Utilities
{
    public class DnnUnitTest
    {
        protected SqlDataProvider SqlProvider;
        public int PortalId { get; private set; }
        private static  bool alreadyLoaded = false;
        public DnnUnitTest(int portalId)
        {
            var simulator = new HttpSimulator.HttpSimulator();
            simulator.SimulateRequest();

            InstallComponents();

            HttpContextBase httpContextBase = new HttpContextWrapper(HttpContext.Current);
            HttpContextSource.RegisterInstance(httpContextBase);

            LoadDnnProviders("data;logging;caching;authentication;members;roles;profiles;permissions;folder");
            //fix Globals.ApplicationMapPath
            var appPath = ConfigurationManager.AppSettings["DefaultPhysicalAppPath"];
            if(!string.IsNullOrEmpty(appPath))
            {
                var mappath = typeof (Globals).GetField("_applicationMapPath", BindingFlags.Static | BindingFlags.NonPublic);
                mappath.SetValue(null, appPath);
            }

            //fix membership
            var providerProp = typeof(Membership).GetField("s_Provider", BindingFlags.Static | BindingFlags.NonPublic);
            providerProp.SetValue(null, Membership.Providers["AspNetSqlMembershipProvider"]);

            var objPortalAliasInfo = new DotNetNuke.Entities.Portals.PortalAliasInfo { PortalID = portalId };
            var ps = new DotNetNuke.Entities.Portals.PortalSettings(59, objPortalAliasInfo);
            System.Web.HttpContext.Current.Items.Add("PortalSettings", ps);
            SqlProvider = new SqlDataProvider();
            PortalId = portalId;
        }

        private static void InstallComponents()
        {
            Globals.ServerName = String.IsNullOrEmpty(Config.GetSetting("ServerName"))
                                ? Dns.GetHostName()
                                : Config.GetSetting("ServerName");

            ComponentFactory.Container = new SimpleContainer();

            ComponentFactory.InstallComponents(new ProviderInstaller("data", typeof(DataProvider), typeof(SqlDataProvider)));
            ComponentFactory.InstallComponents(new ProviderInstaller("caching", typeof(CachingProvider), typeof(FBCachingProvider)));
            ComponentFactory.InstallComponents(new ProviderInstaller("logging", typeof(LoggingProvider), typeof(DBLoggingProvider)));
            ComponentFactory.InstallComponents(new ProviderInstaller("scheduling", typeof(SchedulingProvider), typeof(DNNScheduler)));
            ComponentFactory.InstallComponents(new ProviderInstaller("searchIndex", typeof(IndexingProvider), typeof(ModuleIndexer)));
            ComponentFactory.InstallComponents(new ProviderInstaller("searchDataStore", typeof(SearchDataStoreProvider), typeof(SearchDataStore)));
            ComponentFactory.InstallComponents(new ProviderInstaller("members", typeof(MembershipProvider), typeof(AspNetMembershipProvider)));
            ComponentFactory.InstallComponents(new ProviderInstaller("roles", typeof(RoleProvider), typeof(DNNRoleProvider)));
            ComponentFactory.InstallComponents(new ProviderInstaller("profiles", typeof(ProfileProvider), typeof(DNNProfileProvider)));
            ComponentFactory.InstallComponents(new ProviderInstaller("permissions", typeof(PermissionProvider), typeof(CorePermissionProvider)));
            ComponentFactory.InstallComponents(new ProviderInstaller("outputCaching", typeof(OutputCachingProvider)));
            ComponentFactory.InstallComponents(new ProviderInstaller("moduleCaching", typeof(ModuleCachingProvider)));
            ComponentFactory.InstallComponents(new ProviderInstaller("sitemap", typeof(SitemapProvider), typeof(CoreSitemapProvider)));

            ComponentFactory.InstallComponents(new ProviderInstaller("friendlyUrl", typeof(FriendlyUrlProvider)));
            ComponentFactory.InstallComponents(new ProviderInstaller("folder", typeof(FolderProvider)));
            RegisterIfNotAlreadyRegistered<FolderProvider, StandardFolderProvider>("StandardFolderProvider");
            RegisterIfNotAlreadyRegistered<FolderProvider, SecureFolderProvider>("SecureFolderProvider");
            RegisterIfNotAlreadyRegistered<FolderProvider, DatabaseFolderProvider>("DatabaseFolderProvider");
            RegisterIfNotAlreadyRegistered<PermissionProvider>();
            ComponentFactory.InstallComponents(new ProviderInstaller("htmlEditor", typeof(HtmlEditorProvider), ComponentLifeStyleType.Transient));
            ComponentFactory.InstallComponents(new ProviderInstaller("navigationControl", typeof(NavigationProvider), ComponentLifeStyleType.Transient));
            ComponentFactory.InstallComponents(new ProviderInstaller("clientcapability", typeof(ClientCapabilityProvider)));
        }

        private static void RegisterIfNotAlreadyRegistered<TConcrete>() where TConcrete : class, new()
        {
            RegisterIfNotAlreadyRegistered<TConcrete, TConcrete>("");
        }

        private static void RegisterIfNotAlreadyRegistered<TAbstract, TConcrete>(string name)
            where TAbstract : class
            where TConcrete : class, new()
        {
            var provider = ComponentFactory.GetComponent<TAbstract>();
            if (provider == null)
            {
                if (String.IsNullOrEmpty(name))
                {
                    ComponentFactory.RegisterComponentInstance<TAbstract>(new TConcrete());
                }
                else
                {
                    ComponentFactory.RegisterComponentInstance<TAbstract>(name, new TConcrete());
                }
            }
        }

        /// <summary>
        /// This proc loads up specified DNN providers, because the BuildManager doesn't get the context right
        /// The providers are cahced so that the DNN base buildManager calls don't have to load up hte providers
        /// </summary>
        private static void LoadDnnProviders(string providerList)
        {
            if (alreadyLoaded)
            {
                return;
            }
            alreadyLoaded = true;
            if (providerList != null)
            {
                var providers = providerList.Split(';');
                foreach (var provider in providers)
                {
                    if (provider.Length > 0)
                    {
                        var config = DotNetNuke.Framework.Providers.ProviderConfiguration.GetProviderConfiguration(provider);
                        if (config != null)
                        {
                            foreach (string providerName in config.Providers.Keys)
                            {
                                var providerValue = (DotNetNuke.Framework.Providers.Provider)config.Providers[providerName];
                                var type = providerValue.Type;
                                var assembly = providerValue.Name;

                                if (type.Contains(", "))  //get the straight typename, no assembly, for the cache key
                                {
                                    assembly = type.Substring(type.IndexOf(", ") + 1);
                                    type = type.Substring(0, type.IndexOf(", "));
                                }

                                var cacheKey = type;

                                DotNetNuke.Framework.Reflection.CreateType(providerValue.Type, cacheKey, true, false);
                            }
                        }
                    }
                }
            }
        }
    }
}
