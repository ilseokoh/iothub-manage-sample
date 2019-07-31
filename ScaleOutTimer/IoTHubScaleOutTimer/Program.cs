using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Management.IotHub;
using Microsoft.Azure.Management.IotHub.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace IoTHubScaleOutTimer
{
    class Program
    {
        static ServiceClient serviceClient;

        static string ApplicationId = "<application id>";
        static string SubscriptionId = "<Subscription Id>";
        static string TenantId = "<Tenant Id>";
        static string ApplicationPassword = "<Application Password>";
        static string ResourceGroupName = "<ResourceGroup name>";
        static string IotHubName = "<IotHub Name>";

        static async Task Main(string[] args)
        {
            // connect management lib to iotHub
            IotHubClient client = GetNewIotHubClient();
            if (client == null)
            {
                Console.WriteLine("Unable to create IotHub client");
                return;
            }

            IotHubDescription desc = client.IotHubResource.Get(ResourceGroupName, IotHubName);
            string currentSKU = desc.Sku.Name;
            long currentUnits = desc.Sku.Capacity.Value;

            Console.WriteLine("Current SKU Tier: " + desc.Sku.Tier);
            Console.WriteLine("Current SKU Name: " + currentSKU);
            Console.WriteLine("Current SKU Capacity: " + currentUnits.ToString());

            // update the IoT Hub description with the new sku level and units
            desc.Sku.Name = "S3";
            desc.Sku.Capacity = 2;
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            client.IotHubResource.CreateOrUpdate(ResourceGroupName, IotHubName, desc);

            stopwatch.Stop(); 

            TimeSpan t = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            string answer = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                                    t.Hours,
                                    t.Minutes,
                                    t.Seconds,
                                    t.Milliseconds);

            Console.WriteLine($"Elapsed Time: {answer}");
            Console.WriteLine($"Changed SKU Capacity: {desc.Sku.Capacity}");
            Console.WriteLine($"Changed SKU Name: {desc.Sku.Name}");
        }

        private static IotHubClient GetNewIotHubClient()
        {
            var authContext = new AuthenticationContext(string.Format("https://login.microsoftonline.com/{0}", TenantId));
            var credential = new ClientCredential(ApplicationId, ApplicationPassword);
            AuthenticationResult token = authContext.AcquireTokenAsync("https://management.core.windows.net/", credential).Result;
            if (token == null)
            {
                Console.WriteLine("Failed to obtain the authentication token");
                return null;
            }

            var creds = new TokenCredentials(token.AccessToken);
            var client = new IotHubClient(creds);
            client.SubscriptionId = SubscriptionId;

            return client;
        }
    }
}
