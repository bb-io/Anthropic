using Tests.Anthropic.Base;
using Apps.Anthropic.Connection;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Tests.Anthropic;

[TestClass]
public class ConnectionValidatorTests : TestBaseMultipleConnections
{
    [TestMethod, ContextDataSource]
    public async Task ValidatesCorrectConnection(InvocationContext context)
    {
        // Arrange
        var validator = new ConnectionValidator();
        var tasks = CredentialGroups.Select(x => validator.ValidateConnection(x, CancellationToken.None).AsTask());

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.IsTrue(results.All(x => x.IsValid));
    }

    [TestMethod, ContextDataSource]
    public async Task DoesNotValidateIncorrectConnection(InvocationContext context)
    {
        var validator = new ConnectionValidator();

        var newCreds = Creds.Select(x => new AuthenticationCredentialsProvider(x.KeyName, x.Value + "_incorrect"));
        var result = await validator.ValidateConnection(newCreds, CancellationToken.None);
        Assert.IsFalse(result.IsValid);
    }
}