using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CloudDrop
{
    public class FileTransfer
    {
        public delegate void UploadErrorHandler(RpcException ex);
        public delegate void FileTransferEventHandler(string message);
        public delegate void PercentHandler(double percent);

        public delegate void MultiRepcentHandler(KeyValuePair<string, double> data);
        public event MultiRepcentHandler MultiPercentOfUpload;

        public event FileTransferEventHandler UploadStarted;
        public event FileTransferEventHandler UploadFinished;
        public event UploadErrorHandler UploadError;
        public event PercentHandler ChangedPercentOfUpload;

        private int _sizeOfChunk = 1024 * 1024;
        private string _serverUrl = "http://localhost:5100";
        public FileTransfer(int? sizeOfChunk = null, string serverUrl = null)
        {
            if (sizeOfChunk != null) _sizeOfChunk = (int)sizeOfChunk;
            if (serverUrl != null) _serverUrl = serverUrl;
        }

        public async void Upload(string token, string fileName, string fileType, int storageId, string uploadingFilePath, int? parentId)
        {
            using (var channel = GrpcChannel.ForAddress(_serverUrl))
            {
                var headers = new Metadata();
                headers.Add("authorization", $"Bearer {token}");
                var client = new FileTransferService.FileTransferServiceClient(channel);
                var startRequest = new StartRequest
                {
                    Name = fileName,
                    Type = fileType,
                    StorageId = storageId,
                    ParentId = parentId
                };
                var response = client.StartReceivingFile(startRequest, headers);
                var contentid = response.ContentId;

                UploadStarted?.Invoke("Upload started!");
                using (var call = client.ReceiveFileChunk())
                {
                    FileInfo fileInfo = new FileInfo(uploadingFilePath);
                    long fileSize = fileInfo.Length;
                    int chunkSize = _sizeOfChunk;
                    var chunkCount = (int)Math.Ceiling(fileSize / (double)chunkSize);
                    byte[] buffer = new byte[chunkSize];
                    int bytesRead;

                    using (FileStream stream = new FileStream(uploadingFilePath, FileMode.Open))
                    {
                        int i = 0;
                        // читаем файл по chunkSize байт за раз
                        while ((bytesRead = stream.Read(buffer, 0, chunkSize)) > 0)
                        {
                            ChangedPercentOfUpload?.Invoke((double)i / chunkCount * 100);
                            MultiPercentOfUpload?.Invoke(new KeyValuePair<string, double>(fileName, (double)i / chunkCount * 100));

                            var chunkBytes = buffer.Take(bytesRead).ToArray();

                            var chunk = new Chunk
                            {
                                Data = ByteString.CopyFrom(chunkBytes),
                                FilePath = response.FilePath,
                                ContentId = contentid,
                            };
                            await call.RequestStream.WriteAsync(chunk);
                            i++;
                        }
                    }
                    await call.RequestStream.CompleteAsync();
                    try
                    {
                        var response2 = await call.ResponseAsync;
                        Console.WriteLine(response2);
                        MultiPercentOfUpload?.Invoke(new KeyValuePair<string, double>(fileName, 100));
                        UploadFinished?.Invoke($"Upload finished. Chunks: {chunkCount}");
                    }
                    catch (RpcException ex)
                    {
                        UploadError?.Invoke(ex);
                    }
                }
            }
        }
    }
}