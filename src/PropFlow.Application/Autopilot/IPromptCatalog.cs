namespace PropFlow.Application.Autopilot;

// One named, versioned instruction set. Instructions is the literal text a real provider
// implementation would send as its system/instruction prompt - opaque to everything else in the
// system, including IModelGateway itself, which only threads PromptId/PromptVersion through a
// request and leaves reading Instructions to whichever gateway implementation actually calls a
// provider (the no-op gateway shipped in this task never reads it, by construction - see
// NoOpModelGateway.cs).
public sealed record PromptDefinition(string Id, int Version, string Instructions);

public enum PromptLookupOutcome { Found, NotFound }

public sealed record PromptLookupResult(PromptLookupOutcome Outcome, PromptDefinition? Prompt);

// A catalog holds exactly one active version per id - not a version history. Bumping a prompt's
// wording is expected to replace its entry and increment Version, the same "single current
// value, not a log" shape RepeatRepairPolicy and OrganizationSettings already use for
// organization-wide configuration; a future task that needs multiple prompt versions live at
// once (an A/B rollout, a pinned version per organization) extends this contract rather than
// this task guessing that shape now.
public interface IPromptCatalog
{
    PromptLookupResult Resolve(string promptId);
}
