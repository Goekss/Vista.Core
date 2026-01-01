using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Vista.Core.Controllers;
using Vista.Core.DTOs.Common;
using Vista.Core.DTOs.Ticket;
using Vista.Core.Hubs;
using Vista.Core.Models;
using Vista.Core.Services;
using Vista.Tests.Helpers;

namespace Vista.Tests.ControllerTests;

public class TicketControllerTests
{
    private readonly Guid _mandantId = Guid.NewGuid();

    private TicketController CreateController()
    {
        var db = TestDbHelper.CreateContext(_mandantId);
        var logger = TestDbHelper.CreateLogger<TicketController>();
        var emailService = new Mock<EmailService>(null!, null!).Object;
        var hubContext = new Mock<IHubContext<BenachrichtigungHub>>().Object;

        var controller = new TicketController(db, emailService, hubContext, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = TestDbHelper.CreateHttpContext(_mandantId)
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoTickets()
    {
        var controller = CreateController();
        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PagedResult<TicketResponseDto>>(ok.Value);
        Assert.Empty(paged.Items);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenInvalidId()
    {
        var controller = CreateController();
        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
