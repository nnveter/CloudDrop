
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CloudDrop;

public class Downloader
{
    public delegate void ProgressChangedEventHandler(KeyValuePair<string, double> data);
    public delegate void DownloadCompletedEventHandler(bool IsCompleted);


    public event ProgressChangedEventHandler ProgressChanged;
    public event DownloadCompletedEventHandler DownloadCompleted;

    private readonly FileTransferService.FileTransferServiceClient client;

    public Downloader()
    {
        var channel = GrpcChannel.ForAddress(Constants.URL);
        client = new FileTransferService.FileTransferServiceClient(channel);
    }

    public async Task<bool> Download(int contentId, string token, string path, string fileName)
    {
        long? totalSize = null;
        var headers = new Metadata();
        headers.Add("authorization", $"Bearer {token}");

        double progress = -1;

        try
        {
            client.SendFileStateChange(new SendFileStateChangeRequest() { ContentId = contentId, State = SendFileStateChangeEnum.Start }, headers);

            using (var call = client.SendFileChunks(new SendFileChunksRequest() { ContentId = contentId },
                headers))
            {

                using (var fileStream = File.Create(path))
                {
                    var responseStream = call.ResponseStream;
                    while (await responseStream.MoveNext())
                    {
                        var chunk = responseStream.Current;
                        totalSize = chunk.TotalSize;
                        await fileStream.WriteAsync(chunk.Data.ToArray());
                        if ((int)(fileStream.Length * 100 / totalSize) != progress)
                            ProgressChanged?.Invoke(new KeyValuePair<string, double>(fileName, (int)(fileStream.Length * 100 / totalSize)));
                        progress = (double)(fileStream.Length * 100 / totalSize);
                    }
                    DownloadCompleted?.Invoke(true);
                }
            }
        }
        catch
        {
            return false;
        }
        return true;
    }
}