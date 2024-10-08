﻿using System.Reflection;
using Amazon.Sdk.Fork;

namespace Amazon.S3.Model
{
    [AmazonSdkFork("sdk/src/Services/S3/Custom/Util/TransferProgressArgs.cs", "Amazon.S3.Util")]
    public static class TransferProgressArgsAdapter
    {
        private static readonly PropertyInfo IncrementTransferredPropertyInfo;
        
        static TransferProgressArgsAdapter()
        {
            var typeOfTransferProgressArgs = typeof(TransferProgressArgs);
            
            var incrementTransferredPropertyInfo =
                typeOfTransferProgressArgs.GetProperty(
                    "IncrementTransferred",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            
            //if this is null, the AWS sdk has changed its `internal` representation of TransferProgressArgs
            ArgumentNullException.ThrowIfNull(incrementTransferredPropertyInfo);

            IncrementTransferredPropertyInfo = incrementTransferredPropertyInfo;
        }

        public static long IncrementTransferred(this TransferProgressArgs transferProgressArgs) =>
            (long?) IncrementTransferredPropertyInfo.GetValue(transferProgressArgs) ?? default;
    }
}
