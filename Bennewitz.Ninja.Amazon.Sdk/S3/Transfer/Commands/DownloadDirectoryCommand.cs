﻿using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Internal;
using Amazon.S3.Model;
using Amazon.Sdk.Fork;
using Amazon.Sdk.S3.Util;
using Amazon.Util.Internal;

namespace Amazon.Sdk.S3.Transfer.Internal
{
    [AmazonSdkFork("sdk/src/Services/S3/Custom/Transfer/Internal/DownloadDirectoryCommand.cs", "Amazon.S3.Transfer.Internal")]
    [AmazonSdkFork("sdk/src/Services/S3/Custom/Transfer/Internal/_bcl45+netstandard/DownloadDirectoryCommand.cs", "Amazon.S3.Transfer.Internal")]
    internal class DownloadDirectoryCommand : BaseCommand
    {
        private readonly TransferUtilityConfig _config;
        
        private readonly IAmazonS3 _s3Client;
        private readonly TransferUtilityDownloadDirectoryRequest _request;
        private readonly bool _skipEncryptionInstructionFiles;
        private int _totalNumberOfFilesToDownload;
        private int _numberOfFilesDownloaded;
        private long _totalBytes;
        private long _transferredBytes;
        private string? _currentFile;

        internal DownloadDirectoryCommand(IAmazonS3 s3Client, TransferUtilityDownloadDirectoryRequest request)
        {
            ArgumentNullException.ThrowIfNull(s3Client);

            _s3Client = s3Client;
            _request = request;
            _skipEncryptionInstructionFiles = s3Client is IAmazonS3Encryption;
            _config = new TransferUtilityConfig();
        }
        
        public bool DownloadFilesConcurrently { get; set; }

