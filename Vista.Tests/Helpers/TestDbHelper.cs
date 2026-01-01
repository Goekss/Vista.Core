using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vista.Core.Data;

namespace Vista.Tests.Helpers;

public static class TestDbHelper
{
    private static int _counter;

    public static AppDbContext CreateContext(Guid? mandantId = null)
    {
        var dbName = $"TestDb_{Interlocked.Increment(ref _counter)}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // Test ortamında query filter sorun çıkarmaması için mandantId header'ı context'e GEÇMİYORUZ
        // Global filter atlanır, veriler filtresiz döner
        var httpContext = new DefaultHttpContext();

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        return new AppDbContext(options, httpContextAccessor.Object);
    }

    public static ILogger<T> CreateLogger<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }

    public static HttpContext CreateHttpContext(Guid? mandantId = null)
    {
        var httpContext = new DefaultHttpContext();
        if (mandantId.HasValue)
        {
            httpContext.Request.Headers["X-Mandant-Id"] = mandantId.Value.ToString();

            var claims = new[] { new Claim("MandantId", mandantId.Value.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }
        return httpContext;
    }
}
