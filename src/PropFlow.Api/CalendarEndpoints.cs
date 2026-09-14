using Microsoft.EntityFrameworkCore;
using PropFlow.Application;
using PropFlow.Infrastructure.Persistence;

namespace PropFlow.Api;

public static class CalendarEndpoints
{
    public static void MapCalendarEndpoints(this WebApplication app)
    {
        app.MapGet("/api/calendar", async (DateOnly from, DateOnly to, Guid? propertyId, Guid? portfolioId, OperationsStore store, CancellationToken ct) =>
        {
            if (to < from || to.DayNumber - from.DayNumber > 366)
                return Results.Problem(statusCode: 400, title: "Calendar range must be 0 to 366 days.");

            var properties = await store.Properties.AsNoTracking()
                .Where(x => !x.IsArchived && (propertyId == null || x.Id == propertyId) && (portfolioId == null || x.PortfolioId == portfolioId))
                .ToDictionaryAsync(x => x.Id, ct);
            var spaces = await store.Spaces.AsNoTracking().Where(x => properties.Keys.Contains(x.PropertyId)).ToDictionaryAsync(x => x.Id, ct);
            var residentNames = await store.Residents.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
            var events = new List<CalendarEvent>();

            static bool InRange(DateOnly date, DateOnly from, DateOnly to) => date >= from && date <= to;
            void AddDate(string type, string title, DateOnly date, Guid sourceId, Guid pId, Guid? residentId, string href)
            {
                if (properties.TryGetValue(pId, out var property) && InRange(date, from, to))
                    events.Add(new CalendarEvent(Guid.NewGuid(), type, title, date, null, sourceId, pId, property.Name, property.TimeZoneId,
                        residentId, residentId is { } rid ? residentNames.GetValueOrDefault(rid) : null, href));
            }

            foreach (var occupancy in await store.Occupancies.AsNoTracking().Where(x => spaces.Keys.Contains(x.SpaceId)).ToListAsync(ct))
            {
                var propertyIdForSpace = spaces[occupancy.SpaceId].PropertyId;
                AddDate("MoveIn", "Move-in", occupancy.MovedInOn, occupancy.Id, propertyIdForSpace, occupancy.ResidentId, $"/residents?residentId={occupancy.ResidentId}");
                if (occupancy.MovedOutOn is { } movedOut)
                    AddDate("MoveOut", "Move-out", movedOut, occupancy.Id, propertyIdForSpace, occupancy.ResidentId, $"/residents?residentId={occupancy.ResidentId}");
            }

            foreach (var lease in await store.Leases.AsNoTracking().Where(x => spaces.Keys.Contains(x.SpaceId)).ToListAsync(ct))
            {
                var pId = spaces[lease.SpaceId].PropertyId;
                AddDate("LeaseExpiration", "Lease expiration", lease.EndsOn, lease.Id, pId, lease.ResidentId, $"/leasing/leases?leaseId={lease.Id}");
                if (lease.NoticeDate is { } notice)
                    AddDate("Notice", "Lease notice", notice, lease.Id, pId, lease.ResidentId, $"/leasing/leases?leaseId={lease.Id}");
                if (lease.MoveOutOn is { } movedOut)
                    AddDate("MoveOut", "Scheduled move-out", movedOut, lease.Id, pId, lease.ResidentId, $"/leasing/leases?leaseId={lease.Id}");
            }

            foreach (var inspection in await store.Inspections.AsNoTracking().Where(x => properties.Keys.Contains(x.PropertyId)).ToListAsync(ct))
                AddDate("Inspection", $"{inspection.Kind} inspection", DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(inspection.CreatedAt, Zone(properties[inspection.PropertyId].TimeZoneId)).DateTime), inspection.Id, inspection.PropertyId, null, $"/inspections/{inspection.Id}");
            foreach (var turn in await store.UnitTurns.AsNoTracking().Where(x => properties.Keys.Contains(x.PropertyId)).ToListAsync(ct))
                AddDate("UnitTurn", "Unit-turn target", turn.TargetReadyOn, turn.Id, turn.PropertyId, null, $"/work?unitTurnId={turn.Id}");

            foreach (var work in await store.WorkItems.AsNoTracking().Where(x => properties.Keys.Contains(x.PropertyId) && (x.DueDate != null || x.ScheduledStart != null)).ToListAsync(ct))
            {
                var property = properties[work.PropertyId];
                var instant = work.DueDate ?? work.ScheduledStart!.Value;
                var local = TimeZoneInfo.ConvertTime(instant, Zone(property.TimeZoneId));
                if (InRange(DateOnly.FromDateTime(local.DateTime), from, to))
                    events.Add(new CalendarEvent(Guid.NewGuid(), "Work", work.Title, DateOnly.FromDateTime(local.DateTime), local, work.Id, work.PropertyId, property.Name, property.TimeZoneId,
                        work.ResidentId, work.ResidentId is { } rid ? residentNames.GetValueOrDefault(rid) : null, $"/work/{work.Id}"));
            }

            return Results.Ok(events.OrderBy(x => x.Date).ThenBy(x => x.StartsAt).ThenBy(x => x.Title));
        }).RequireAuthorization(Capabilities.ReadWork);
    }

    private static TimeZoneInfo Zone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}

public sealed record CalendarEvent(Guid Id, string Type, string Title, DateOnly Date, DateTimeOffset? StartsAt, Guid SourceId, Guid PropertyId,
    string PropertyName, string TimeZoneId, Guid? ResidentId, string? ResidentName, string Href);
