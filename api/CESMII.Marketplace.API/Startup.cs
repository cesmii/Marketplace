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
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;

using NLog;
using NLog.Extensions.Logging;
using NLog.Fluent;

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
using CESMII.Marketplace.JobManager;
using CESMII.Marketplace.Api.Shared.Extensions;
using CESMII.Common.CloudLibClient;
using CESMII.Common.SelfServiceSignUp.Services;
using CESMII.Common.SelfServiceSignUp.Models;
using System.Security;

namespace CESMII.Marketplace.Api
{
    public class Startup
    {
        private readonly string _corsPolicyName = "SiteCorsPolicy";
        private readonly string _version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Check for this later: MARKETPLACE_GITHUB_WORKFLOW_COMMANDS
            // For now, let's just see if we can see this at all!
            Console.WriteLine("::notice::ConfigureServices - Entering function. Does this actually work?");

            //MongoDB approach
            services.Configure<MongoDBConfig>(Configuration);

            string strConnectionString = Environment.GetEnvironmentVariable("MARKETPLACE_MONGODB_CONNECTIONSTRING");
            string strDatabase = Environment.GetEnvironmentVariable("MARKETPLACE_MONGODB_DATABASE");

            // In a test environment, we get connection string from the environment
            if (!string.IsNullOrEmpty(strConnectionString))
            { 
                Configuration["MongoDBSettings:ConnectionString"] = strConnectionString;
            }

            if (!string.IsNullOrEmpty(strDatabase))
            {
                 Configuration["MongoDBSettings:DatabaseName"] = strDatabase;
            }

            Console.WriteLine($"::notice::ConfigureServices - strConnectionString:{strConnectionString}");
            Console.WriteLine($"::notice::ConfigureServices - strDatabase:{strDatabase}");

            var root = (IConfigurationRoot)Configuration;
            var debugView = root.GetDebugView();
            System.Diagnostics.Debug.WriteLine(debugView);

            string strCollection = Configuration["MongoDBSettings:NLogCollectionName"];
            NLog.Mongo.MongoTarget.SetNLogMongoOverrides(strConnectionString: strConnectionString,
                                                         strCollectionName: strCollection,
                                                         strDatabaseName: strDatabase);

            // Set variable used in nLog.config
            NLog.LogManager.Configuration.Variables["appName"] = "CESMII-Marketplace";

            //marketplace and related data
            services.AddScoped<IMongoRepository<MarketplaceItem>, MongoRepository<MarketplaceItem>>();
            services.AddScoped<IMongoRepository<LookupItem>, MongoRepository<LookupItem>>();
            services.AddScoped<IMongoRepository<Publisher>, MongoRepository<Publisher>>();
            services.AddScoped<IMongoRepository<MarketplaceItemAnalytics>, MongoRepository<MarketplaceItemAnalytics>>();
            services.AddScoped<IMongoRepository<RequestInfo>, MongoRepository<RequestInfo>>();
            services.AddScoped<IMongoRepository<ImageItem>, MongoRepository<ImageItem>>();
            services.AddScoped<IMongoRepository<ImageItemSimple>, MongoRepository<ImageItemSimple>>();
            services.AddScoped<IMongoRepository<SearchKeyword>, MongoRepository<SearchKeyword>>();
            services.AddScoped<IMongoRepository<ProfileItem>, MongoRepository<ProfileItem>>();

            //stock tables
            services.AddScoped<IMongoRepository<Organization>, MongoRepository<Organization>>();
            services.AddScoped<IMongoRepository<User>, MongoRepository<User>>();
            //services.AddScoped<IMongoRepository<Permission>, MongoRepository<Permission>>();
            services.AddScoped<IMongoRepository<JobLog>, MongoRepository<JobLog>>();
            services.AddScoped<IMongoRepository<JobDefinition>, MongoRepository<JobDefinition>>();

            Console.WriteLine("::notice::ConfigureServices - Line 115");

