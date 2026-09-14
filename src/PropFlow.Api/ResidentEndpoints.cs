using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PropFlow.Application;
using PropFlow.Domain.Communications;
using PropFlow.Domain.People;
using PropFlow.Infrastructure.Persistence;
using PropFlow.Infrastructure.Identity;

namespace PropFlow.Api;

public static class ResidentEndpoints
{
    public static void MapResidentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/residents").RequireAuthorization(Capabilities.ReadWork);

        group.MapGet("/directory", async (HttpRequest request, ClaimsPrincipal user, OperationsStore store, CancellationToken ct) =>
        {
            var query = request.Query["q"].ToString().Trim();
            var propertyId = ParseGuid(request.Query["propertyId"]);
            var buildingId = ParseGuid(request.Query["buildingId"]);
            var floor = request.Query["floor"].ToString().Trim();
            var room = request.Query["room"].ToString().Trim();
            var status = request.Query["status"].ToString().Trim();
            var moveInFrom = ParseDate(request.Query["moveInFrom"]);
            var moveInTo = ParseDate(request.Query["moveInTo"]);
            var noticeFrom = ParseDate(request.Query["noticeFrom"]);
            var noticeTo = ParseDate(request.Query["noticeTo"]);
            var leaseExpiresFrom = ParseDate(request.Query["leaseExpiresFrom"]);
            var leaseExpiresTo = ParseDate(request.Query["leaseExpiresTo"]);

            var residents = await store.Residents.AsNoTracking()
                .Where(x => query == "" || x.FullName.ToLower().Contains(query.ToLower()) ||
                    (x.Email != null && x.Email.ToLower().Contains(query.ToLower())) ||
                    (x.Phone != null && x.Phone.Contains(query)))
                .Where(x => propertyId == null || store.Occupancies.Any(o => o.ResidentId == x.Id && store.Spaces.Any(s => s.Id == o.SpaceId && s.PropertyId == propertyId)))
                .Where(x => buildingId == null || store.Occupancies.Any(o => o.ResidentId == x.Id && store.Spaces.Any(s => s.Id == o.SpaceId && s.BuildingId == buildingId)))
                .Where(x => floor == "" || store.Occupancies.Any(o => o.ResidentId == x.Id && store.Spaces.Any(s => s.Id == o.SpaceId && s.Code.StartsWith(floor))))
                .Where(x => room == "" || store.Occupancies.Any(o => o.ResidentId == x.Id && store.Spaces.Any(s => s.Id == o.SpaceId && s.Code.EndsWith(room))))
                .OrderBy(x => x.FullName).ThenBy(x => x.Id).Take(500).ToListAsync(ct);

            var ids = residents.Select(x => x.Id).ToArray();
            var occupancies = await store.Occupancies.AsNoTracking().Where(x => ids.Contains(x.ResidentId)).ToListAsync(ct);
            var spaceIds = occupancies.Select(x => x.SpaceId).Distinct().ToArray();
            var spaces = await store.Spaces.AsNoTracking().Where(x => spaceIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var propertyIds = spaces.Values.Select(x => x.PropertyId).Distinct().ToArray();
            var properties = await store.Properties.AsNoTracking().Where(x => propertyIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var leases = await store.Leases.AsNoTracking().Where(x => ids.Contains(x.ResidentId)).ToListAsync(ct);

            var rows = residents.Select(resident =>
            {
                var occupancy = occupancies.Where(x => x.ResidentId == resident.Id).OrderByDescending(x => x.MovedOutOn == null).ThenByDescending(x => x.MovedInOn).FirstOrDefault();
                var lease = leases.Where(x => x.ResidentId == resident.Id).OrderByDescending(x => x.Status != PropFlow.Domain.Leasing.LeaseStatus.Ended).ThenByDescending(x => x.EndsOn).FirstOrDefault();
                spaces.TryGetValue(occupancy?.SpaceId ?? Guid.Empty, out var space);
                properties.TryGetValue(space?.PropertyId ?? Guid.Empty, out var property);
                return new ResidentDirectoryRow(resident.Id, resident.FullName,
                    Mask(resident.Email, user), Mask(resident.Phone, user),
                    property?.Id, property?.Name, space?.BuildingId, space?.Code,
                    occupancy?.MovedInOn, occupancy?.MovedOutOn, lease?.Status.ToString(), lease?.NoticeDate, lease?.EndsOn);
            }).Where(x => Match(x, status, moveInFrom, moveInTo, noticeFrom, noticeTo, leaseExpiresFrom, leaseExpiresTo)).ToArray();
            return Results.Ok(rows);
        });

        group.MapGet("/directory/{id:guid}", async (Guid id, ClaimsPrincipal user, OperationsStore store, ITenantContext tenant, IdentityAuditLog audit, IdentityStore identity, CancellationToken ct) =>
        {
            var resident = await store.Residents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (resident is null) return Results.NotFound();
            var occupancies = await store.Occupancies.AsNoTracking().Where(x => x.ResidentId == id).OrderByDescending(x => x.MovedInOn).ToListAsync(ct);
            var leases = await store.Leases.AsNoTracking().Where(x => x.ResidentId == id).OrderByDescending(x => x.StartsOn).ToListAsync(ct);
            var household = await store.HouseholdMembers.AsNoTracking().Where(x => x.ResidentId == id).OrderBy(x => x.FullName).ToListAsync(ct);
            var work = await store.WorkItems.AsNoTracking().Where(x => x.ResidentId == id).OrderByDescending(x => x.CreatedAt).Take(50)
                .Select(x => new ResidentWorkLink(x.Id, x.Title, x.Status.ToString(), x.Priority.ToString(), x.CreatedAt)).ToListAsync(ct);
            var spaceIds = occupancies.Select(x => x.SpaceId).Concat(leases.Select(x => x.SpaceId)).Distinct().ToArray();
            var spaces = await store.Spaces.AsNoTracking().Where(x => spaceIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var propertyIds = spaces.Values.Select(x => x.PropertyId).Distinct().ToArray();
            var properties = await store.Properties.AsNoTracking().Where(x => propertyIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            audit.Record(tenant.OrganizationId, IdentityAuditLog.EventTypes.SensitiveResidentProfileViewed,
                Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : null,
                null, $"Resident:{resident.Id:N}:{resident.FullName}", null, "Resident profile and contact projection viewed");
            await identity.SaveChangesAsync(ct);
            return Results.Ok(new ResidentProfile(
                new ResidentProfileSummary(resident.Id, resident.FullName, Mask(resident.Email, user), Mask(resident.Phone, user), resident.SmsConsent.ToString(), resident.EmailConsent.ToString()),
                occupancies.Select(x => new OccupancySummary(x.Id, x.SpaceId, spaces.GetValueOrDefault(x.SpaceId)?.Code, properties.GetValueOrDefault(spaces.GetValueOrDefault(x.SpaceId)?.PropertyId ?? Guid.Empty)?.Name, x.MovedInOn, x.MovedOutOn)),
                leases.Select(x => new LeaseSummary(x.Id, x.SpaceId, spaces.GetValueOrDefault(x.SpaceId)?.Code, x.Status.ToString(), x.StartsOn, x.EndsOn, x.NoticeDate, x.MoveOutOn, x.MonthlyRent)),
                household.Select(x => new HouseholdSummary(x.Id, x.FullName, x.Relationship, Mask(x.Email, user))), work));
        });

        group.MapGet("/", async (OperationsStore store, CancellationToken ct) =>
            Results.Ok(await store.Residents.AsNoTracking()
                .OrderBy(x => x.FullName).ThenBy(x => x.Id).Take(200).ToListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, OperationsStore store, CancellationToken ct) =>
            await store.Residents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) is { } resident
                ? Results.Ok(resident) : Results.NotFound());

        group.MapGet("/{id:guid}/occupancies", async (Guid id, OperationsStore store, CancellationToken ct) =>
            await store.Residents.AnyAsync(x => x.Id == id, ct)
                ? Results.Ok(await store.Occupancies.AsNoTracking().Where(x => x.ResidentId == id)
                    .OrderByDescending(x => x.MovedInOn).ThenBy(x => x.Id).ToListAsync(ct))
                : Results.NotFound());

        group.MapPost("/", async (ResidentRequest request, OperationsStore store, ITenantContext tenant, CancellationToken ct) =>
        {
            Resident resident;
            try
            {
                resident = new Resident(tenant.OrganizationId, Guid.NewGuid(), request.FullName, request.Email, request.Phone);
            }
            catch (ArgumentException exception)
            {
                return Results.Problem(statusCode: 400, title: exception.Message);
            }

            store.Residents.Add(resident);
            await store.SaveChangesAsync(ct);
            return Results.Created($"/api/residents/{resident.Id}", resident);
        }).RequireAuthorization(Capabilities.ManagePeople);

        group.MapPut("/{id:guid}", async (Guid id, ResidentRequest request, OperationsStore store, CancellationToken ct) =>
        {
            var resident = await store.Residents.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (resident is null) return Results.NotFound();

            try
            {
                resident.Rename(request.FullName);
                resident.UpdateContact(request.Email, request.Phone);
            }
            catch (ArgumentException exception)
            {
                return Results.Problem(statusCode: 400, title: exception.Message);
            }

            await store.SaveChangesAsync(ct);
            return Results.Ok(resident);
        }).RequireAuthorization(Capabilities.ManagePeople);

        group.MapPut("/{id:guid}/consent", async (Guid id, ConsentRequest request, OperationsStore store,
            TimeProvider clock, CancellationToken ct) =>
        {
            var resident = await store.Residents.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (resident is null) return Results.NotFound();

            try
            {
                resident.SetConsent(request.Channel, request.Granted, clock.GetUtcNow());
            }
            catch (ArgumentException exception)
            {
                return Results.Problem(statusCode: 400, title: exception.Message);
            }

            await store.SaveChangesAsync(ct);
            return Results.Ok(resident);
        }).RequireAuthorization(Capabilities.ManagePeople);

        group.MapPost("/{id:guid}/occupancies", async (Guid id, OccupancyRequest request, OperationsStore store, ITenantContext tenant, CancellationToken ct) =>
        {
            if (!await store.Residents.AnyAsync(x => x.Id == id, ct)) return Results.NotFound();

            Occupancy occupancy;
            try
            {
                occupancy = new Occupancy(tenant.OrganizationId, Guid.NewGuid(), id, request.SpaceId, request.MovedInOn);
            }
            catch (ArgumentException exception)
            {
                return Results.Problem(statusCode: 400, title: exception.Message);
            }

            store.Occupancies.Add(occupancy);
            try
            {
                await store.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // The space belongs to another organization or does not exist.
                return Results.Problem(statusCode: 400, title: "Unknown space");
            }
            return Results.Created($"/api/residents/{id}/occupancies/{occupancy.Id}", occupancy);
        }).RequireAuthorization(Capabilities.ManagePeople);

        group.MapPut("/{id:guid}/occupancies/{occupancyId:guid}/end", async (Guid id, Guid occupancyId,
            EndOccupancyRequest request, OperationsStore store, CancellationToken ct) =>
        {
            var occupancy = await store.Occupancies.SingleOrDefaultAsync(x => x.Id == occupancyId && x.ResidentId == id, ct);
            if (occupancy is null) return Results.NotFound();

            try
            {
                occupancy.EndOn(request.MovedOutOn);
            }
            catch (ArgumentException exception)
            {
                return Results.Problem(statusCode: 400, title: exception.Message);
            }

            await store.SaveChangesAsync(ct);
            return Results.Ok(occupancy);
        }).RequireAuthorization(Capabilities.ManagePeople);
    }

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
    private static DateOnly? ParseDate(string? value) => DateOnly.TryParse(value, out var date) ? date : null;
    private static string? Mask(string? value, ClaimsPrincipal user) =>
        user.HasClaim(TenantAccess.CapabilityClaim, Capabilities.ManagePeople) || string.IsNullOrWhiteSpace(value)
            ? value : value.Length <= 4 ? "••••" : $"••••{value[^Math.Min(4, value.Length)..]}";
    private static bool Match(ResidentDirectoryRow row, string status, DateOnly? moveInFrom, DateOnly? moveInTo, DateOnly? noticeFrom, DateOnly? noticeTo, DateOnly? leaseExpiresFrom, DateOnly? leaseExpiresTo) =>
        (status == "" || string.Equals(row.LeaseStatus, status, StringComparison.OrdinalIgnoreCase)) &&
        (!moveInFrom.HasValue || row.MoveInOn >= moveInFrom) && (!moveInTo.HasValue || row.MoveInOn <= moveInTo) &&
        (!noticeFrom.HasValue || row.NoticeDate >= noticeFrom) && (!noticeTo.HasValue || row.NoticeDate <= noticeTo) &&
        (!leaseExpiresFrom.HasValue || row.LeaseExpiresOn >= leaseExpiresFrom) && (!leaseExpiresTo.HasValue || row.LeaseExpiresOn <= leaseExpiresTo);
}

// Tenant comes from the verified session.
public sealed record ResidentRequest(string FullName, string? Email, string? Phone);
public sealed record ConsentRequest(MessageChannel Channel, bool Granted);
public sealed record OccupancyRequest(Guid SpaceId, DateOnly MovedInOn);
public sealed record EndOccupancyRequest(DateOnly MovedOutOn);
public sealed record ResidentDirectoryRow(Guid Id, string FullName, string? Email, string? Phone, Guid? PropertyId, string? PropertyName, Guid? BuildingId, string? SpaceCode, DateOnly? MoveInOn, DateOnly? MoveOutOn, string? LeaseStatus, DateOnly? NoticeDate, DateOnly? LeaseExpiresOn);
public sealed record ResidentProfile(ResidentProfileSummary Resident, IEnumerable<OccupancySummary> Occupancies, IEnumerable<LeaseSummary> Leases, IEnumerable<HouseholdSummary> Household, IEnumerable<ResidentWorkLink> Work);
public sealed record ResidentProfileSummary(Guid Id, string FullName, string? Email, string? Phone, string SmsConsent, string EmailConsent);
public sealed record OccupancySummary(Guid Id, Guid SpaceId, string? SpaceCode, string? PropertyName, DateOnly MovedInOn, DateOnly? MovedOutOn);
public sealed record LeaseSummary(Guid Id, Guid SpaceId, string? SpaceCode, string Status, DateOnly StartsOn, DateOnly EndsOn, DateOnly? NoticeDate, DateOnly? MoveOutOn, decimal MonthlyRent);
public sealed record HouseholdSummary(Guid Id, string FullName, string Relationship, string? Email);
public sealed record ResidentWorkLink(Guid Id, string Title, string Status, string Priority, DateTimeOffset CreatedAt);
