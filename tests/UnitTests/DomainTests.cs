using Xunit;

namespace CryptoTrading.UnitTests;

public class DomainTests
{
    [Fact]
    public void Foundation_ShouldBeReadyAndCompilable()
    {
        // Arrange & Act
        bool isReady = true;

        // Assert
        Assert.True(isReady, "A base do projeto deve estar configurada e pronta para desenvolvimento.");
    }
}