            //DAL objects
            services.AddScoped<UserDAL>();  //this one has extra methods outside of the IDal interface
            services.AddScoped<OrganizationDAL>();
            services.AddScoped<IUserSignUpData, UserSignUpData>();
            services.AddScoped<IDal<MarketplaceItem, MarketplaceItemModel>, MarketplaceDAL>();
            services.AddScoped<IDal<MarketplaceItem, AdminMarketplaceItemModel>, AdminMarketplaceDAL>();
            services.AddScoped<IDal<LookupItem, LookupItemModel>, LookupDAL>();
            services.AddScoped<IDal<Publisher, PublisherModel>, PublisherDAL>();
            services.AddScoped<IDal<Publisher, AdminPublisherModel>, AdminPublisherDAL>();
            services.AddScoped<IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel>, MarkeplaceAnalyticsDAL>();
            services.AddScoped<IDal<RequestInfo, RequestInfoModel>, RequestInfoDAL>();
            services.AddScoped<IDal<ImageItem, ImageItemModel>, ImageItemDAL>();
            services.AddScoped<IDal<JobLog, JobLogModel>, JobLogDAL>();
            services.AddScoped<IDal<JobDefinition, JobDefinitionModel>, JobDefinitionDAL>();
            services.AddScoped<IDal<SearchKeyword, SearchKeywordModel>, SearchKeywordDAL>();

            // Configuration, utils, one off objects
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<ConfigUtil>();  //helper to allow us to bind to app settings data 
            services.AddSingleton<MailRelayService>();  //helper for emailing
            services.AddSingleton<MongoClientGlobal>();
            services.AddScoped<IJobFactory, JobFactory>();
            services.AddScoped<IHttpApiFactory, HttpApiFactory>();

            //Cloud Lib
            //var xx = Configuration.GetSection("CloudLibrary");
            //if (xx.)
            services.Configure<Opc.Ua.Cloud.Library.Client.UACloudLibClient.Options>(Configuration.GetSection("CloudLibrary"));
            services.AddSingleton<Opc.Ua.Cloud.Library.Client.UACloudLibClient>();
            services.AddSingleton<ICloudLibWrapper, CloudLibWrapper>();
            services.AddScoped<ICloudLibDAL<MarketplaceItemModelWithCursor>, CloudLibDAL>();
            services.AddScoped<IAdminCloudLibDAL<AdminMarketplaceItemModelWithCursor>, AdminCloudLibDAL>();

            //AAD - no longer need this
            // Add token builder.
            //var configUtil = new ConfigUtil(Configuration);
            //services.AddTransient(provider => new TokenUtils(configUtil));

            Console.WriteLine("::notice::ConfigureServices - Line 155");

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
                    },Array.Empty<string>()
                }
                });
            });

            Console.WriteLine("::notice::ConfigureServices - Line 185");

            // https://stackoverflow.com/questions/46112258/how-do-i-get-current-user-in-net-core-web-api-from-jwt-token
            //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //    .AddJwtBearer(options =>
            //    {
            //        options.TokenValidationParameters = new TokenValidationParameters
            //        {
            //            ValidateIssuer = true,
            //            ValidateLifetime = true,
            //            ValidateAudience = false,
            //            ValidateIssuerSigningKey = true,
            //            ValidIssuer = configUtil.JWTSettings.Issuer,
            //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configUtil.JWTSettings.Key))
            //        };
            //    });
            //New - Azure AD approach replaces previous code above
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(Configuration, "AzureAdSettings");

            //TBD - may not need these at all anymore since AAD implementation
            // Add permission authorization requirements.
            services.AddAuthorization(options =>
            {
                /*
                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanManageMarketplace),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanManageMarketplace)));

                options.AddPolicy(
                    nameof(PermissionEnum.GeneralUser),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.GeneralUser)));

                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanManageJobDefinitions),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanManageJobDefinitions)));
				*/
                // this "permission" is set once AD user has a mapping to a user record in the Mktplace DB
                options.AddPolicy(
                    nameof(PermissionEnum.UserAzureADMapped),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.UserAzureADMapped)));
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

            Console.WriteLine("::notice::ConfigureServices - Line 241");

            services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

            services.AddMvc(); //add this to permit emailing to bind models to view templates.
            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            });

            Console.WriteLine("::notice::ConfigureServices - Line 251");

            // Add in-memory caching
            services.AddMemoryCache();

            // Add response caching.
            services.AddResponseCaching();

            //add httpclient service for dependency injection
            //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-6.0
            services.AddHttpClient();

            Console.WriteLine("::notice::ConfigureServices - EXITING!!!! Line 263 ");
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
                    System.Diagnostics.Debug.WriteLine("Pre-flight check complete - {context.Request.Path}");
                }
                else
                {
                    //else, continue on to next request
                    await next();
                }
            });

            #pragma warning disable CS1998  // Silence warning: This async method lacks 'await' operators and will run synchronously.
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(async o =>
                {
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
                IdentityModelEventSource.ShowPII = true;
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

            app.UseMiddleware<UserAzureADMapping>();

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
