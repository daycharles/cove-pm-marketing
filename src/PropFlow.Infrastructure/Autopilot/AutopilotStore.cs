using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PropFlow.Application;
using PropFlow.Domain;
using PropFlow.Domain.Autopilot;
using PropFlow.Infrastructure.Persistence;

namespace PropFlow.Infrastructure.Autopilot;

// Tenant-scoped store for the Autopilot module (CPM-8.05) — its own schema and migration
// history, same reasoning IntegrationStore gives for Integrations
// (.claude/rules/tenancy-and-rls.md's "a new module gets its own context" rule). Same
// five-layer tenant defence as every other context: composite key, central query filter,
// connection GUC, forced RLS in the migration, least-privilege grants in DatabaseProvisioner.
public sealed class AutopilotStore(DbContextOptions<AutopilotStore> options, ITenantContext tenant) : DbContext(options)
{
    public Guid OrganizationId => tenant.OrganizationId;
    public DbSet<AutopilotRun> Runs => Set<AutopilotRun>();
    public DbSet<AutopilotFinding> Findings => Set<AutopilotFinding>();
    public DbSet<AutopilotEvidence> Evidence => Set<AutopilotEvidence>();
    public DbSet<AutopilotFeedback> Feedback => Set<AutopilotFeedback>();
    public DbSet<AutopilotAuditEntry> AuditEntries => Set<AutopilotAuditEntry>();
    public DbSet<AutopilotFindingRead> FindingReads => Set<AutopilotFindingRead>();
    public DbSet<AutopilotRecommendation> Recommendations => Set<AutopilotRecommendation>();
    public DbSet<AutopilotActionProposal> ActionProposals => Set<AutopilotActionProposal>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.AddInterceptors(new TenantConnectionInterceptor(tenant));

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasDefaultSchema("autopilot");

        model.Entity<AutopilotRun>(entity =>
        {
            entity.ToTable("Runs");
            entity.Property(x => x.Trigger).HasMaxLength(AutopilotRun.TriggerMaxLength).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(x => x.FailureReason).HasMaxLength(AutopilotRun.FailureReasonMaxLength);
            // Run history, newest first, per organization — the brief and any admin screen list
            // recent runs this way.
            entity.HasIndex(x => new { x.OrganizationId, x.StartedAt }).IsDescending(false, true);
        });

