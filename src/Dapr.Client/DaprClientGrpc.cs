﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapr.Client.Autogen.Grpc;
    using Google.Protobuf;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using Grpc.Net.Client;
    using Autogenerated = Dapr.Client.Autogen.Grpc;

    /// <summary>
    /// A client for interacting with the Dapr endpoints.
    /// </summary>
    internal class DaprClientGrpc : DaprClient
    {
        private readonly Autogenerated.Dapr.DaprClient client;
        private readonly JsonSerializerOptions jsonSerializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DaprClientGrpc"/> class.
        /// </summary>
        /// <param name="channel">gRPC channel to create gRPC clients.</param>
        /// <param name="jsonSerializerOptions">Json serialization options.</param>
        internal DaprClientGrpc(GrpcChannel channel, JsonSerializerOptions jsonSerializerOptions = null)
        {
            this.jsonSerializerOptions = jsonSerializerOptions;
            this.client = new Autogenerated.Dapr.DaprClient(channel);
        }

        #region Publish Apis
        /// <inheritdoc/>
        public override Task PublishEventAsync<TContent>(string topicName, TContent content, CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(topicName, nameof(topicName));
            ArgumentVerifier.ThrowIfNull(content, nameof(content));
            return MakePublishRequest(topicName, content, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task PublishEventAsync(string topicName, CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(topicName, nameof(topicName));
            return MakePublishRequest(topicName, string.Empty, cancellationToken);
        }

        private async Task MakePublishRequest<TContent>(string topicName, TContent content, CancellationToken cancellationToken)
        {
            // Create PublishEventEnvelope
            var envelope = new Autogenerated.PublishEventEnvelope()
            {
                Topic = topicName,
            };

            if (content != null)
            {
                envelope.Data = ConvertToAnyAsync(content, this.jsonSerializerOptions);
            }

            await this.MakeGrpcCallHandleError(
                (options) =>
                {
                    return client.PublishEventAsync(envelope, options);
                },
                cancellationToken);
        }
        #endregion

        #region InvokeBinding Apis
        public override async Task InvokeBindingAsync<TContent>(
           string name,
           TContent content,
           Dictionary<string, string> metadata = default,
           CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(name, nameof(name));

            var envelope = new Autogenerated.InvokeBindingEnvelope()
            {
                Name = name,
            };

            if (content != null)
            {
                envelope.Data = ConvertToAnyAsync(content, this.jsonSerializerOptions);
            }

            if (metadata != null)
            {
                envelope.Metadata.Add(metadata);
            }

            await this.MakeGrpcCallHandleError(
                (options) =>
                {
                    return client.InvokeBindingAsync(envelope, options);
                },
                cancellationToken);
        }
        #endregion

        #region InvokeMethod Apis
        public override async Task InvokeMethodAsync(
           string appId,
           string methodName,
           Dictionary<string, string> metadata = default,
           CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(appId, nameof(appId));
            ArgumentVerifier.ThrowIfNullOrEmpty(methodName, nameof(methodName));

            _ = await this.MakeInvokeRequestAsync(appId, methodName, null, metadata, cancellationToken);
        }

        public override async Task InvokeMethodAsync<TRequest>(
           string appId,
           string methodName,
           TRequest data,
           Dictionary<string, string> metadata = default,
           CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(appId, nameof(appId));
            ArgumentVerifier.ThrowIfNullOrEmpty(methodName, nameof(methodName));

            Any serializedData = null;
            if (data != null)
            {
                serializedData = ConvertToAnyAsync(data, this.jsonSerializerOptions);
            }

            _ = await this.MakeInvokeRequestAsync(appId, methodName, serializedData, metadata, cancellationToken);
        }

        public override async ValueTask<TResponse> InvokeMethodAsync<TResponse>(
           string appId,
           string methodName,
           Dictionary<string, string> metadata = default,
           CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(appId, nameof(appId));
            ArgumentVerifier.ThrowIfNullOrEmpty(methodName, nameof(methodName));

            var response = await this.MakeInvokeRequestAsync(appId, methodName, null, metadata, cancellationToken);
            if (response.Data.Value.IsEmpty)
            {
                return default;
            }

            var responseData = response.Data.Value.ToStringUtf8();
            return JsonSerializer.Deserialize<TResponse>(responseData, this.jsonSerializerOptions);
        }

        public override async ValueTask<TResponse> InvokeMethodAsync<TRequest, TResponse>(
            string appId,
            string methodName,
            TRequest data,
            Dictionary<string, string> metadata = default,
            CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(appId, nameof(appId));
            ArgumentVerifier.ThrowIfNullOrEmpty(methodName, nameof(methodName));

            Any serializedData = null;
            if (data != null)
            {
                serializedData = ConvertToAnyAsync(data, this.jsonSerializerOptions);
            }

            var response = await this.MakeInvokeRequestAsync(appId, methodName, serializedData, metadata, cancellationToken);
            if (response.Data.Value.IsEmpty)
            {
                return default;
            }

            var responseData = response.Data.Value.ToStringUtf8();
            return JsonSerializer.Deserialize<TResponse>(responseData, this.jsonSerializerOptions);
        }

        private async Task<InvokeServiceResponseEnvelope> MakeInvokeRequestAsync(
            string appId,
            string methodName,
            Any data,
            Dictionary<string, string> metadata = default,
            CancellationToken cancellationToken = default)
        {
            var envelope = new Autogenerated.InvokeServiceEnvelope()
            {
                Id = appId,
                Method = methodName
            };

            if (data != null)
            {
                envelope.Data = data;
            }
                                     
            if (metadata != null)
            {
                envelope.Metadata.Add(metadata);
            }

            return await this.MakeGrpcCallHandleError(
                (options) =>
                {
                    return client.InvokeServiceAsync(envelope, options);
                },
                cancellationToken);
        }
        #endregion

        #region State Apis
        /// <inheritdoc/>
        public override async ValueTask<TValue> GetStateAsync<TValue>(string storeName, string key, ConsistencyMode? consistencyMode = default, CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(storeName, nameof(storeName));
            ArgumentVerifier.ThrowIfNullOrEmpty(key, nameof(key));

            var getStateEnvelope = new GetStateEnvelope()
            {
                StoreName = storeName,
                Key = key,
            };

            if (consistencyMode != null)
            {
                getStateEnvelope.Consistency = GetStringForConsistencyMode(consistencyMode.Value);
            }

            var response = await this.MakeGrpcCallHandleError(
                (options) =>
                {
                    return client.GetStateAsync(getStateEnvelope, options);
                },
                cancellationToken);

            if (response.Data.Value.IsEmpty)
            {
                return default;
            }

            var responseData = response.Data.Value.ToStringUtf8();
            return JsonSerializer.Deserialize<TValue>(responseData, this.jsonSerializerOptions);
        }

        /// <inheritdoc/>
        public override async ValueTask<(TValue value, string etag)> GetStateAndETagAsync<TValue>(string storeName, string key, ConsistencyMode? consistencyMode = default, CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(storeName, nameof(storeName));
            ArgumentVerifier.ThrowIfNullOrEmpty(key, nameof(key));

            var getStateEnvelope = new GetStateEnvelope()
            {
                StoreName = storeName,
                Key = key,
            };

            if (consistencyMode != null)
            {
                getStateEnvelope.Consistency = GetStringForConsistencyMode(consistencyMode.Value);
            }

            var response = await this.MakeGrpcCallHandleError(
                (options) =>
                {
                    return client.GetStateAsync(getStateEnvelope, options);
                },
                cancellationToken);

            if (response.Data.Value.IsEmpty)
            {
                return (default(TValue), response.Etag);
            }

            var responseData = response.Data.Value.ToStringUtf8();
            var deserialized = JsonSerializer.Deserialize<TValue>(responseData, this.jsonSerializerOptions);
            return (deserialized, response.Etag);
        }

        /// <inheritdoc/>
        public override async Task SaveStateAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            StateOptions stateOptions = default,
            Dictionary<string, string> metadata = default,
            CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(storeName, nameof(storeName));
            ArgumentVerifier.ThrowIfNullOrEmpty(key, nameof(key));

            await this.MakeSaveStateCallAsync<TValue>(
                storeName,
                key,
                value,
                etag: null,
                stateOptions,
                metadata,
                cancellationToken);
        }

        /// <inheritdoc/>
        public override async ValueTask<bool> TrySaveStateAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            StateOptions stateOptions = default,
            Dictionary<string, string> metadata = default,
            CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(storeName, nameof(storeName));
            ArgumentVerifier.ThrowIfNullOrEmpty(key, nameof(key));

            try
            {
                await this.MakeSaveStateCallAsync<TValue>(storeName, key, value, etag, stateOptions, metadata, cancellationToken);
                return true;
            }
            catch (RpcException)
            { }

            return false;
        }

        internal async ValueTask MakeSaveStateCallAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag = default,
            StateOptions stateOptions = default,
            Dictionary<string, string> metadata = default,
            CancellationToken cancellationToken = default)
        {
            // Create PublishEventEnvelope
            var saveStateEnvelope = new Autogenerated.SaveStateEnvelope()
            {
                StoreName = storeName,
            };

            var stateRequest = new Autogenerated.StateRequest()
            {
                Key = key,
            };

            if (metadata != null)
            {
                stateRequest.Metadata.Add(metadata);
            }

            if (etag != null)
            {
                stateRequest.Etag = etag;
            }

            if (stateOptions != null)
            {
                stateRequest.Options = ToAutoGeneratedStateRequestOptions(stateOptions);
            }

            if (value != null)
            {
                stateRequest.Value = ConvertToAnyAsync(value, this.jsonSerializerOptions);
            }

            saveStateEnvelope.Requests.Add(stateRequest);

            await this.MakeGrpcCallHandleError(
                (options) =>
                {
                    return client.SaveStateAsync(saveStateEnvelope, options);
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task DeleteStateAsync(
            string storeName,
            string key,
            StateOptions stateOptions = default,
            CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(storeName, nameof(storeName));
            ArgumentVerifier.ThrowIfNullOrEmpty(key, nameof(key));

            await this.MakeDeleteStateCallsync(
                storeName,
                key,
                etag: null,
                stateOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public override async ValueTask<bool> TryDeleteStateAsync(
            string storeName,
            string key,
            string etag = default,
            StateOptions stateOptions = default,
            CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(storeName, nameof(storeName));
            ArgumentVerifier.ThrowIfNullOrEmpty(key, nameof(key));

            try
            {
                await this.MakeDeleteStateCallsync(storeName, key, etag, stateOptions, cancellationToken);
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        private async ValueTask MakeDeleteStateCallsync(
           string storeName,
           string key,
           string etag = default,
           StateOptions stateOptions = default,
           CancellationToken cancellationToken = default)
        {
            var deleteStateEnvelope = new DeleteStateEnvelope()
            {
                StoreName = storeName,
                Key = key,
            };

            if (etag != null)
            {
                deleteStateEnvelope.Etag = etag;
            }

            if (stateOptions != null)
            {
                deleteStateEnvelope.Options = ToAutoGeneratedStateOptions(stateOptions);
            }

            await this.MakeGrpcCallHandleError(
                (options) =>
                {
                    return client.DeleteStateAsync(deleteStateEnvelope, options);
                },
                cancellationToken);
        }
        #endregion

        #region Secret Apis
        /// <inheritdoc/>
        public async override ValueTask<Dictionary<string, string>> GetSecretAsync(
            string storeName,
            string key,
            Dictionary<string, string> metadata = default,
            CancellationToken cancellationToken = default)
        {
            ArgumentVerifier.ThrowIfNullOrEmpty(storeName, nameof(storeName));
            ArgumentVerifier.ThrowIfNullOrEmpty(key, nameof(key));

            var envelope = new Autogenerated.GetSecretEnvelope()
            {
                StoreName = storeName,
                Key = key
            };

            if (metadata != null)
            {
                envelope.Metadata.Add(metadata);
            }

            var response = await this.MakeGrpcCallHandleError(
                 (options) =>
                 {
                     return client.GetSecretAsync(envelope, options);
                 },
                 cancellationToken);

            return response.Data.ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        #endregion

        #region Helper Methods

        /// <summary>
        /// Makes Grpc call using the cancellationToken and handles Errors.
        /// All common exception handling logic will reside here.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="callFunc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<TResponse> MakeGrpcCallHandleError<TResponse>(Func<CallOptions, AsyncUnaryCall<TResponse>> callFunc, CancellationToken cancellationToken = default)
        {
            var callOptions = new CallOptions(cancellationToken: cancellationToken);

            // COmmon Exception Handling logic can be added here for all calls.
            return await callFunc.Invoke(callOptions);
        }

        private StateRequestOptions ToAutoGeneratedStateRequestOptions(StateOptions stateOptions)
        {
            var stateRequestOptions = new Autogenerated.StateRequestOptions();

            if (stateOptions.Consistency != null)
            {
                stateRequestOptions.Consistency = GetStringForConsistencyMode(stateOptions.Consistency.Value);
            }

            if (stateOptions.Concurrency != null)
            {
                stateRequestOptions.Concurrency = GetStringForConcurrencyMode(stateOptions.Concurrency.Value);
            }

            if (stateOptions.RetryOptions != null)
            {
                var retryPolicy = new Autogenerated.StateRetryPolicy();
                if (stateOptions.RetryOptions.RetryMode != null)
                {
                    retryPolicy.Pattern = GetStringForRetryMode(stateOptions.RetryOptions.RetryMode.Value);
                }

                if (stateOptions.RetryOptions.RetryInterval != null)
                {
                    retryPolicy.Interval = Duration.FromTimeSpan(stateOptions.RetryOptions.RetryInterval.Value);
                }

                if (stateOptions.RetryOptions.RetryThreshold != null)
                {
                    retryPolicy.Threshold = stateOptions.RetryOptions.RetryThreshold.Value;
                }

                stateRequestOptions.RetryPolicy = retryPolicy;
            }

            return stateRequestOptions;
        }

        private Autogenerated.StateOptions ToAutoGeneratedStateOptions(StateOptions stateOptions)
        {
            var stateRequestOptions = new Autogenerated.StateOptions();

            if (stateOptions.Consistency != null)
            {
                stateRequestOptions.Consistency = GetStringForConsistencyMode(stateOptions.Consistency.Value);
            }

            if (stateOptions.Concurrency != null)
            {
                stateRequestOptions.Concurrency = GetStringForConcurrencyMode(stateOptions.Concurrency.Value);                
            }

            if (stateOptions.RetryOptions != null)
            {
                var retryPolicy = new Autogenerated.RetryPolicy();
                if (stateOptions.RetryOptions.RetryMode != null)
                {
                    retryPolicy.Pattern = GetStringForRetryMode(stateOptions.RetryOptions.RetryMode.Value);
                }

                if (stateOptions.RetryOptions.RetryInterval != null)
                {
                    retryPolicy.Interval = Duration.FromTimeSpan(stateOptions.RetryOptions.RetryInterval.Value);
                }

                if (stateOptions.RetryOptions.RetryThreshold != null)
                {
                    retryPolicy.Threshold = stateOptions.RetryOptions.RetryThreshold.Value;
                }

                stateRequestOptions.RetryPolicy = retryPolicy;
            }

            return stateRequestOptions;
        }

        private static Any ConvertToAnyAsync<T>(T data, JsonSerializerOptions options = null)
        {
            var any = new Any();

            if (data != null)
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(data, options);
                any.Value = ByteString.CopyFrom(bytes);

            }

            return any;
        }

        private static string GetStringForConsistencyMode(ConsistencyMode consistencyMode)
        {
            if (consistencyMode.Equals(ConsistencyMode.Eventual))
            {
                return Constants.Eventual;
            }

            if (consistencyMode.Equals(ConsistencyMode.Strong))
            {
                return Constants.Strong;
            }

            throw new ArgumentException($"{consistencyMode.ToString()} Consistency Mode is not supported.");
        }    

        private static string GetStringForConcurrencyMode(ConcurrencyMode concurrencyMode)
        {
            if (concurrencyMode.Equals(ConcurrencyMode.FirstWrite))
            {
                return Constants.FirstWrite;
            }

            if (concurrencyMode.Equals(ConcurrencyMode.LastWrite))
            {
                return Constants.LastWrite;
            }

            throw new ArgumentException($"{concurrencyMode.ToString()} Concurrency Mode is not supported.");
        }

        private static string GetStringForRetryMode(RetryMode retryMode)
        {
            if (retryMode.Equals(RetryMode.Exponential))
            {
                return Constants.Exponential;
            }

            if (retryMode.Equals(RetryMode.Linear))
            {
                return Constants.Linear;
            }

            throw new ArgumentException($"{retryMode.ToString()} Retry Mode is not supported.");
        }
        #endregion Helper Methods
    }
}
