using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using NLog;
using NLog.Extensions.Logging;

using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Utils;
using CESMII.Marketplace.Api.Shared.Utils;
using CESMII.Marketplace.Data.Contexts;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Data.Repositories;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common.Enums;
using CESMII.Marketplace.Common.Models;

namespace CESMII.Marketplace.Api
{
    public class Startup
    {
        private string _corsPolicyName = "SiteCorsPolicy";
        private string _version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //MongoDB approach
            services.Configure<MongoDBConfig>(Configuration);

            //set variables used in nLog.config
            //TBD - Mongo db - log to DB
            //NLog.LogManager.Configuration.Variables["connectionString"] = connectionStringProfileDesigner;
            NLog.LogManager.Configuration.Variables["appName"] = "CESMII-Marketplace";
           

            //marketplace and related data
            services.AddScoped<IMongoRepository<MarketplaceItem>, MongoRepository<MarketplaceItem>>();
            services.AddScoped<IMongoRepository<LookupItem>, MongoRepository<LookupItem>>();
            services.AddScoped<IMongoRepository<Publisher>, MongoRepository<Publisher>>();
            services.AddScoped<IMongoRepository<MarketplaceItemAnalytics>, MongoRepository<MarketplaceItemAnalytics>>();
            services.AddScoped<IMongoRepository<RequestInfo>, MongoRepository<RequestInfo>>();
            services.AddScoped<IMongoRepository<ImageItem>, MongoRepository<ImageItem>>();
            services.AddScoped<IMongoRepository<ImageItemSimple>, MongoRepository<ImageItemSimple>>();

            //stock tables
            services.AddScoped<IMongoRepository<Organization>, MongoRepository<Organization>>();
            services.AddScoped<IMongoRepository<User>, MongoRepository<User>>();
            services.AddScoped<IMongoRepository<Permission>, MongoRepository<Permission>>();

            //DAL objects
            services.AddScoped<UserDAL>();  //this one has extra methods outside of the IDal interface
            services.AddScoped<IDal<MarketplaceItem, MarketplaceItemModel>, MarketplaceDAL>();
            services.AddScoped<IDal<MarketplaceItem, AdminMarketplaceItemModel>, AdminMarketplaceDAL>();
            services.AddScoped<IDal<LookupItem, LookupItemModel>, LookupDAL>();
            services.AddScoped<IDal<Publisher, PublisherModel>, PublisherDAL>();
            services.AddScoped<IDal<Publisher, AdminPublisherModel>, AdminPublisherDAL>();
            services.AddScoped<IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel>, MarkeplaceAnalyticsDAL>();
            services.AddScoped<IDal<RequestInfo, RequestInfoModel>, RequestInfoDAL>();
            services.AddScoped<IDal<ImageItem, ImageItemModel>, ImageItemDAL>();

            //services.AddScoped<IDal<Organization, OrganizationModel>, OrganizationDAL>();


            // Configuration, utils, one off objects
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<ConfigUtil>();  //helper to allow us to bind to app settings data 
            services.AddSingleton<MailRelayService>();  //helper for emailing
            services.AddSingleton<MongoClientGlobal>();  //helper for emailing

            // Add token builder.
            var configUtil = new ConfigUtil(Configuration);
            services.AddTransient(provider => new TokenUtils(configUtil));

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "CESMII.Marketplace.Api",
                    Version = "v1"
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    Description = "Please insert JWT with Bearer into field"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                    new OpenApiSecurityScheme {
                    Reference = new OpenApiReference {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                    },new string[] { }
                }
                });
            });

            // https://stackoverflow.com/questions/46112258/how-do-i-get-current-user-in-net-core-web-api-from-jwt-token
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateLifetime = true,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configUtil.JWTSettings.Issuer,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configUtil.JWTSettings.Key))
                    };
                });

            // Add permission authorization requirements.
            services.AddAuthorization(options =>
            {
                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanManageMarketplace),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanManageMarketplace)));

                options.AddPolicy(
                    nameof(PermissionEnum.CanManagePublishers),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanManagePublishers)));

                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanManageSystemSettings),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanManageSystemSettings)));

                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanManageUsers),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanManageUsers)));

                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanManageRequestInfo),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanManageRequestInfo)));
            });

            services.AddCors(options =>
            {
                options.AddPolicy(_corsPolicyName,
                builder =>
                {
                    //TBD - uncomment, come back to this and lock down the origins based on the appsettings config settings
                    //builder.WithOrigins(configUtil.CorsSettings.AllowedOrigins);
                    builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

            services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

            services.AddMvc(); //add this to permit emailing to bind models to view templates.
            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            });

            // Add in-memory caching
            services.AddMemoryCache();

            // Add response caching.
            services.AddResponseCaching();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Check for pre-flight request. if pre-flight request, return immediately that this is ok
            // if request made by our front end.
            // This is done for performance reasons.
            app.Use(async (context, next) =>
            {
                if (context.Request.Method.ToLower().Equals("options") &&
                    (context.Request.Headers.ContainsKey("origin") || context.Request.Headers.ContainsKey("Origin")))
                {

                    System.Diagnostics.Debug.WriteLine($"Pre-flight Options Check - {context.Request.Path}");
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    context.Response.Headers.Add("Access-Control-Allow-Headers", "authorization,content-type");
                    context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT");
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.NoContent;
                    await context.Response.WriteAsync("Pre-flight check complete.");
                }
                else
                {
                    //else, continue on to next request
                    await next();
                }
            });

            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(async o => {
                    if (o is HttpContext ctx)
                    {
                        ctx.Response.Headers["x-api-version"] = _version;
                    }
                }, context);
                await next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CESMII.Marketplace.Api v1"));
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // Enable CORS. - this needs to go after UseRouting.
            app.UseCors(_corsPolicyName);

            // Enable authentications (Jwt in our case)
            app.UseAuthentication();

            app.UseAuthorization();

            // Allow use of static files.
            app.UseStaticFiles();

            // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-2.2
            app.UseResponseCaching();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
