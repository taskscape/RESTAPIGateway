using GenericTableAPI.Repositories;
using GenericTableAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Serilog;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.AspNetCore.Authorization;

namespace GenericTableAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                builder.Configuration.AddJsonFile("tablesettings.json", optional: false, reloadOnChange: true);
            }
            catch (Exception)
            {
                Log.Logger.Error("[AUTH] Missing configuration file: tablesettings.json ");
                Log.Logger.Information("Please refer to ### Table Authorization ### section in the documentation to create a valid tablesettings.json file!");
                throw new InvalidOperationException("tablesettings.json is missing. Add tablesettings.json in order to run the service!");
            }
            

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSingleton(new DapperRepository(builder.Configuration.GetConnectionString("DefaultConnection"), builder.Configuration.GetValue<string>("SchemaName"), Log.Logger));
            builder.Services.AddScoped<DatabaseService>();
            builder.Services.AddHttpClient();

            builder.Services.AddScoped<CompositeService>();

            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Description = "Standard Authorization header using scheme (\"scheme {token}\")",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                });

                options.DocumentFilter<DynamicSwaggerFilter>();
                options.OperationFilter<SecurityRequirementsOperationFilter>();
                options.SwaggerDoc("v1",
                    new OpenApiInfo
                    {
                        Title = "My API - V1",
                        Version = "v1"
                    }
                );
                string filePath = Path.Combine(AppContext.BaseDirectory, "GenericTableAPI.xml");
                options.IncludeXmlComments(filePath);
            });

            // Authentication Configuration
            string? jwtKey = builder.Configuration["JwtSettings:Key"];
            string? basicAuthUser = builder.Configuration["BasicAuthSettings:0:Username"];

            List<string> allowedAuthSchemes = [];
            if (!string.IsNullOrEmpty(jwtKey))
            {
                Log.Logger.Information("[AUTH] Using Bearer Authentication");
                builder.Services.AddAuthentication()
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                            ValidateIssuer = false,
                            ValidateAudience = false
                        };
                    });
                allowedAuthSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
            }
            if (!string.IsNullOrEmpty(basicAuthUser))
            {
                Log.Logger.Information("[AUTH] Using BasicAuthentication");
                builder.Services.AddAuthentication("BasicAuthentication")
                    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
                allowedAuthSchemes.Add("BasicAuthentication");
            }
            if (bool.Parse(builder.Configuration["NTLMAuthentication"]??"false"))
            {
                Log.Logger.Information("[AUTH] Using NTLMAuthentication");
                builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
                allowedAuthSchemes.Add(IISDefaults.AuthenticationScheme);
            }   
            if (allowedAuthSchemes.Count == 0)
            {
                Log.Logger.Warning("[AUTH] Using No Authentication");
                builder.Services.AddAuthentication("NoAuthentication")
                    .AddScheme<AuthenticationSchemeOptions, NoAuthenticationHandler>("NoAuthentication", null);
                allowedAuthSchemes.Add("NoAuthentication");
            }

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            // Add Authorization
            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(allowedAuthSchemes.ToArray())
                    .RequireAuthenticatedUser()
                    .Build();
            });

            WebApplication app = builder.Build();
            
            app.UseRouting();

            if(bool.Parse(builder.Configuration["EnableSwagger"] ?? "false"))
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Use exception handling middleware
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync("An error occurred. Please try again later.");
                    });
                });
            }
            else
            {
                app.UseDeveloperExceptionPage(); // Shows stack trace
            }

            app.UseHttpsRedirection();
            
            app.UseAuthentication();
            app.UseAuthorization();
            
            // prioritize controllers in the following order

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "TokenController",
                    pattern: "api/token",
                    defaults: new { controller = "TokenController" },
                    constraints: new { controllerPriority = new ControllerPriorityConstraint("TokenController") }
                );

                endpoints.MapControllerRoute(
                    name: "TestController",
                    pattern: "api/test",
                    defaults: new { controller = "TestController" },
                    constraints: new { controllerPriority = new ControllerPriorityConstraint("TestController") }
                );

                endpoints.MapControllerRoute(
                    name: "GenericController",
                    pattern: "{controller}",
                    defaults: new { controller = "DatabaseController" }
                );
            });

            app.MapControllers();

            app.Run();
        }
    }
}
