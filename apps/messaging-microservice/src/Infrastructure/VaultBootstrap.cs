using Microsoft.Extensions.Configuration;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace MessagingMicroservice.Infrastructure;

/// <summary>
/// Optional startup load of secrets from HashiCorp Vault (KV v2) into configuration keys used by the host.
/// </summary>
public static class VaultBootstrap
{
    public static async Task ApplyIfConfiguredAsync(ConfigurationManager configuration)
    {
        var address = configuration["Vault:Address"];
        if (string.IsNullOrWhiteSpace(address))
            return;

        var token = configuration["Vault:Token"];
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Vault:Token is required when Vault:Address is set.");

        try
        {
            var settings = new VaultClientSettings(address.TrimEnd('/'), new TokenAuthMethodInfo(token));
            IVaultClient client = new VaultClient(settings);
            var mount = configuration["Vault:KvMount"] ?? "secret";
            var path = configuration["Vault:RabbitMqSecretPath"] ?? "messaging";

            var secret = await client.V1.Secrets.KeyValue.V2
                .ReadSecretAsync(path, mountPoint: mount)
                .ConfigureAwait(false);

            var data = secret.Data?.Data;
            if (data is null)
                return;

            if (data.TryGetValue("rabbitmq_connection_string", out var connObj)
                && connObj is not null
                && !string.IsNullOrWhiteSpace(connObj.ToString()))
            {
                configuration["RabbitMQ:ConnectionString"] = connObj.ToString();
            }
        }
        catch (Exception ex)
        {
            // Vault may be sealed or secret path missing during local bring-up; keep appsettings values.
            Console.WriteLine($"Vault bootstrap skipped: {ex.Message}");
        }
    }
}
