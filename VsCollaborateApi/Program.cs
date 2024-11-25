using System.Net.NetworkInformation;
using System.Net.Sockets;
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
            var config = serviceProvider.GetService<IConfiguration>();

            return new DatabaseClient(config.GetConnectionString("database"));
        });
        var hostOptions = builder.Configuration.GetSection("HostOptions");

        builder.WebHost.ConfigureKestrel(options =>
        {
            bool useLocalhost = hostOptions.GetValue<bool>("UseLocalhost");
            int port = hostOptions.GetValue<int>("Port");

            if (useLocalhost)
            {
                options.ListenLocalhost(port);
            }
            else
            {
                string ipAddress = GetLocalIPAddress();
                options.Listen(System.Net.IPAddress.Parse(ipAddress), port);
            }
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

    private static string GetLocalIPAddress()
    {
        // Iterate through network interfaces to find a valid IPv4 address
        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString(); // Return the first valid IPv4 address
                    }
                }
            }
        }
        return null;
    }
}