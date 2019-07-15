﻿using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notebook.Business.Managers.Abstract;
using Notebook.Business.Managers.Concrete;
using Notebook.Business.Tools.Logging;
using Notebook.Core.Aspects.SimpleProxy.Caching;
using Notebook.Core.Aspects.SimpleProxy.Logging;
using Notebook.Core.Aspects.SimpleProxy.Validation;
using Notebook.Core.CrossCuttingConcerns.Logging;
using Notebook.DataAccess.DataAccess.Abstract;
using Notebook.DataAccess.DataAccess.Concrete.EntityFramework;
using Notebook.DataAccess.DataContext;
using Notebook.Web.Filters;
using Notebook.Web.Middlewares;
using Notebook.Web.Tools.FileManager;
using SimpleProxy.Extensions;
using SimpleProxy.Strategies;
using System;
using System.Globalization;
//using AutoMapper.Extensions.Microsoft.DependencyInjection;

namespace Notebook.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region DbContext

            services.AddDbContext<NotebookContext>(options => options.UseMySql(Configuration.GetConnectionString("NotebookContext")));
            services.AddScoped<DbContext, NotebookContext>();

            #endregion

            #region Aspects

            services.EnableSimpleProxy(p => p
                .AddInterceptor<ValidateAttribute, ValidateInterceptor>()
                .AddInterceptor<CacheAttribute, CacheInterceptor>()
                .AddInterceptor<LogAttribute, LogInterceptor>()
                .WithOrderingStrategy<PyramidOrderStrategy>());

            services.AddScoped<ILoggerService, FileLogger>();
            services.AddScoped<LogFilterAttribute>();
            services.AddScoped<AccountFilterAttribute>();
            services.AddScoped<ExceptionFilterAttribute>();
            services.AddScoped<IFileManager,FileManager>();
            #endregion

            #region Managers

            services.AddScopedWithProxy<IUserDal, EfUserDal>();
            services.AddScopedWithProxy<IUserManager, UserManager>();

            services.AddScopedWithProxy<ILogDal, EfLogDal>();
            services.AddScopedWithProxy<ILogManager, LogManager>();

            services.AddScopedWithProxy<IGroupDal, EfGroupDal>();
            services.AddScopedWithProxy<IGroupManager, GroupManager>();

            services.AddScopedWithProxy<IFolderDal, EfFolderDal>();
            services.AddScopedWithProxy<IFolderManager, FolderManager>();

            services.AddScopedWithProxy<INoteDal, EfNoteDal>();
            services.AddScopedWithProxy<INoteManager, NoteManager>();

            services.AddScopedWithProxy<IUserGroupDal, EfUserGroupDal>();
            services.AddScopedWithProxy<IUserGroupManager, UserGroupManager>();

            services.AddScopedWithProxy<IUserFolderDal, EfUserFolderDal>();
            services.AddScopedWithProxy<IUserFolderManager, UserFolderManager>();

            services.AddScopedWithProxy<IUserNoteDal, EfUserNoteDal>();
            services.AddScopedWithProxy<IUserNoteManager, UserNoteManager>();

            services.AddScopedWithProxy<IGroupFolderDal, EfGroupFolderDal>();
            services.AddScopedWithProxy<IGroupFolderManager, GroupFolderManager>();

            services.AddScopedWithProxy<IGroupNoteDal, EfGroupNoteDal>();
            services.AddScopedWithProxy<IGroupNoteManager, GroupNoteManager>();

            services.AddScopedWithProxy<IFolderNoteDal, EfFolderNoteDal>();
            services.AddScopedWithProxy<IFolderNoteManager, FolderNoteManager>();

            services.AddScopedWithProxy<ISettingsDal, EfSettingsDal>();
            services.AddScopedWithProxy<ISettingsManager, SettingsManager>();
            #endregion

            #region Session

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.CookieHttpOnly = true;
            });

            //services.AddDistributedMemoryCache();
            services.AddMemoryCache();

            //services.Configure<CookiePolicyOptions>(options =>
            //{
            //    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            //    options.CheckConsentNeeded = context => false;
            //    options.MinimumSameSitePolicy = SameSiteMode.None;
            //});

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            #endregion

            #region Multi Language

            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("tr-TR")
                };

                options.DefaultRequestCulture = new RequestCulture(culture: "tr-TR", uiCulture: "tr-TR");

                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            #endregion

            #region AutoMapper

            services.AddAutoMapper();

            #endregion

            #region Social Login

            services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = Configuration.GetSection("Authentication").GetValue<string>("GoogleClientId");
                googleOptions.ClientSecret = Configuration.GetSection("Authentication").GetValue<string>("GoogleClientSecret");
            })
            .AddFacebook(facebookOptions =>
            {
                facebookOptions.AppId = Configuration.GetSection("Authentication").GetValue<string>("FacebookClientId");
                facebookOptions.AppSecret = Configuration.GetSection("Authentication").GetValue<string>("FacebookClientSecret");
            })
            .AddCookie();

            #endregion
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(locOptions.Value);


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseAuthentication();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
