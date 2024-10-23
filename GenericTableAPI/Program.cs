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
                builder.Configuration.AddJsonFile("tablesettings.json", optional: true, reloadOnChange: true);
            }
            catch (Exception)
            {
                Log.Logger.Warning("[AUTH] No tablesettings.json. Granting access to every table");
            }
            

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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

            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Standard Authorization header using basic authentication scheme",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic"
                });
            });

            // Authentication Configuration
            string? jwtKey = builder.Configuration["JwtSettings:Key"];
            string? basicAuthUser = builder.Configuration["BasicAuthSettings:Username"];

            if (!string.IsNullOrEmpty(jwtKey))
            {
                Log.Logger.Information("[AUTH] Using Bearer Authentication");
                builder.Services.AddAuthentication(x => { x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; })
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
            }
            else if (!string.IsNullOrEmpty(basicAuthUser))
            {
                Log.Logger.Information("[AUTH] Using BasicAuthentication");
                builder.Services.AddAuthentication("BasicAuthentication")
                    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
            }
            else if (bool.Parse(builder.Configuration["NTLMAuthentication"]))
            {
                Log.Logger.Information("[AUTH] Using NTLMAuthentication");
                builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
            }   
            else
            {
                Log.Logger.Warning("[AUTH] Using No Authentication");
                builder.Services.AddAuthentication("NoAuthentication")
                    .AddScheme<AuthenticationSchemeOptions, NoAuthenticationHandler>("NoAuthentication", null);
            }

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            WebApplication app = builder.Build();
            
            app.UseRouting();

            app.UseSwagger();
            app.UseSwaggerUI();

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
