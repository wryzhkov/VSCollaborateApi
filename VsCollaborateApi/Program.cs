using VsCollaborateApi.Services;

namespace VsCollaborateApi;

public partial class Program
{
    public static void Main(string[] args)
    {
        WebApplication app = BuildWebApp(args);

        app.Run();
    }

    public static WebApplication BuildWebApp(string[] args, Action<WebApplicationBuilder>? build = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton<IDocumentService, DocumentService>();
        builder.Services.AddSingleton<IDocumentRedactionService, DocumentRedactionService>();
        builder.Services.AddSingleton<IIdentityService, IdentityService>();
        builder.Services.AddSingleton<IDatabaseClient, DatabaseClient>((serviceProvider) =>
        {
            var conifg = serviceProvider.GetService<IConfiguration>();

            return new DatabaseClient(conifg.GetConnectionString("database"));
        });
        build?.Invoke(builder);
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseWebSockets();

        app.MapControllers();
        return app;
    }
}