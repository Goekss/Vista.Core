using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vista.Core.Controllers;
using Vista.Core.DTOs.Abonnement;
using Vista.Core.Enums;
using Vista.Tests.Helpers;

namespace Vista.Tests.ControllerTests;

public class AbonnementControllerTests
{
    private readonly Guid _mandantId = Guid.NewGuid();

    private AbonnementController CreateController()
    {
        var db = TestDbHelper.CreateContext(_mandantId);
        var logger = TestDbHelper.CreateLogger<AbonnementController>();
        var controller = new AbonnementController(db, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = TestDbHelper.CreateHttpContext(_mandantId)
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetPlaene_ReturnsPlans()
    {
        var controller = CreateController();
        var result = controller.GetPlaene();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        var controller = CreateController();
        var dto = new AbonnementRequestDto
        {
            Plan = AbonnementPlan.Basis,
            PlanName = "Basis",
            Preis = 29.99m,
            StartDatum = DateTime.UtcNow
        };

        var result = await controller.Create(dto);
        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenInvalidId()
    {
        var controller = CreateController();
        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }
}
