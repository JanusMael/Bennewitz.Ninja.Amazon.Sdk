﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Amazon.Sdk.Fork;

namespace Amazon.Sdk.S3.Transfer;

/// <summary>
/// Encapsulates the information needed to provide
/// transfer progress to subscribers of the <c>DownloadDirectory</c>
/// event.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[AmazonSdkFork("sdk/src/Services/S3/Custom/Transfer/TransferUtilityDownloadDirectoryRequest.cs", "Amazon.S3.Transfer")]
public class DownloadDirectoryProgressArgs : EventArgs
{
    /// <summary>
    /// Constructs a new instance of <c>DownloadDirectoryProgressArgs</c>.
    /// </summary>
    /// <param name="numberOfFilesDownloaded">
    /// The number of files downloaded.
    /// </param>
    /// <param name="totalNumberOfFiles">
    /// The total number of files to download.
    /// </param>
    /// <param name="currentFile">
    /// The current file being downloaded
    /// </param>
    /// <param name="transferredBytesForCurrentFile">
    /// The number of transferred bytes for the current file.
    /// </param>
    /// <param name="totalNumberOfBytesForCurrentFile">
    /// The size of the current file in bytes.
    /// </param>
    public DownloadDirectoryProgressArgs(int numberOfFilesDownloaded, int totalNumberOfFiles,
        string? currentFile, long transferredBytesForCurrentFile, long totalNumberOfBytesForCurrentFile)
    {
        NumberOfFilesDownloaded = numberOfFilesDownloaded;
        TotalNumberOfFiles = totalNumberOfFiles;
        CurrentFile = currentFile;
        TransferredBytesForCurrentFile = transferredBytesForCurrentFile;
        TotalNumberOfBytesForCurrentFile = totalNumberOfBytesForCurrentFile;
    }

    /// <summary>
    /// Constructs a new instance of <c>DownloadDirectoryProgressArgs</c>.
    /// </summary>
    /// <param name="numberOfFilesDownloaded">
    /// The number of files downloaded.
    /// </param>
    /// <param name="totalNumberOfFiles">
    /// The total number of files to download.
    /// </param>
    /// <param name="transferredBytes">
    /// The bytes transferred across all files being downloaded.
    /// </param>
    /// <param name="totalBytes">
    /// The total number of bytes across all files being downloaded.
    /// </param>
    /// <param name="currentFile">
    /// The current file being downloaded.
    /// </param>
    /// <param name="transferredBytesForCurrentFile">
    /// The number of transferred bytes for the current file.
    /// </param>
    /// <param name="totalNumberOfBytesForCurrentFile">
    /// The size of the current file in bytes.
    /// </param>
    public DownloadDirectoryProgressArgs(int numberOfFilesDownloaded, int totalNumberOfFiles, long transferredBytes, long totalBytes,
        string? currentFile, long transferredBytesForCurrentFile, long totalNumberOfBytesForCurrentFile)
    {
        NumberOfFilesDownloaded = numberOfFilesDownloaded;
        TotalNumberOfFiles = totalNumberOfFiles;
        TransferredBytes = transferredBytes;
        TotalBytes = totalBytes;
        CurrentFile = currentFile;
        TransferredBytesForCurrentFile = transferredBytesForCurrentFile;
        TotalNumberOfBytesForCurrentFile = totalNumberOfBytesForCurrentFile;
    }

    /// <summary>
    /// Gets or sets the total number of files.
    /// </summary>
    /// <value>The total number of files.</value>
    public int TotalNumberOfFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files downloaded so far.
    /// </summary>
    /// <value>The number of files downloaded.</value>
    public int NumberOfFilesDownloaded { get; set; }

    /// <summary>
    /// Gets or sets the total number of bytes across all files being downloaded.
    /// </summary>
    /// <value>The total number of bytes across all files being downloaded.</value>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets the bytes transferred across all files being downloaded.
    /// </summary>
    /// <value>The bytes transferred across all files being downloaded.</value>
    public long TransferredBytes { get; set; }

        
    /// <summary>
    /// Gets or sets the current file being downloaded.
    /// </summary>
    /// <remarks>
    /// This property is only valid if DownloadDirectory is used without enabling concurrent file downloads (by default concurrent download is disabled).
    /// If concurrent file downloads are enabled by setting TransferUtilityDownloadDirectoryRequest.DownloadFilesConcurrently to true, this property
    /// will return null.
    /// </remarks>
    /// <value>The current file being downloaded.</value>
    public string? CurrentFile { get; set; }

    /// <summary>
    /// Gets or sets the transferred bytes for the current file.
    /// </summary>
    /// <remarks>
    /// This property is only valid if DownloadDirectory is used without enabling concurrent file downloads (by default concurrent download is disabled).
    /// If concurrent file downloads are enabled by setting TransferUtilityDownloadDirectoryRequest.DownloadFilesConcurrently to true, this property
    /// will return 0.
    /// </remarks>
    /// <value>The transferred bytes for the current file.</value>
    public long TransferredBytesForCurrentFile { get; set; }

    /// <summary>
    /// Gets or sets the total number of bytes for the current file.
    /// </summary>
    /// <remarks>
    /// This property is only valid if DownloadDirectory is used without enabling concurrent file downloads (by default concurrent download is disabled).
    /// If concurrent file downloads are enabled by setting TransferUtilityDownloadDirectoryRequest.DownloadFilesConcurrently to true, this property
    /// will return 0.
    /// </remarks>
    /// <value>The total number of bytes for the current file.</value>
    public long TotalNumberOfBytesForCurrentFile { get; set; }

    /// <summary>
    /// The string representation of this instance of DownloadDirectoryProgressArgs.
    /// </summary>
    /// <returns>The string representation of this instance of DownloadDirectoryProgressArgs.</returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "Total Files: {0}, Downloaded Files {1}, Total Bytes: {2}, Transferred Bytes: {3}",
            TotalNumberOfFiles, NumberOfFilesDownloaded, TotalBytes, TransferredBytes);
    }
}