using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Todo.Api.Security.Jwt;

namespace Todo.Api.UnitTests.Security.Jwt;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void Generate_Should_Create_Token_With_Expected_Claims_And_Expiry()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "Todo.Api",
            Audience = "Todo.Api.Client",
            SigningKey = "development-signing-key-change-me-1234567890",
            AccessTokenLifetimeMinutes = 60
        });
        var generator = new JwtTokenGenerator(options);

        var accessToken = generator.Generate(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "user@example.com",
            "sampleUser");

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);

        token.Issuer.Should().Be("Todo.Api");
        token.Audiences.Should().Contain("Todo.Api.Client");
        token.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Sub && x.Value == "11111111-1111-1111-1111-111111111111");
        token.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Email && x.Value == "user@example.com");
        token.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.UniqueName && x.Value == "sampleUser");
        token.ValidTo.Should().BeAfter(DateTime.UtcNow.AddMinutes(59));
    }
}
