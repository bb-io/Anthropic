using Tests.Anthropic.Base;
using Apps.Anthropic.Connection;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Tests.Anthropic;

[TestClass]
public class ConnectionValidatorTests : TestBase
{
    [TestMethod]
    public async Task ValidatesCorrectConnection()
    {
        // Arrange
        var validator = new ConnectionValidator();
        var tasks = CredentialGroups.Select(x => validator.ValidateConnection(x, CancellationToken.None).AsTask());

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.IsTrue(results.All(x => x.IsValid));
    }

    [TestMethod]
    public async Task DoesNotValidateIncorrectConnection()
    {
        // Arrange
        var validator = new ConnectionValidator();
        var newCreds = CredentialGroups
            .First()
            .Select(x => new AuthenticationCredentialsProvider(x.KeyName, x.Value + "_incorrect"));

        // Act
        var result = await validator.ValidateConnection(newCreds, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsValid);
    }
}