        internal DownloadDirectoryCommand(IAmazonS3 s3Client, TransferUtilityDownloadDirectoryRequest request, TransferUtilityConfig config)
            : this(s3Client, request)
        {
            _config = config;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            //ValidateRequest()
            if (!_request.IsSetBucketName())
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(_request.BucketName);
            }
            if (!_request.IsSetS3Directory())
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(_request.S3Directory);
            }
            if (!_request.IsSetLocalDirectory())
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(_request.LocalDirectory);
            }

            if (File.Exists(_request.S3Directory))
            {
                throw new IOException($"A file `{_request.S3Directory}` already exists with the same name specified by `{nameof(_request.S3Directory)}`");
            }
            //\
            
            EnsureDirectoryExists(new(_request.LocalDirectory));

            List<S3Object> objs;
            string listRequestPrefix;
            try
            {
                ListObjectsRequest listRequest = ConstructListObjectRequest();
                listRequestPrefix = listRequest.Prefix;
                objs = await GetS3ObjectsToDownloadAsync(listRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode != HttpStatusCode.NotImplemented)
                    throw;

                ListObjectsV2Request listRequestV2 = ConstructListObjectRequestV2();
                listRequestPrefix = listRequestV2.Prefix;
                objs = await GetS3ObjectsToDownloadV2Async(listRequestV2, cancellationToken).ConfigureAwait(false);
            }

            _totalNumberOfFilesToDownload = objs.Count;

            SemaphoreSlim? asyncThrottler = null;
            CancellationTokenSource? internalCts = null;

            try
            {
                asyncThrottler = DownloadFilesConcurrently ?
                    new(_config.ConcurrentServiceRequests) :
                    new SemaphoreSlim(1);

                internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var pendingTasks = new List<Task>();
                foreach (S3Object s3O in objs)
                {
                    if (s3O.Key.EndsWith("/", StringComparison.Ordinal))
                        continue;

                    await asyncThrottler.WaitAsync(cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    cancellationToken.ThrowIfCancellationRequested();
                    if (internalCts.IsCancellationRequested)
                    {
                        // Operation cancelled as one of the download requests failed with an exception,
                        // don't schedule any more download tasks.
                        // Don't throw an OperationCanceledException here as we want to process the 
                        // responses and throw the original exception.
                        break;
                    }

                    // Valid for serial uploads when
                    // TransferUtilityDownloadDirectoryRequest.DownloadFilesConcurrently is set to false.
                    int prefixLength = listRequestPrefix.Length;

                    // If DisableSlashCorrection is enabled (i.e. S3Directory is a key prefix) and it doesn't end with '/' then we need the parent directory to properly construct download path.
                    if (_request.DisableSlashCorrection && !listRequestPrefix.EndsWith('/'))
                    {
                        prefixLength = listRequestPrefix.LastIndexOf('/') + 1;
                    }

                    _currentFile = s3O.Key.Substring(prefixLength);

                    var downloadRequest = ConstructTransferUtilityDownloadRequest(s3O, prefixLength);
                    var command = new DownloadCommand(_s3Client, downloadRequest);

                    var task = ExecuteCommandAsync(command, internalCts, asyncThrottler);
                    pendingTasks.Add(task);
                }
                await WhenAllOrFirstExceptionAsync(pendingTasks, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            finally
            {
                internalCts?.Dispose();
                asyncThrottler?.Dispose();
            }
        }

        private async Task<List<S3Object>> GetS3ObjectsToDownloadAsync(ListObjectsRequest listRequest, CancellationToken cancellationToken)
        {
            List<S3Object> objs = new();
            do
            {
                ListObjectsResponse listResponse = await _s3Client.ListObjectsAsync(listRequest, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (listResponse.S3Objects != null)
                {
                    foreach (S3Object s3O in listResponse.S3Objects)
                    {
                        if (ShouldDownload(s3O))
                        {
                            _totalBytes += s3O.Size;
                            objs.Add(s3O);
                        }
                    }
                }
                listRequest.Marker = listResponse.NextMarker;
            } while (!string.IsNullOrWhiteSpace(listRequest.Marker));
            return objs;
        }

        private async Task<List<S3Object>> GetS3ObjectsToDownloadV2Async(ListObjectsV2Request listRequestV2, CancellationToken cancellationToken)
        {
            List<S3Object> objs = new();
            do
            {
                ListObjectsV2Response listResponse = await _s3Client.ListObjectsV2Async(listRequestV2, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (listResponse.S3Objects != null)
                {
                    foreach (S3Object s3O in listResponse.S3Objects)
                    {
                        if (ShouldDownload(s3O))
                        {
                            _totalBytes += s3O.Size;
                            objs.Add(s3O);
                        }
                    }
                }
                listRequestV2.ContinuationToken = listResponse.NextContinuationToken;
            } while (!string.IsNullOrWhiteSpace(listRequestV2.ContinuationToken));
            return objs;
        }

        private void DownloadedProgressEventCallback(object? sender, WriteObjectProgressArgs e)
        {
            var transferredBytes = Interlocked.Add(ref _transferredBytes, e.IncrementTransferred());

            int numberOfFilesDownloaded = _numberOfFilesDownloaded;
            if (e.IsCompleted)
            {
                numberOfFilesDownloaded = Interlocked.Increment(ref _numberOfFilesDownloaded);
            }

            DownloadDirectoryProgressArgs downloadDirectoryProgress;
            if (_request.DownloadFilesConcurrently)
            {
                // If concurrent download is enabled, values for current file, 
                // transferred and total bytes for current file are not set.
                downloadDirectoryProgress = new(numberOfFilesDownloaded, _totalNumberOfFilesToDownload,
                           transferredBytes, _totalBytes,
                           null, 0, 0);
            }
            else
            {
                downloadDirectoryProgress = new(numberOfFilesDownloaded, _totalNumberOfFilesToDownload,
                    transferredBytes, _totalBytes,
                    _currentFile, e.TransferredBytes, e.TotalBytes);
            }
            _request.OnRaiseProgressEvent(downloadDirectoryProgress);
        }

        private void EnsureDirectoryExists(DirectoryInfo directory)
        {
            if (directory.Exists)
                return;

            if (directory.Parent != null)
            {
                EnsureDirectoryExists(directory.Parent);    
            }
            directory.Create();
        }

        private TransferUtilityDownloadRequest ConstructTransferUtilityDownloadRequest(S3Object s3Object, int prefixLength)
        {
            var downloadRequest = new TransferUtilityDownloadRequest();
            downloadRequest.BucketName = _request.BucketName;
            downloadRequest.Key = s3Object.Key;
            var file = s3Object.Key.Substring(prefixLength).Replace('/', Path.DirectorySeparatorChar);
            downloadRequest.FilePath = _request.LocalDirectory == null ? file : Path.Combine(_request.LocalDirectory, file);
            downloadRequest.ServerSideEncryptionCustomerMethod = _request.ServerSideEncryptionCustomerMethod;
            downloadRequest.ServerSideEncryptionCustomerProvidedKey = _request.ServerSideEncryptionCustomerProvidedKey;
            downloadRequest.ServerSideEncryptionCustomerProvidedKeyMd5 = _request.ServerSideEncryptionCustomerProvidedKeyMd5;

            //Ensure the target file is a rooted within LocalDirectory. Otherwise error.
            if(!InternalSDKUtils.IsFilePathRootedWithDirectoryPath(downloadRequest.FilePath, _request.LocalDirectory))
            {
                throw new AmazonClientException($"The file `{downloadRequest.FilePath}` is not allowed outside of the target directory `{_request.LocalDirectory}`.");
            }

            downloadRequest.WriteObjectProgressEvent += DownloadedProgressEventCallback;

            return downloadRequest;
        }

        private ListObjectsV2Request ConstructListObjectRequestV2()
        {
            ListObjectsV2Request listRequestV2 = new();
            listRequestV2.BucketName = _request.BucketName;
            listRequestV2.Prefix = _request.S3Directory;

            if (listRequestV2.Prefix != null)
            {
                listRequestV2.Prefix = listRequestV2.Prefix.Replace('\\', '/');
                
                if (!_request.DisableSlashCorrection)
                {
                    if (!listRequestV2.Prefix.EndsWith('/'))
                        listRequestV2.Prefix += '/';
                }
                
                if (listRequestV2.Prefix.StartsWith('/'))
                {
                    listRequestV2.Prefix = listRequestV2.Prefix.Length == 1 ? string.Empty : listRequestV2.Prefix.Substring(1);
                }
            }

            return listRequestV2;
        }

        private ListObjectsRequest ConstructListObjectRequest()
        {
            ListObjectsRequest listRequest = new();
            listRequest.BucketName = _request.BucketName;
            listRequest.Prefix = _request.S3Directory;

            if (listRequest.Prefix != null)
            {
                listRequest.Prefix = listRequest.Prefix.Replace('\\', '/');

                if (!_request.DisableSlashCorrection)
                {
                    if (!listRequest.Prefix.EndsWith('/'))
                        listRequest.Prefix += '/';
                }

                if (listRequest.Prefix.StartsWith('/'))
                {
                    listRequest.Prefix = listRequest.Prefix.Length == 1 ? string.Empty : listRequest.Prefix.Substring(1);
                }
            }

            return listRequest;
        }


        private bool IsInstructionFile(string key)
        {
            return (_skipEncryptionInstructionFiles && AmazonS3Util.IsInstructionFile(key));
        }

        private bool ShouldDownload(S3Object s3O)
        {
            // skip objects based on ModifiedSinceDateUtc
            if (_request.IsSetModifiedSinceDateUtc() && s3O.LastModified.ToUniversalTime() <= _request.ModifiedSinceDateUtc.ToUniversalTime())
                return false;
            // skip objects based on UnmodifiedSinceDateUtc
            if (_request.IsSetUnmodifiedSinceDateUtc() && s3O.LastModified.ToUniversalTime() > _request.UnmodifiedSinceDateUtc.ToUniversalTime())
                return false;
            // skip objects which are instruction files and we're using encryption client
            if (IsInstructionFile(s3O.Key))
                return false;

            return true;
        }
    }
}
