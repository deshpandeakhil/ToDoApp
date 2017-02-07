﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ToDo.Web.Data;
using ToDo.Web.Filters;
using ToDo.Web.Repository;
using ToDo.Web.ViewModels;

namespace ToDo.Web
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            _config = builder.Build();
            _env = env;
        }

        private IConfigurationRoot _config { get; }
        private IHostingEnvironment _env { get; }

        // To setup dependency injection
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_config);
            //const string ConnString = @"Server=tcp:ard.database.windows.net,1433;Data Source=ard.database.windows.net;Initial Catalog=ToDo;Persist Security Info=False;User ID=ard;Password=Akhil1012;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            services.AddScoped<IItemRepository, ItemRepository>();
            //services.AddScoped((_) => new ToDoContext(Configuration["Data:ToDoContext"]));
            services.AddTransient<IdentityInitializer>();

            services.AddDbContext<ToDoContext>(options => options.UseSqlServer(_config["Data:ToDoContext"]));

            // Add caching
            services.AddMemoryCache();

            // Add Identity
            services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<ToDoContext>();

            services.Configure<IdentityOptions>(config =>
            {
                config.Cookies.ApplicationCookie.Events = new CookieAuthenticationEvents()
                {
                    OnRedirectToLogin = (ctx) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                        {
                            ctx.Response.StatusCode = 401;
                        }
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = (ctx) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                        {
                            ctx.Response.StatusCode = 403;
                        }
                        return Task.CompletedTask;
                    }
                };
                config.Cookies.ApplicationCookie.CookieName = "ToDoCookie";
                config.Cookies.ApplicationCookie.ExpireTimeSpan = TimeSpan.FromHours(2);
                
            });

            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllAllow", cfg =>
                {
                    cfg.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });

                options.AddPolicy("OnlyGetAllow", cfg =>
                {
                    cfg.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .WithMethods("GET");
                });
            });

            // Add framework services.
            services.AddMvc(options =>
            {
                if (!_env.IsProduction())
                {
                    options.SslPort = 44388;
                }
                options.Filters.Add(typeof(SampleActionFilter)); // global filter
                options.Filters.Add(new RequireHttpsAttribute());
            })
            .AddJsonOptions(opt =>
                {
                    // property names should be camel case, that is first character is lower case
                    //opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    opt.SerializerSettings.ContractResolver = new DefaultContractResolver();

                    // make output json indented
                    opt.SerializerSettings.Formatting = Formatting.Indented;
                    
                });

        }

        // To setup how request is being handled
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IdentityInitializer identitySeeder)
        {
            loggerFactory.AddConsole(_config.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.Use(async (context, next) =>
            {
                await next();

                if (context.Response.StatusCode == 404 && !Path.HasExtension(context.Request.Path.Value))
                {
                    context.Request.Path = "/index.html"; // Put your Angular root page here 
                    await next();
                }
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseIdentity();
            
            app.UseMvcWithDefaultRoute();

            InitializeAutoMapper();

            identitySeeder.Seed().Wait();
        }

        private void InitializeAutoMapper()
        {
            AutoMapper.Mapper.Initialize(config => {
                config.CreateMap<Item, ItemVM>().ReverseMap();
                config.CreateMap<SubItem, SubItemVM>().ReverseMap();
                config.CreateMap<Priority, PriorityVM>().ReverseMap();

            });
        }
    }
}
