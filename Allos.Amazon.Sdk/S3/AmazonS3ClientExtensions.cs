﻿using System.Diagnostics.CodeAnalysis;
using Allos.Amazon.Sdk.Fork;
using Allos.Amazon.Sdk.S3.Transfer;
using Amazon.Runtime.SharedInterfaces;
using Amazon.S3;
using Amazon.Util.Internal;

namespace Amazon.Sdk.S3;

/// <summary>
/// Extensions implementing parts of <see cref="ICoreAmazonS3"/> relevent to <see cref="AsyncTransferUtility"/>
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[AmazonSdkFork("sdk/src/Services/S3/Custom/_async/AmazonS3Client.Extensions.cs", "Amazon.S3")]
public static class AmazonS3ClientExtensions
{
    public static Task UploadObjectFromStreamAsync(
        this IAmazonS3 s3Client, 
        string bucketName, 
        string objectKey, 
        Stream stream, 
        IDictionary<string, object> additionalProperties, 
        CancellationToken cancellationToken)
    {
        var transfer = new AsyncTransferUtility(s3Client);
        var request = new UploadRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = stream
        };
        InternalSDKUtils.ApplyValues(request, additionalProperties);
        return transfer.UploadAsync(request, cancellationToken);
    }

    public static Task UploadObjectFromFilePathAsync(
        this IAmazonS3 s3Client, 
        string bucketName, 
        string objectKey, 
        string filepath, 
        IDictionary<string, object> additionalProperties, 
        CancellationToken cancellationToken)
    {
        var transfer = new AsyncTransferUtility(s3Client);
        var request = new UploadRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            FilePath = filepath
        };
        InternalSDKUtils.ApplyValues(request, additionalProperties);

        return transfer.UploadAsync(request, cancellationToken);
    }

    public static Task DownloadToFilePathAsync(
        this IAmazonS3 s3Client, 
        string bucketName, 
        string objectKey, 
        string filepath, 
        IDictionary<string, object> additionalProperties, 
        CancellationToken cancellationToken)
    {
        var transfer = new AsyncTransferUtility(s3Client);

        var request = new DownloadRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            FilePath = filepath
        };
        InternalSDKUtils.ApplyValues(request, additionalProperties);

        return transfer.DownloadAsync(request, cancellationToken);
    }
}