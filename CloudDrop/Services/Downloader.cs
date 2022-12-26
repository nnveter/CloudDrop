using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using CloudDrop;
using Grpc.Core;
using Grpc.Net.Client;

namespace CloudDrop;

public class Downloader
{
    private readonly FileTransferService.FileTransferServiceClient client;

    public Downloader()
    {
        var channel = GrpcChannel.ForAddress(Constants.URL);
        client = new FileTransferService.FileTransferServiceClient(channel);
    }

    public async Task Download(int contentId, string token, string path)
    {
        long? totalSize = null;
        var headers = new Metadata();
        headers.Add("authorization", $"Bearer {token}");

        using (var call = client.SendFileChunks(new SendFileChunksRequest() { ContentId = contentId }, 
            headers)) {

            using (var fileStream = File.Create(path))
            {
                var responseStream = call.ResponseStream;
                while (await responseStream.MoveNext())
                {
                    var chunk = responseStream.Current;
                    if (chunk.TotalSize != null) totalSize = chunk.TotalSize;
                    await fileStream.WriteAsync(chunk.Data.ToArray());
                }
            }
        }
    }
}