using System.Runtime.InteropServices;
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

public class DotnetDataSource : SimpleDataSource
{
    public override Task<CatalogRegistration[]> GetCatalogRegistrationsAsync(string path, CancellationToken cancellationToken)
    {
        if (path == "/")
            return Task.FromResult(new CatalogRegistration[]
                {
                    new CatalogRegistration("/A/B/C", "Test catalog /A/B/C.")
                });

        else
            return Task.FromResult(new CatalogRegistration[0]);
    }

    public override Task<ResourceCatalog> GetCatalogAsync(string catalogId, CancellationToken cancellationToken)
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
                .AddResource(resource)
                .Build();

            return Task.FromResult(catalog);
        }

        else
        {
            throw new Exception("Unknown catalog identifier.");
        }
    }

    public override async Task ReadAsync(
        DateTime begin, 
        DateTime end, 
        ReadRequest[] requests, 
        ReadDataHandler readData, 
        IProgress<double> progress, 
        CancellationToken cancellationToken)
    {
        var temperatureData = await readData.Invoke("/SAMPLE/LOCAL/T1", begin, end, cancellationToken);

        foreach (var request in requests)
        {
            Calculate();

            /* this nested sync method is required because spans cannot be accessed in async methods */
            void Calculate()
            {
                /* generate data */
                var temperatureBuffer = temperatureData.Span;
                var resultBuffer = MemoryMarshal.Cast<byte, double>(request.Data.Span);

                for (int i = 0; i < resultBuffer.Length; i++)
                {
                    /* example: multiply by two */
                    resultBuffer[i] = temperatureBuffer[i] * 2;
                }

                /* mark all data as valid */
                request.Status.Span.Fill(1);
            }
        }
    }
}
