using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notificator.DataSources;
using Notificator.Models;

namespace Notificator.Controllers;

[ApiController]
[Route("api/apartments")]
public class ApartmentController(NotificatorDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.Apartments
            .AsNoTracking()
            .Where(u => !u.Deleted)
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:long}/schedules")]
    public async Task<IActionResult> SchedulesListAsync(long id, CancellationToken cancellationToken)
    {
        if (!await dbContext.Apartments.AnyAsync(u => u.Id == id && !u.Deleted, cancellationToken))
        {
            return NotFound();
        }

        var items = await dbContext.Schedules
            .AsNoTracking()
            .OrderBy(u => u.Day)
            .Where(u => u.ApartmentId == id)
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost("{id:long}/schedules")]
    public async Task<IActionResult> SchedulesAddAsync(long id, [FromBody]ScheduleAddRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Apartments.AnyAsync(u => u.Id == id && !u.Deleted, cancellationToken))
        {
            return NotFound();
        }

        if(request.Day is null || request.Description is null)
        {
            return BadRequest();
        }

        var schedule = new Schedule
        {
            Description = request.Description,
            Day = (DayOfWeek)request.Day,
            ApartmentId = id
        };

        dbContext.Schedules.Add(schedule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/apartments/5550802597/schedules/{schedule.Id}", schedule);
    }

    [HttpGet("{id:long}/schedules/{schedulId:int}")]
    public async Task<IActionResult> SchedulesGetAsync(long id, int schedulId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Apartments.AnyAsync(u => u.Id == id && !u.Deleted, cancellationToken))
        {
            return NotFound();
        }

        var item = await dbContext.Schedules.FirstOrDefaultAsync(u => u.Id == schedulId, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpDelete("{id:long}/schedules/{schedulId:int}")]
    public async Task<IActionResult> SchedulesDeleteAsync(long id, int schedulId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Apartments.AnyAsync(u => u.Id == id && !u.Deleted, cancellationToken))
        {
            return NotFound();
        }

        var row = await dbContext.Schedules
            .Where(u => u.Id == schedulId)
            .ExecuteDeleteAsync(cancellationToken);

        if (row == 0)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPatch("{id:long}/schedules/{schedulId:int}")]
    public async Task<IActionResult> SchedulesUpdateAsync(long id, int schedulId, [FromBody]ScheduleAddRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Apartments.AnyAsync(u => u.Id == id && !u.Deleted, cancellationToken))
        {
            return NotFound();
        }

        var item = await dbContext.Schedules
            .FirstOrDefaultAsync(u => u.Id == schedulId, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        item.Description = request.Description is null ? item.Description : request.Description;
        item.Day = request.Day is null ? item.Day : (DayOfWeek)request.Day;

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:long}/users")]
    public async Task<IActionResult> UsersListAsync(long id, CancellationToken cancellationToken)
    {
        if (!await dbContext.Apartments.AnyAsync(u => u.Id == id && !u.Deleted, cancellationToken))
        {
            return NotFound();
        }

        var items = await dbContext.Subscribers
            .AsNoTracking()
            .OrderBy(u => u.Order)
            .Where(u => !u.Deleted)
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:long}/users/{userId:long}")]
    public async Task<IActionResult> UsersListAsync(long id, long userId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Apartments.AnyAsync(u => u.Id == id && !u.Deleted, cancellationToken))
        {
            return NotFound();
        }

        var item = await dbContext.Subscribers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.Deleted, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPatch("{id:long}/users/{userId:long}")]
    public async Task<IActionResult> UsersListAsync(long id, long userId, [FromBody]SubscriberUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Apartments.AnyAsync(u => u.Id == id && !u.Deleted, cancellationToken))
        {
            return NotFound();
        }

        var item = await dbContext.Subscribers
            .FirstOrDefaultAsync(u => u.Id == userId && !u.Deleted, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        item.Order = request.Order is null ? item.Order : (int)request.Order;
        item.IsActive = request.IsActive is null ? item.IsActive : (bool)request.IsActive;
        item.IsCurrent = request.IsCurrent is null ? item.IsCurrent : (bool)request.IsCurrent;
        item.Role = request.Role is null ? item.Role : request.Role;

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public record SubscriberUpdateRequest
{
    public int? Order { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsCurrent { get; set; }
    public string? Role { get; set; }
}

public record ScheduleAddRequest
{
    public string? Description { get; set; }
    public DayOfWeek? Day { get; set; }
}