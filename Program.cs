using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Nexus.DataModel;
using Nexus.Extensibility;
using Nexus.Remoting;

// args
if (args.Length < 2)
    throw new Exception("No argument for address and/or port was specified.");

// get address
var address = args[0];

// get port
int port;

try
{
    port = int.Parse(args[1]);
}
catch (Exception ex)
{
    throw new Exception("The second command line argument must be a valid port number.", ex);
}

var communicator = new RemoteCommunicator(new DotnetDataSource(), address, port);
await communicator.RunAsync();

public class DotnetDataSource : IDataSource
{
    public Task SetContextAsync(DataSourceContext context, ILogger logger, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<CatalogRegistration[]> GetCatalogRegistrationsAsync(string path, CancellationToken cancellationToken)
    {
        if (path == "/")
            return Task.FromResult(new CatalogRegistration[]
                {
                    new CatalogRegistration("/A/B/C", "Test catalog /A/B/C.")
                });

        else
            return Task.FromResult(new CatalogRegistration[0]);
    }

    public Task<ResourceCatalog> GetCatalogAsync(string catalogId, CancellationToken cancellationToken)
    {
        if (catalogId == "/A/B/C")
        {
            var representation = new Representation(NexusDataType.FLOAT64, TimeSpan.FromSeconds(1));

            var resource = new ResourceBuilder("resource1")
                .WithUnit("°C")
                .WithGroups("group1")
                .AddRepresentation(representation)
                .Build();

            var catalog = new ResourceCatalogBuilder("/A/B/C")
                .WithProperty("a", "b")
                .AddResource(resource)
                .Build();

            return Task.FromResult(catalog);
        }

        else
        {
            throw new Exception("Unknown catalog identifier.");
        }
    }

    public Task<(DateTime Begin, DateTime End)> GetTimeRangeAsync(string catalogId, CancellationToken cancellationToken)
    {
        return Task.FromResult((DateTime.MinValue, DateTime.MaxValue));
    }

    public Task<double> GetAvailabilityAsync(string catalogId, DateTime begin, DateTime end, CancellationToken cancellationToken)
    {
        return Task.FromResult(1.0);
    }

    public Task ReadAsync(
        DateTime begin, 
        DateTime end, 
        ReadRequest[] requests, 
        ReadDataHandler readData, 
        IProgress<double> progress, 
        CancellationToken cancellationToken)
    {
        foreach (var request in requests)
        {
            Calculate();

            void Calculate()
            {
                var doubleData = MemoryMarshal.Cast<byte, double>(request.Data.Span);

                for (int i = 0; i < doubleData.Length; i++)
                {
                    doubleData[i] = i;
                }

                request.Status.Span.Fill(1);
            }
        }

        return Task.CompletedTask;
    }
}
