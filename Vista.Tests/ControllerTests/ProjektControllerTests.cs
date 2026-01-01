using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vista.Core.Controllers;
using Vista.Core.DTOs.Common;
using Vista.Core.DTOs.Projekt;
using Vista.Core.Models;
using Vista.Tests.Helpers;

namespace Vista.Tests.ControllerTests;

public class ProjektControllerTests
{
    private readonly Guid _mandantId = Guid.NewGuid();

    private ProjektController CreateController(out Vista.Core.Data.AppDbContext db)
    {
        db = TestDbHelper.CreateContext(_mandantId);
        var logger = TestDbHelper.CreateLogger<ProjektController>();
        var controller = new ProjektController(db, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = TestDbHelper.CreateHttpContext(_mandantId)
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PagedResult<ProjektResponseDto>>(ok.Value);
        Assert.Empty(paged.Items);
    }

    [Fact]
    public async Task GetAll_ReturnsProjekte_WhenExist()
    {
        var controller = CreateController(out var db);

        db.Projekte.Add(new Projekt
        {
            MandantId = _mandantId,
            Name = "TestProjekt",
            Beschreibung = "Test",
            Status = "NichtGestartet",
            Prioritaet = "Mittel"
        });
        await db.SaveChangesAsync();

        var result = await controller.GetAll();
        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PagedResult<ProjektResponseDto>>(ok.Value);
        Assert.Single(paged.Items);
        Assert.Equal("TestProjekt", paged.Items.First().Name);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenInvalidId()
    {
        var controller = CreateController(out _);
        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithSearch_FiltersResults()
    {
        var controller = CreateController(out var db);

        db.Projekte.AddRange(
            new Projekt { MandantId = _mandantId, Name = "Frontend", Beschreibung = "React", Status = "InBearbeitung", Prioritaet = "Hoch" },
            new Projekt { MandantId = _mandantId, Name = "Backend", Beschreibung = "API", Status = "InBearbeitung", Prioritaet = "Hoch" }
        );
        await db.SaveChangesAsync();

        var result = await controller.GetAll(search: "Frontend");
        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PagedResult<ProjektResponseDto>>(ok.Value);
        Assert.Single(paged.Items);
    }
}
