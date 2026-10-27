using System;
using System.IO;
using Xunit;

namespace InterchangeBuilder.LaneConnections.Tests;

public sealed class CurveKernelTests
{
    [Fact]
    public void CurveContractAndCrossLanguageVectors()
    {
        var checks=CurveKernelContractChecks.Run(Path.Combine(AppContext.BaseDirectory,"curve-contract-vectors.json"));
        Assert.NotEmpty(checks);
    }
}
