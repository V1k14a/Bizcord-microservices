using MessageClient.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MessagingMicroservice.Tests.Fixture;

public class MessagingWebApplicationFactory : WebApplicationFactory<Program>
{
    public RecordingMessageClient Recording { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMessageClient>();
            services.AddSingleton<IMessageClient>(Recording);
        });
    }
}