        model.Entity<AutopilotFinding>(entity =>
        {
            entity.ToTable("Findings");
            entity.Property(x => x.SignalType).HasMaxLength(AutopilotFinding.SignalTypeMaxLength).IsRequired();
            // Deliberately NOT HasConversion<string>() — AttentionSeverity's own comment says why
            // its declaration order matters: "Ordered so OrderByDescending puts the most urgent
            // first". Storing it as a string would sort the brief's core query alphabetically
            // ("Warning" before "Informational" before "Critical"), silently inverting severity
            // ordering in the database even though it reads correctly in memory. The default
            // integer mapping preserves the ordinal the enum was declared with on purpose.
            entity.Property(x => x.Severity).IsRequired();
            entity.Property(x => x.SubjectType).HasMaxLength(AutopilotFinding.SubjectTypeMaxLength).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(AutopilotFinding.SummaryMaxLength).IsRequired();
            entity.Property(x => x.DismissedReason).HasMaxLength(AutopilotFinding.DismissedReasonMaxLength);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property<uint>("Version").IsRowVersion();
            entity.HasOne<AutopilotRun>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.RunId }).OnDelete(DeleteBehavior.Cascade);
            // The brief's core query: everything not yet decided, most severe and soonest-detected
            // first, optionally narrowed to a property or portfolio.
            entity.HasIndex(x => new { x.OrganizationId, x.Status, x.Severity, x.DetectedAt });
            entity.HasIndex(x => new { x.OrganizationId, x.PropertyId, x.Status });
            entity.HasIndex(x => new { x.OrganizationId, x.PortfolioId, x.Status });
            entity.HasIndex(x => new { x.OrganizationId, x.SignalType });
        });

        model.Entity<AutopilotEvidence>(entity =>
        {
            entity.ToTable("Evidence");
            entity.Property(x => x.InputsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.SourceLinksJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.ImpactCategory).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.ImpactDescription).HasMaxLength(500);
            entity.Ignore(x => x.Inputs);
            entity.Ignore(x => x.SourceLinks);
            entity.Ignore(x => x.Impact);
            // One row per finding — a re-run that wants new evidence for the same subject creates
            // a new finding too, it never overwrites evidence in place (see the class comment).
            entity.HasIndex(x => new { x.OrganizationId, x.FindingId }).IsUnique();
            entity.HasOne<AutopilotFinding>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.FindingId }).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<AutopilotFeedback>(entity =>
        {
            entity.ToTable("Feedback");
            entity.Property(x => x.Sentiment).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(AutopilotFeedback.CommentMaxLength);
            entity.HasIndex(x => new { x.OrganizationId, x.FindingId, x.RecordedBy, x.RecordedAt });
            entity.HasOne<AutopilotFinding>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.FindingId }).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<AutopilotAuditEntry>(entity =>
        {
            entity.ToTable("AuditEntries");
            entity.Property(x => x.EventType).HasMaxLength(AutopilotAuditEntry.EventTypeMaxLength).IsRequired();
            entity.Property(x => x.SubjectType).HasMaxLength(AutopilotAuditEntry.SubjectTypeMaxLength).IsRequired();
            entity.Property(x => x.Detail).HasMaxLength(AutopilotAuditEntry.DetailMaxLength).IsRequired();
            // "What happened to this finding/run/etc." — the audit trail's own read pattern.
            entity.HasIndex(x => new { x.OrganizationId, x.SubjectType, x.SubjectId, x.OccurredAt });
        });

        model.Entity<AutopilotFindingRead>(entity =>
        {
            entity.ToTable("FindingReads");
            entity.HasIndex(x => new { x.OrganizationId, x.FindingId, x.ViewerId }).IsUnique();
            entity.HasOne<AutopilotFinding>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.FindingId }).OnDelete(DeleteBehavior.Cascade);
        });

        // CPM-8.08. Recommendation has no FK to Finding in the model - AutopilotFinding.Id is a
        // domain-validated Guid on AutopilotRecommendation the same way it is everywhere else in
        // this schema, and a real FK constraint here is exactly as safe (both tables share the
        // OrganizationId query filter). Deliberately not marked mutable-then-append-only:
        // Recommendations and ActionProposals change (Approve/Reject/MarkExecuted mutate their own
        // row) the same way Findings and Runs do - only Evidence/Feedback/AuditEntries are
        // append-only in this schema.
        model.Entity<AutopilotRecommendation>(entity =>
        {
            entity.ToTable("Recommendations");
            entity.Property(x => x.Description).HasMaxLength(AutopilotRecommendation.DescriptionMaxLength).IsRequired();
            entity.Property(x => x.DecisionReason).HasMaxLength(AutopilotRecommendation.DecisionReasonMaxLength);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property<uint>("Version").IsRowVersion();
            entity.HasOne<AutopilotFinding>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.FindingId }).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.OrganizationId, x.FindingId, x.Status });
        });

        model.Entity<AutopilotActionProposal>(entity =>
        {
            entity.ToTable("ActionProposals");
            entity.Property(x => x.ActionType).HasMaxLength(AutopilotActionProposal.ActionTypeMaxLength).IsRequired();
            entity.Property(x => x.PayloadSummary).HasMaxLength(AutopilotActionProposal.PayloadSummaryMaxLength).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(x => x.DecisionReason).HasMaxLength(AutopilotActionProposal.DecisionReasonMaxLength);
            entity.Property(x => x.ExecutionOutcome).HasMaxLength(AutopilotActionProposal.ExecutionOutcomeMaxLength);
            entity.Property(x => x.PayloadFieldsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.PreviewDescription).HasMaxLength(AutopilotActionProposal.PreviewDescriptionMaxLength).IsRequired();
            entity.Property(x => x.PreviewChangesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.RequiredApprovalCapability).HasMaxLength(AutopilotActionProposal.CapabilityMaxLength).IsRequired();
            entity.Property(x => x.RequiredExecutionCapability).HasMaxLength(AutopilotActionProposal.CapabilityMaxLength).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(AutopilotActionProposal.IdempotencyKeyMaxLength).IsRequired();
            entity.Property(x => x.RequiredConsentType).HasMaxLength(AutopilotActionProposal.ConsentTypeMaxLength);
            entity.Property(x => x.ConcurrencyToken).HasMaxLength(AutopilotActionProposal.ConcurrencyTokenMaxLength);
            entity.Property<uint>("Version").IsRowVersion();
            entity.HasOne<AutopilotRecommendation>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.RecommendationId }).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.OrganizationId, x.RecommendationId, x.Status });
            // The dedup guarantee IdempotencyKey's own comment on AutopilotActionProposal promises
            // - two proposals cannot share a key within one organization.
            entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique();
        });

        // Same tenant convention as the other contexts: composite key, no store-generated Id,
        // query filter fixed to the current organization.
        foreach (var entity in model.Model.GetEntityTypes().Where(x => typeof(TenantEntity).IsAssignableFrom(x.ClrType)))
        {
            model.Entity(entity.ClrType).HasKey(nameof(TenantEntity.OrganizationId), nameof(TenantEntity.Id));
            model.Entity(entity.ClrType).Property(nameof(TenantEntity.Id)).ValueGeneratedNever();
            var parameter = Expression.Parameter(entity.ClrType, "entity");
            var body = Expression.Equal(Expression.Property(parameter, nameof(TenantEntity.OrganizationId)),
                Expression.Property(Expression.Constant(this), nameof(OrganizationId)));
            entity.SetQueryFilter(Expression.Lambda(body, parameter));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        GuardWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void GuardWrites()
    {
        var id = OrganizationId;
        if (id == Guid.Empty) throw new TenantAccessException();
        foreach (var entry in ChangeTracker.Entries<TenantEntity>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Entity.OrganizationId != id ||
                (entry.State != EntityState.Added && entry.Property(x => x.OrganizationId).OriginalValue != id))
                throw new TenantAccessException();
        }
    }
}
