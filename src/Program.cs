using System;
using System.IO;
using System.Linq;
using System.Net;
using DnsClient;
using DnsClient.Protocol;
using Amazon.Route53.Model;
using Amazon.Route53;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using System.Diagnostics;

namespace ddns_in_route53
{
    // ENV variables
    // FQDN
    // ZoneId
    // AccessKeyid
    // SecretAccessKey

    class Program
    {
        private static void CheckEnvVariables()
        {
            Environment.GetEnvironmentVariable("DDNS_FQDN").ShouldNotBeNullOrWhiteSpace();
            Environment.GetEnvironmentVariable("DDNS_ZoneId").ShouldNotBeNullOrWhiteSpace();
            Environment.GetEnvironmentVariable("DDNS_AccessKeyId").ShouldNotBeNullOrWhiteSpace();
            Environment.GetEnvironmentVariable("DDNS_SecretAccessKey").ShouldNotBeNullOrWhiteSpace();
        }

        static async Task Main(string[] args)
        {
            CheckEnvVariables();

            var FQDN = Environment.GetEnvironmentVariable("DDNS_FQDN");
            var ZoneId = Environment.GetEnvironmentVariable("DDNS_ZoneId");
            var AccessKeyId = Environment.GetEnvironmentVariable("DDNS_AccessKeyId");
            var SecretAccessKey = Environment.GetEnvironmentVariable("DDNS_SecretAccessKey");

            var client = new LookupClient(new LookupClientOptions { UseCache = false });
            var result = client.Query(FQDN, QueryType.A);

            var record = result.Answers.OfType<ARecord>().FirstOrDefault();
            if (record == null)
            {
                Console.WriteLine($"Unexpected: There is no A record for {FQDN}");
                Environment.Exit(501);
            }
            else
            {
                Console.WriteLine($"Address: {record.Address}");
            }

            var publicIp = GetPublicIPAddress();
            if (string.IsNullOrEmpty(publicIp))
            {
                Console.WriteLine("Unexpected: Cannot detect the public IP");
                Environment.Exit(502);
            }
            else
            {
                Console.WriteLine($"Public IP: {publicIp}");
            }

            if (record.Address.ToString() == publicIp)
            {
                Console.WriteLine("Nothing to do. Our Public IP matches DNS");
            }
            else
            {
                Console.WriteLine("IP Address changed detected. Updating...");
                await UpdateR53Async(ZoneId, FQDN, publicIp, AccessKeyId, SecretAccessKey);
            }
           
        }

        private static async Task UpdateR53Async(string zoneId, string recordName, string publicIp, string accessKeyId, string secretAccessKey)
        {
            var client = new AmazonRoute53Client(accessKeyId, secretAccessKey, Amazon.RegionEndpoint.USEast1);

            var recordSet = new ResourceRecordSet
            {
                Name = recordName,
                TTL = 60,
                Type = RRType.A,
                ResourceRecords = new List<ResourceRecord> { new ResourceRecord { Value = publicIp } }
            };

            var change1 = new Change { ResourceRecordSet = recordSet, Action = ChangeAction.UPSERT };

            var changeBatch = new ChangeBatch { Changes = new List<Change> { change1 } };


            var request = new ChangeResourceRecordSetsRequest { HostedZoneId = zoneId, ChangeBatch = changeBatch };

            var response = await client.ChangeResourceRecordSetsAsync(request);
        }

        static string GetPublicIPAddress()
        {
            String address = "";
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                address = stream.ReadToEnd();
            }

            int first = address.IndexOf("Address: ") + 9;
            int last = address.LastIndexOf("</body>");
            address = address.Substring(first, last - first);

            return address;
        }
    }
}
