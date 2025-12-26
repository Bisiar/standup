using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Standup.Api;

public static class StandupEndpoints
{
    public static RouteGroupBuilder MapStandupEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/generate", async (
            MediatR.IMediator mediator,
            Standup.Application.Features.GenerateStandup.GenerateStandupCommand command) =>
        {
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapGet("/repositories", async (
            MediatR.IMediator mediator,
            string userId) =>
        {
            var query = new Standup.Application.Features.ConfigureRepository.ListRepositoriesQuery(userId);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        });

        group.MapPost("/repositories", async (
            MediatR.IMediator mediator,
            Standup.Application.Features.ConfigureRepository.AddRepositoryCommand command) =>
        {
            var result = await mediator.Send(command);
            return Results.Created($"/api/standup/repositories/{result.Id}", result);
        });

        return group;
    }
}
