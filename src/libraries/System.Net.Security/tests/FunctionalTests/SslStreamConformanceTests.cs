// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Tests;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    public abstract class SslStreamConformanceTests : WrappingConnectedStreamConformanceTests
    {
        protected override bool UsableAfterCanceledReads => false;
        protected override bool BlocksOnZeroByteReads => true;
        protected override Type UnsupportedConcurrentExceptionType => typeof(NotSupportedException);

        protected virtual SslProtocols GetSslProtocols() => SslProtocols.None;

        protected override async Task<StreamPair> CreateWrappedConnectedStreamsAsync(StreamPair wrapped, bool leaveOpen = false)
        {
            X509Certificate2? cert = Test.Common.Configuration.Certificates.GetServerCertificate();
            var ssl1 = new SslStream(wrapped.Stream1, leaveOpen, delegate { return true; });
            var ssl2 = new SslStream(wrapped.Stream2, leaveOpen, delegate { return true; });

            await new[]
            {
                ssl1.AuthenticateAsClientAsync(cert.GetNameInfo(X509NameType.SimpleName, false), null, GetSslProtocols(), false),
                ssl2.AuthenticateAsServerAsync(cert, false, GetSslProtocols(), false)
            }.WhenAllOrAnyFailed().ConfigureAwait(false);

            return new StreamPair(ssl1, ssl2);
        }
    }

    public sealed class SslStreamMemoryConformanceTests : SslStreamConformanceTests
    {
        protected override Task<StreamPair> CreateConnectedStreamsAsync() =>
            CreateWrappedConnectedStreamsAsync(ConnectedStreams.CreateBidirectional());
    }

    public abstract class SslStreamDefaultNetworkConformanceTests : SslStreamConformanceTests
    {
        protected override bool CanTimeout => true;

        protected override Task<StreamPair> CreateConnectedStreamsAsync() =>
            CreateWrappedConnectedStreamsAsync(TestHelper.GetConnectedTcpStreams());
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls11))]
    public sealed class SslStreamTls11NetworkConformanceTests : SslStreamDefaultNetworkConformanceTests
    {
        protected override SslProtocols GetSslProtocols() => SslProtocols.Tls11;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls12))]
    public sealed class SslStreamTls12NetworkConformanceTests : SslStreamDefaultNetworkConformanceTests
    {
        protected override SslProtocols GetSslProtocols() => SslProtocols.Tls12;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls13))]
    public sealed class SslStreamTls13NetworkConformanceTests : SslStreamDefaultNetworkConformanceTests
    {
        protected override SslProtocols GetSslProtocols() => SslProtocols.Tls13;
    }
}
