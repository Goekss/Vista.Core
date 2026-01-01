using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Vista.Core.Controllers;
using Vista.Core.DTOs.Common;
using Vista.Core.DTOs.Kunde;
using Vista.Core.Models;
using Vista.Core.Services;
using Vista.Tests.Helpers;

namespace Vista.Tests.ControllerTests;

public class KundeControllerTests
{
    private readonly Guid _mandantId = Guid.NewGuid();

    private KundeController CreateController()
    {
        var db = TestDbHelper.CreateContext(_mandantId);
        var logger = TestDbHelper.CreateLogger<KundeController>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
        var mockStorageLogger = new Mock<ILogger<FileStorageService>>();
        var fileStorage = new Mock<FileStorageService>(MockBehavior.Loose, mockEnv.Object, mockStorageLogger.Object).Object;
        var controller = new KundeController(db, logger, fileStorage)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = TestDbHelper.CreateHttpContext(_mandantId)
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoKunden()
    {
        var controller = CreateController();
        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PagedResult<KundeResponseDto>>(ok.Value);
        Assert.Empty(paged.Items);
        Assert.Equal(0, paged.TotalCount);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithValidData()
    {
        var controller = CreateController();
        var dto = new KundeRequestDto
        {
            Unternehmen = "TestFirma GmbH",
            Vorname = "Max",
            Nachname = "Müller",
            Email = "max@testfirma.de"
        };

        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task Create_ThenGetById_ReturnsKunde()
    {
        // Aynı DB context paylaşmak için manuel oluşturma
        var db = TestDbHelper.CreateContext(_mandantId);
        var logger = TestDbHelper.CreateLogger<KundeController>();
        var httpContext = TestDbHelper.CreateHttpContext(_mandantId);
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
        var mockStorageLogger = new Mock<ILogger<FileStorageService>>();
        var fileStorage = new Mock<FileStorageService>(MockBehavior.Loose, mockEnv.Object, mockStorageLogger.Object).Object;

        var controller = new KundeController(db, logger, fileStorage)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var dto = new KundeRequestDto
        {
            Unternehmen = "ABC AG",
            Vorname = "Hans",
            Nachname = "Schmidt",
            Email = "hans@abc.de"
        };

        var createResult = await controller.Create(dto) as CreatedAtActionResult;
        Assert.NotNull(createResult);

        // ID'yi al
        var idProp = createResult.Value!.GetType().GetProperty("Id");
        var id = (Guid)idProp!.GetValue(createResult.Value)!;

        var getResult = await controller.GetById(id);
        var ok = Assert.IsType<OkObjectResult>(getResult);
        var kunde = Assert.IsType<KundeResponseDto>(ok.Value);
        Assert.Equal("ABC AG", kunde.Unternehmen);
        Assert.Equal("hans@abc.de", kunde.Email);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenInvalidId()
    {
        var controller = CreateController();
        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsNoContent_WhenValid()
    {
        var db = TestDbHelper.CreateContext(_mandantId);
        var logger = TestDbHelper.CreateLogger<KundeController>();
        var httpContext = TestDbHelper.CreateHttpContext(_mandantId);
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
        var mockStorageLogger = new Mock<ILogger<FileStorageService>>();
        var fileStorage = new Mock<FileStorageService>(MockBehavior.Loose, mockEnv.Object, mockStorageLogger.Object).Object;

        var controller = new KundeController(db, logger, fileStorage)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Önce oluştur
        var kunde = new Kunde
        {
            MandantId = _mandantId,
            Unternehmen = "OldName",
            Vorname = "V",
            Nachname = "N",
            Email = "old@test.de"
        };
        db.Kunden.Add(kunde);
        await db.SaveChangesAsync();

        // Güncelle
        var updateDto = new KundeRequestDto
        {
            Unternehmen = "NewName",
            Vorname = "V",
            Nachname = "N",
            Email = "new@test.de"
        };

        var result = await controller.Update(kunde.Id, updateDto);
        Assert.IsType<NoContentResult>(result);

        // Doğrula
        var updated = await db.Kunden.FindAsync(kunde.Id);
        Assert.Equal("NewName", updated!.Unternehmen);
        Assert.Equal("new@test.de", updated.Email);
    }

    [Fact]
    public async Task Delete_SoftDeletes_Kunde()
    {
        var db = TestDbHelper.CreateContext(_mandantId);
        var logger = TestDbHelper.CreateLogger<KundeController>();
        var httpContext = TestDbHelper.CreateHttpContext(_mandantId);
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
        var mockStorageLogger = new Mock<ILogger<FileStorageService>>();
        var fileStorage = new Mock<FileStorageService>(MockBehavior.Loose, mockEnv.Object, mockStorageLogger.Object).Object;

        var controller = new KundeController(db, logger, fileStorage)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var kunde = new Kunde
        {
            MandantId = _mandantId,
            Unternehmen = "ToDelete",
            Vorname = "D",
            Nachname = "D",
            Email = "del@test.de"
        };
        db.Kunden.Add(kunde);
        await db.SaveChangesAsync();

        var result = await controller.Delete(kunde.Id);
        Assert.IsType<NoContentResult>(result);

        var deleted = await db.Kunden.FindAsync(kunde.Id);
        Assert.True(deleted!.IstGeloescht);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenInvalidId()
    {
        var controller = CreateController();
        var result = await controller.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithSearch_FiltersResults()
    {
        var db = TestDbHelper.CreateContext(_mandantId);
        var logger = TestDbHelper.CreateLogger<KundeController>();
        var httpContext = TestDbHelper.CreateHttpContext(_mandantId);
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
        var mockStorageLogger = new Mock<ILogger<FileStorageService>>();
        var fileStorage = new Mock<FileStorageService>(MockBehavior.Loose, mockEnv.Object, mockStorageLogger.Object).Object;

        var controller = new KundeController(db, logger, fileStorage)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        db.Kunden.AddRange(
            new Kunde { MandantId = _mandantId, Unternehmen = "Alpha GmbH", Vorname = "A", Nachname = "A", Email = "a@a.de" },
            new Kunde { MandantId = _mandantId, Unternehmen = "Beta AG", Vorname = "B", Nachname = "B", Email = "b@b.de" }
        );
        await db.SaveChangesAsync();

        var result = await controller.GetAll(search: "Alpha");
        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PagedResult<KundeResponseDto>>(ok.Value);
        Assert.Single(paged.Items);
        Assert.Equal("Alpha GmbH", paged.Items.First().Unternehmen);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenNoMandantId()
    {
        var db = TestDbHelper.CreateContext(null);
        var logger = TestDbHelper.CreateLogger<KundeController>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
        var mockStorageLogger = new Mock<ILogger<FileStorageService>>();
        var fileStorage = new Mock<FileStorageService>(MockBehavior.Loose, mockEnv.Object, mockStorageLogger.Object).Object;

        var controller = new KundeController(db, logger, fileStorage)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = TestDbHelper.CreateHttpContext(null)
            }
        };

        var dto = new KundeRequestDto
        {
            Unternehmen = "NoTenant",
            Vorname = "X",
            Nachname = "X",
            Email = "x@x.de"
        };

        var result = await controller.Create(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
