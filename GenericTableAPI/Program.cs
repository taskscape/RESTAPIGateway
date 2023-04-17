using GenericTableAPI.Repositories;
using GenericTableAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Serilog;

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

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSingleton(new DapperRepository(builder.Configuration.GetConnectionString("DefaultConnection"), builder.Configuration.GetValue<string>("SchemaName")));
            builder.Services.AddScoped<DapperService>();
            builder.Services.AddHttpContextAccessor();

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

            builder.Services.AddAuthentication("BasicAuthentication")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    string? token = builder.Configuration.GetSection("JwtSettings:Key").Value;
                    if (token != null)
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(token)),
                            ValidateIssuer = false,
                            ValidateAudience = false
                        };
                });


            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            WebApplication app = builder.Build();
            
            app.UseRouting();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
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
                    defaults: new { controller = "DapperController" }
                );
            });
            
            app.MapControllers();

            app.Run();
        }
    }
}