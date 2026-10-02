using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Vista.Core.Controllers;
using Vista.Core.Data;
using Vista.Core.DTOs.Common;
using Vista.Core.DTOs.Kunde;
using Vista.Core.Models;
using Vista.Core.Services;
using Vista.Tests.Helpers;
using Xunit;

namespace Vista.Tests.SecurityTests;

public class TenantSecurityAndAuthTests
{
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    private KundeController CreateKundeController(AppDbContext db, Guid mandantId)
    {
        var logger = TestDbHelper.CreateLogger<KundeController>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
        var mockStorageLogger = new Mock<ILogger<FileStorageService>>();
        var fileStorage = new Mock<FileStorageService>(MockBehavior.Loose, mockEnv.Object, mockStorageLogger.Object).Object;

        var controller = new KundeController(db, logger, fileStorage)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = TestDbHelper.CreateHttpContext(mandantId)
            }
        };
        return controller;
    }

    // TR: Kiracı A sorgu yaptığında Kiracı B'nin müşterilerini asla göremez (Veri sızıntısı engeli).
    // DE: Mandant A darf niemals die Kundendaten von Mandant B sehen (Schutz vor Datenlecks).
    [Fact]
    public async Task Test1_TenantIsolation_GetAll_DoesNotReturnOtherTenantData()
    {
        // Arrange
        var dbName = $"MultiTenantDb_{Guid.NewGuid()}";
        
        // Seed both tenants into the shared database
        using (var seedDb = TestDbHelper.CreateContext(null, dbName))
        {
            seedDb.Kunden.Add(new Kunde { Id = Guid.NewGuid(), MandantId = _tenantA, Unternehmen = "TenantA Company" });
            seedDb.Kunden.Add(new Kunde { Id = Guid.NewGuid(), MandantId = _tenantB, Unternehmen = "TenantB Company" });
            await seedDb.SaveChangesAsync();
        }

        // Act: Tenant A queries all customers
        using var tenantADb = TestDbHelper.CreateContext(_tenantA, dbName);
        var controller = CreateKundeController(tenantADb, _tenantA);
        var result = await controller.GetAll();

        // Assert: Only Tenant A's customer should be visible
        var okResult = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PagedResult<KundeResponseDto>>(okResult.Value);
        Assert.Single(paged.Items);
        Assert.Equal("TenantA Company", paged.Items[0].Unternehmen);
        Assert.Equal(_tenantA, paged.Items[0].MandantId);
    }

    [Fact]
    public async Task Test2_TenantIsolation_GetById_ReturnsNotFound_ForOtherTenantEntity()
    {
        // Arrange
        var dbName = $"MultiTenantDb_{Guid.NewGuid()}";
        var tenantB_KundeId = Guid.NewGuid();

        // Seed Tenant B entity
        using (var seedDb = TestDbHelper.CreateContext(null, dbName))
        {
            seedDb.Kunden.Add(new Kunde { Id = tenantB_KundeId, MandantId = _tenantB, Unternehmen = "Secret B Data" });
            await seedDb.SaveChangesAsync();
        }

        // Act: Tenant A tries to directly fetch Tenant B's entity by ID
        using var tenantADb = TestDbHelper.CreateContext(_tenantA, dbName);
        var controller = CreateKundeController(tenantADb, _tenantA);
        var result = await controller.GetById(tenantB_KundeId);

        // Assert: Should return 404 NotFound
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Test3_TenantIsolation_UpdateAndDelete_Blocked_ForOtherTenantEntity()
    {
        // Arrange
        var dbName = $"MultiTenantDb_{Guid.NewGuid()}";
        var tenantB_KundeId = Guid.NewGuid();

        using (var seedDb = TestDbHelper.CreateContext(null, dbName))
        {
            seedDb.Kunden.Add(new Kunde { Id = tenantB_KundeId, MandantId = _tenantB, Unternehmen = "Original B Name" });
            await seedDb.SaveChangesAsync();
        }

        // Act 1: Tenant A tries to update Tenant B entity
        using var tenantADb = TestDbHelper.CreateContext(_tenantA, dbName);
        var controller = CreateKundeController(tenantADb, _tenantA);
        var updateResult = await controller.Update(tenantB_KundeId, new KundeRequestDto
        {
            Unternehmen = "Hacked B Name"
        });

        // Assert 1: Update rejected with 404 NotFound
        Assert.IsType<NotFoundObjectResult>(updateResult);

        // Act 2: Tenant A tries to delete Tenant B entity
        var deleteResult = await controller.Delete(tenantB_KundeId);

        // Assert 2: Delete rejected with 404 NotFound
        Assert.IsType<NotFoundObjectResult>(deleteResult);

        // Verify entity in DB is completely untouched
        using var verifyDb = TestDbHelper.CreateContext(_tenantB, dbName);
        var entityB = await verifyDb.Kunden.FindAsync(tenantB_KundeId);
        Assert.NotNull(entityB);
        Assert.Equal("Original B Name", entityB.Unternehmen);
        Assert.False(entityB.IstGeloescht);
    }

    // TR: MandantId claim'i olmayan veya anonim isteklerde sistem hiçbir kiracının verisini sızdırmaz (0 kayıt döner).
    // DE: Bei anonymen Anfragen ohne MandantId-Claim werden null Datensätze zurückgegeben (Sicherheitsgarantie).
    [Fact]
    public async Task Test4_TenantIsolation_AnonymousOrMissingClaim_ReturnsZeroRecords()
    {
        // Arrange
        var dbName = $"MultiTenantDb_{Guid.NewGuid()}";

        using (var seedDb = TestDbHelper.CreateContext(null, dbName))
        {
            seedDb.Kunden.Add(new Kunde { Id = Guid.NewGuid(), MandantId = _tenantA, Unternehmen = "Company A" });
            seedDb.Kunden.Add(new Kunde { Id = Guid.NewGuid(), MandantId = _tenantB, Unternehmen = "Company B" });
            await seedDb.SaveChangesAsync();
        }

        // Act: Context created without any MandantId (Anonymous / Unauthenticated)
        using var anonDb = TestDbHelper.CreateContext(null, dbName);
        var visibleKunden = await anonDb.Kunden.ToListAsync();

        // Assert: Strict isolation guarantees 0 records are returned
        Assert.Empty(visibleKunden);
    }

    [Fact]
    public async Task Test5_Auth_2FA_Verification_RejectsInvalidCode_AndSucceedsWithValidCode()
    {
        // Arrange
        var mockCache = new Mock<IDistributedCache>();
        var testEmail = "security@vista.local";
        var cacheKey = $"2fa:{testEmail}";
        var validCodeBytes = System.Text.Encoding.UTF8.GetBytes("654321");

        mockCache.Setup(c => c.GetAsync(cacheKey, default))
            .ReturnsAsync(validCodeBytes);

        var service = new ZweiFaktorService(mockCache.Object);

        // Act & Assert 1: Invalid code should fail
        var isInvalidValid = await service.CodeVerifizierenAsync(testEmail, "000000");
        Assert.False(isInvalidValid);

        // Act & Assert 2: Correct code should succeed
        var isValidSuccess = await service.CodeVerifizierenAsync(testEmail, "654321");
        Assert.True(isValidSuccess);

        // Assert cache removal was called on successful verification
        mockCache.Verify(c => c.RemoveAsync(cacheKey, default), Times.Once);
    }

    [Fact]
    public void Test6_Authorization_RoleAttributes_EnforceAdminRestrictions()
    {
        // Arrange: Check that administrative controllers declare strict role-based access
        var benutzerControllerType = typeof(BenutzerController);
        var vikaAdminControllerType = typeof(VikaAdminController);

        var benutzerAuthorize = benutzerControllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .FirstOrDefault();

        var vikaAdminAuthorize = vikaAdminControllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .FirstOrDefault();

        // Assert: Sensitive controllers require Admin/SuperAdmin roles
        Assert.NotNull(benutzerAuthorize);
        Assert.Contains("Admin", benutzerAuthorize.Roles);
        Assert.DoesNotContain("NurLesen", benutzerAuthorize.Roles);

        Assert.NotNull(vikaAdminAuthorize);
        Assert.Contains("SuperAdmin", vikaAdminAuthorize.Roles);
        Assert.DoesNotContain("NurLesen", vikaAdminAuthorize.Roles);
    }
}
