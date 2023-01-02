using System;
using PactNet;
using PactNet.Mocks.MockHttpService;

namespace QuizClient.Tests;

public class QuizServiceApiPact : IDisposable
{
    public IPactBuilder PactBuilder { get; }
    public IMockProviderService MockProviderService { get; }

    public int MockServerPort => 9222;
    public Uri MockProviderServiceBaseUri => new UriBuilder(Uri.UriSchemeHttp, "localhost", MockServerPort).Uri;

    public QuizServiceApiPact()
    {
        PactBuilder = new PactBuilder(new PactConfig { SpecificationVersion = "2.0.0", PactDir = @"c:\temp\pacts" });

        PactBuilder
            .ServiceConsumer("QuizClient")
            .HasPactWith("QuizService");

        MockProviderService = PactBuilder.MockService(MockServerPort);
    }

    public void Dispose()
    {
        PactBuilder.Build();
    }
}