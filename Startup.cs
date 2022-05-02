using Core.Api.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: ApiController]
namespace Core.Api
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
            services.AddControllers();
            services.AddCustomControllers();
            services.AddVersioning();
            services.AddSwaggerDocumentation();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();
            app.UseSwaggerDocumentation(provider);
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

    }
    public static class ConfigurationExtensionMethods
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            services.AddSwaggerGen(options =>
            {
                // add a custom operation filter which sets default values
                options.OperationFilter<SwaggerDefaultValues>();

                // integrate xml comments
                options.IncludeXmlComments(XmlCommentsFilePath);

                options.OrderActionsBy(description =>
                {
                    ControllerActionDescriptor controllerActionDescriptor = (ControllerActionDescriptor)description.ActionDescriptor;
                    SwaggerOrderAttribute attribute = (SwaggerOrderAttribute)controllerActionDescriptor.ControllerTypeInfo.GetCustomAttribute(typeof(SwaggerOrderAttribute));
                    return string.IsNullOrEmpty(attribute?.Order?.Trim()) ? description.GroupName : attribute.Order.Trim();
                });

                //Operation security scheme based on Authorize attribute using OperationFilter()
                options.OperationFilter<SwaggerAuthOperationFilter>();
            });

            return services;
        }
        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app, IApiVersionDescriptionProvider provider)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.            
            app.UseSwagger();

            //Enable middleware to serve swagger - ui(HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(options =>
            {
                //options.RoutePrefix = "";
                // build a swagger endpoint for each discovered API version
                foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }
                options.DocExpansion(docExpansion: DocExpansion.None);
            });

            return app;
        }

        public static IServiceCollection AddVersioning(this IServiceCollection services)
        {
            //REF https://dev.to/99darshan/restful-web-api-versioning-with-asp-net-core-1e8g
            //REF https://github.com/Microsoft/aspnet-api-versioning/wiki
            services.AddApiVersioning(options =>
            {
                // specify the default API Version as 1.0
                options.DefaultApiVersion = new ApiVersion(1, 0);

                // if the client hasn't specified the API version in the request, use the default API version number 
                options.AssumeDefaultVersionWhenUnspecified = true;

                // reporting api versions will return the headers "api-supported-versions" and "api-deprecated-versions"
                options.ReportApiVersions = true;

                // DEFAULT Version reader is QueryStringApiVersionReader();
                // clients request the specific version using the X-version header
                // options.ApiVersionReader = new Microsoft.AspNetCore.Mvc.Versioning.HeaderApiVersionReader("X-version");   
                // Supporting multiple versioning scheme
                // options.ApiVersionReader = ApiVersionReader.Combine(new HeaderApiVersionReader(new[] { "api-version", "x-version", "version" }),
                // new QueryStringApiVersionReader(new[] { "api-version", "v", "version" }));//MediaTypeApiVersionReader-UrlSegmentApiVersionReader

                options.ApiVersionReader = new UrlSegmentApiVersionReader();
                options.ErrorResponses = new VersionErrorProvider();
            });

            services.AddVersionedApiExplorer(options =>
            {
                // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                // note: the specified format code will format the version as "'v'major[.minor][-status]"
                options.GroupNameFormat = "'v'VVV";

                // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                options.SubstituteApiVersionInUrl = true;
            });

            return services;
        }

        public static IServiceCollection AddCustomControllers(this IServiceCollection services)
        {
            //TODO: revisit porting to native system.text.json https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to
            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(ModelStateFilter));
                options.Filters.Add(typeof(ExceptionFilter));
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new EmptyStringToNullConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });

            return services;
        }
        public static string XmlCommentsFilePath
        {
            get
            {
                //typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            }
        }

    }

    /// <summary>
    /// Allow a conversion of empty string to null for better handling with Convert functions. E.g. Convert.ToInt64()
    /// REF : https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to
    /// </summary>
    public class EmptyStringToNullConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string value = reader.GetString();
            return value == string.Empty ? null : value.Trim();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
