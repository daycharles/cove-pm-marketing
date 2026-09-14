"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { AppShell } from "../components/app-shell";
import { ProtectedPage } from "../components/protected-page";
import {
  api,
  ApiError,
  type AutopilotActionProposal,
  type AutopilotBriefPage,
  type AutopilotFinding,
  type AutopilotFindingLifecycleStatus,
  type AutopilotRecommendation,
  type Portfolio,
  type PropertyReference,
  type Session,
} from "../../lib/api";

// CPM-8.06. Friendly labels for the eleven signal types CPM-8.02's catalog produces
// (PropFlow.Domain.Autopilot.SignalTypes). SignalType stays server-side validated free text, not
// a closed enum — see that file's own comment — so an unrecognized value here falls back to the
// raw string rather than disappearing, the same way reasonLabels does not exist in the attention
// page for a reason it doesn't know about.
const signalTypeLabels: Record<string, string> = {
  SlaRisk: "SLA risk",
  StalledWork: "Stalled work",
  VendorFollowUp: "Vendor follow-up",
  TurnRisk: "Unit turn at risk",
  RepeatRepair: "Repeat repair",
  LeaseDeadline: "Lease deadline",
  PaymentDeadline: "Payment deadline",
  BudgetVariance: "Budget variance",
  InvoiceException: "Invoice exception",
  ComplianceDeadline: "Compliance deadline",
  AssetReplacement: "Asset replacement",
};
const signalTypeOptions = Object.keys(signalTypeLabels);
function signalLabel(signalType: string) {
  return signalTypeLabels[signalType] ?? signalType;
}

const statusLabels: Record<AutopilotFindingLifecycleStatus, string> = {
  New: "New",
  Reviewed: "Reviewed",
  Snoozed: "Snoozed",
  Dismissed: "Dismissed",
  Resolved: "Resolved",
};

// Only two subject types resolve to a real detail page today. Every other SubjectType
// (BudgetLine, ComplianceObligation, LeaseNotice, LeaseCharge, PayableInvoice, ReceivableInvoice)
// has no single-record route in apps/web yet — rendering a link for those would be a link to
// nowhere, so the row shows the subject as plain text instead of fabricating a href.
function subjectHref(subjectType: string, subjectId: string): string | null {
  if (subjectType === "WorkItem") return `/work/${subjectId}`;
  if (subjectType === "Asset") return `/assets/${subjectId}`;
  return null;
}

// CPM-8.09. GovernedActionTypes.cs's seven constants, with the form fields each one's own
// endpoint (AutopilotEndpoints.cs's "propose an action" routes) requires. `kind` picks the input
// control; `optional` fields are omitted from the request body when blank rather than sent as "".
type ActionFormField = {
  key: string;
  label: string;
  kind: "guid" | "text" | "datetime" | "date" | "number" | "select";
  options?: readonly string[];
  optional?: boolean;
};
const actionTypeFields: Record<string, readonly ActionFormField[]> = {
  AssignVendor: [
    { key: "workId", label: "Work item ID", kind: "guid" },
    { key: "vendorId", label: "Vendor ID", kind: "guid" },
  ],
  AssignEmployee: [
    { key: "workId", label: "Work item ID", kind: "guid" },
    { key: "employeeId", label: "Employee ID", kind: "guid" },
  ],
  ScheduleWork: [
    { key: "workId", label: "Work item ID", kind: "guid" },
    { key: "scheduledStart", label: "Start", kind: "datetime" },
    { key: "scheduledEnd", label: "End", kind: "datetime" },
  ],
  CreateFollowUp: [
    { key: "propertyId", label: "Property ID", kind: "guid" },
    { key: "title", label: "Title", kind: "text" },
    { key: "description", label: "Description", kind: "text", optional: true },
    { key: "dueDate", label: "Due date", kind: "date", optional: true },
  ],
  DraftCommunication: [
    {
      key: "recipientType",
      label: "Recipient type",
      kind: "select",
      options: ["Resident", "Vendor"],
    },
    { key: "recipientId", label: "Recipient ID", kind: "guid" },
    { key: "channel", label: "Channel", kind: "select", options: ["Sms", "Email"] },
    { key: "draftText", label: "Draft text", kind: "text" },
  ],
  RequestApproval: [
    { key: "subjectType", label: "Subject type", kind: "text" },
    { key: "subjectId", label: "Subject ID", kind: "guid" },
    { key: "note", label: "Note", kind: "text", optional: true },
  ],
  CreatePurchaseOrderDraft: [
    { key: "vendorId", label: "Vendor ID", kind: "guid" },
    { key: "number", label: "PO number", kind: "text" },
    { key: "amount", label: "Amount", kind: "number" },
    { key: "approvalThreshold", label: "Approval threshold", kind: "number" },
    { key: "propertyId", label: "Property ID", kind: "guid", optional: true },
    { key: "workItemId", label: "Work item ID", kind: "guid", optional: true },
  ],
};
const actionTypeLabels: Record<string, string> = {
  AssignVendor: "Assign vendor",
  AssignEmployee: "Assign employee",
  ScheduleWork: "Schedule work",
  CreateFollowUp: "Create follow-up",
  DraftCommunication: "Draft communication",
  RequestApproval: "Request approval",
  CreatePurchaseOrderDraft: "Create purchase order draft",
};
function actionTypeLabel(actionType: string) {
  return actionTypeLabels[actionType] ?? actionType;
}

const PAGE_SIZE = 50;

type ConfirmState =
  | { action: "dismiss"; finding: AutopilotFinding; reason: string }
  | { action: "resolve"; finding: AutopilotFinding }
  | { action: "snooze"; finding: AutopilotFinding; until: string };

export default function AutopilotPage() {
  return (
    <ProtectedPage capability="Autopilot.Manage">
      {(session) => <AutopilotBrief session={session} />}
    </ProtectedPage>
  );
}

function AutopilotBrief({ session }: { session: Session }) {
  const [brief, setBrief] = useState<AutopilotBriefPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [runBusy, setRunBusy] = useState(false);
  const [runNotice, setRunNotice] = useState("");
  const [runError, setRunError] = useState("");
  const [page, setPage] = useState(1);
  const [propertyId, setPropertyId] = useState("");
  const [portfolioId, setPortfolioId] = useState("");
  const [signalType, setSignalType] = useState("");
  const [status, setStatus] = useState<AutopilotFindingLifecycleStatus | "">("");
  const [properties, setProperties] = useState<PropertyReference[]>([]);
  const [portfolios, setPortfolios] = useState<Portfolio[]>([]);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [confirm, setConfirm] = useState<ConfirmState | null>(null);
  const [readIds, setReadIds] = useState<Set<string>>(new Set());

  async function load(targetPage = page) {
    setLoading(true);
    setError("");
    try {
      const result = await api.autopilot.brief({
        propertyId: propertyId || undefined,
        portfolioId: portfolioId || undefined,
        signalType: signalType || undefined,
        status: status || undefined,
        page: targetPage,
        pageSize: PAGE_SIZE,
      });
      setBrief(result);
      setPage(targetPage);
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "Unable to load the daily brief.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void load(1);
      api.properties
        .list()
        .then(setProperties)
        .catch(() => setProperties([]));
      api.portfolios
        .list()
        .then(setPortfolios)
        .catch(() => setPortfolios([]));
    }, 0);
    return () => window.clearTimeout(timer);
    // Re-run whenever a filter changes; page resets to 1 in that case (see filter handlers below).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [propertyId, portfolioId, signalType, status]);

  function onPropertyChange(value: string) {
    setPropertyId(value);
    setExpandedId(null);
  }
  function onPortfolioChange(value: string) {
    setPortfolioId(value);
    setExpandedId(null);
  }
  function onSignalTypeChange(value: string) {
    setSignalType(value);
    setExpandedId(null);
  }
  function onStatusChange(value: string) {
    setStatus(value as AutopilotFindingLifecycleStatus | "");
    setExpandedId(null);
  }

  async function runNow() {
    setRunBusy(true);
    setRunNotice("");
    setRunError("");
    try {
      const result = await api.autopilot.run("Manual");
      setRunNotice(
        result.findingCount === 1
          ? "Analysis complete — 1 finding."
          : `Analysis complete — ${result.findingCount} findings.`,
      );
      await load(1);
    } catch (cause) {
      // A 502 from POST /runs means AutopilotRunOutcome.Failed (EfAutopilotRunner.cs) — the sweep
      // itself threw. Every analyzer is deterministic today (no analyzer calls IModelGateway yet),
      // so this is the closest honest reading of "the analysis is unavailable right now": say so
      // without claiming a specific vendor or provider is down, and keep showing whatever the
      // brief last had rather than clearing it.
      setRunError(
        cause instanceof ApiError
          ? `The analysis run did not complete: ${cause.message}`
          : "The analysis run did not complete.",
      );
    } finally {
      setRunBusy(false);
    }
  }

  function toggleExpanded(finding: AutopilotFinding) {
    const next = expandedId === finding.id ? null : finding.id;
    setExpandedId(next);
    if (next && !readIds.has(finding.id) && !finding.isRead) {
      setReadIds((current) => new Set(current).add(finding.id));
      void api.autopilot.markRead(finding.id).catch(() => {
        // Best-effort — a failed read-marker never blocks the reviewer from seeing the finding.
        setReadIds((current) => {
          const copy = new Set(current);
          copy.delete(finding.id);
          return copy;
        });
      });
    }
  }

  const totalPages = brief ? Math.max(1, Math.ceil(brief.totalCount / brief.pageSize)) : 1;
  const filtersActive = Boolean(propertyId || portfolioId || signalType || status);

  return (
    <AppShell session={session}>
      <section className="detail-workspace">
        <div className="work-heading">
          <div>
            <h1>Autopilot daily brief</h1>
            <p>Deterministic findings across work, leasing, accounting, compliance and assets.</p>
          </div>
          <button className="secondary" onClick={() => void runNow()} disabled={runBusy}>
            {runBusy ? "Running…" : "Run analysis now"}
          </button>
        </div>

        {runNotice && <p className="message">{runNotice}</p>}
        {runError && (
          <p className="message" role="alert">
            {runError}
          </p>
        )}
        {error && (
          <p className="message" role="alert">
            {error}
          </p>
        )}

        <section className="panel autopilot-filters">
          <label>
            Property
            <select value={propertyId} onChange={(event) => onPropertyChange(event.target.value)}>
              <option value="">All properties</option>
              {properties.map((property) => (
                <option key={property.id} value={property.id}>
                  {property.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            Portfolio
            <select value={portfolioId} onChange={(event) => onPortfolioChange(event.target.value)}>
              <option value="">All portfolios</option>
              {portfolios.map((portfolio) => (
                <option key={portfolio.id} value={portfolio.id}>
                  {portfolio.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            Signal
            <select value={signalType} onChange={(event) => onSignalTypeChange(event.target.value)}>
              <option value="">All signals</option>
              {signalTypeOptions.map((type) => (
                <option key={type} value={type}>
                  {signalLabel(type)}
                </option>
              ))}
            </select>
          </label>
          <label>
            Status
            <select value={status} onChange={(event) => onStatusChange(event.target.value)}>
              <option value="">Workable (default)</option>
              {(Object.keys(statusLabels) as AutopilotFindingLifecycleStatus[]).map((key) => (
                <option key={key} value={key}>
                  {statusLabels[key]}
                </option>
              ))}
            </select>
          </label>
        </section>

        <section className="panel">
          <div className="attention-list-heading">
            <h2>
              {status ? `${statusLabels[status]} findings` : "Workable findings"}
              {brief ? ` (${brief.totalCount})` : ""}
            </h2>
            <button className="link-button" type="button" onClick={() => void load(page)}>
              Refresh
            </button>
          </div>

          {loading ? (
            <p>Loading…</p>
          ) : !brief || brief.items.length === 0 ? (
            <p>
              {filtersActive
                ? "No findings match these filters."
                : "Nothing needs attention right now. Run analysis to check for new findings."}
            </p>
          ) : (
            <>
              <ul className="attention-list autopilot-list">
                {brief.items.map((finding) => (
                  <FindingRow
                    key={finding.id}
                    finding={{ ...finding, isRead: finding.isRead || readIds.has(finding.id) }}
                    expanded={expandedId === finding.id}
                    onToggle={() => toggleExpanded(finding)}
                    onDismiss={(reason) => setConfirm({ action: "dismiss", finding, reason })}
                    onResolve={() => setConfirm({ action: "resolve", finding })}
                    onSnooze={(until) => setConfirm({ action: "snooze", finding, until })}
                    onReview={async () => {
                      await api.autopilot.review(finding.id);
                      await load(page);
                    }}
                    onReopen={async () => {
                      await api.autopilot.reopen(finding.id);
                      await load(page);
                    }}
                    onFeedback={async (sentiment, comment) => {
                      await api.autopilot.feedback(finding.id, sentiment, comment);
                    }}
                  />
                ))}
              </ul>
              <div className="autopilot-pagination">
                <button
                  className="secondary"
                  disabled={page <= 1 || loading}
                  onClick={() => void load(page - 1)}
                >
                  Previous
                </button>
                <span>
                  Page {page} of {totalPages}
                </span>
                <button
                  className="secondary"
                  disabled={page >= totalPages || loading}
                  onClick={() => void load(page + 1)}
                >
                  Next
                </button>
              </div>
            </>
          )}
        </section>
      </section>

      {confirm && (
        <ConfirmModal
          confirm={confirm}
          onCancel={() => setConfirm(null)}
          onConfirmed={() => {
            setConfirm(null);
            void load(page);
          }}
        />
      )}
    </AppShell>
  );
}

function FindingRow({
  finding,
  expanded,
  onToggle,
  onDismiss,
  onResolve,
  onSnooze,
  onReview,
  onReopen,
  onFeedback,
}: {
  finding: AutopilotFinding;
  expanded: boolean;
  onToggle: () => void;
  onDismiss: (reason: string) => void;
  onResolve: () => void;
  onSnooze: (until: string) => void;
  onReview: () => Promise<void>;
  onReopen: () => Promise<void>;
  onFeedback: (sentiment: "Helpful" | "NotHelpful", comment: string) => Promise<void>;
}) {
  const [busy, setBusy] = useState(false);
  const [rowError, setRowError] = useState("");
  const [dismissReason, setDismissReason] = useState("");
  const [snoozeUntil, setSnoozeUntil] = useState(defaultSnoozeDate());
  const [feedbackComment, setFeedbackComment] = useState("");
  const [feedbackSent, setFeedbackSent] = useState<"Helpful" | "NotHelpful" | null>(null);

  async function run(action: () => Promise<void>) {
    setBusy(true);
    setRowError("");
    try {
      await action();
    } catch (cause) {
      setRowError(
        cause instanceof ApiError ? cause.message : "That action could not be completed.",
      );
    } finally {
      setBusy(false);
    }
  }

  const isTerminal = finding.status === "Dismissed" || finding.status === "Resolved";
  const isNew = finding.status === "New";
  const canReopen = finding.status === "Snoozed" || isTerminal;

  return (
    <li className={`attention-row autopilot-row severity-${finding.severity.toLowerCase()}`}>
      <button type="button" className="autopilot-row-toggle" onClick={onToggle}>
        <div className="attention-row-main">
          <strong className={finding.isRead ? "" : "autopilot-unread"}>{finding.summary}</strong>
          <span className="attention-reason">{signalLabel(finding.signalType)}</span>
        </div>
        <dl className="attention-row-meta">
          <div>
            <dt>Impact</dt>
            <dd>
              {finding.impact?.estimatedAmount != null
                ? money(finding.impact.estimatedAmount)
                : "—"}
            </dd>
          </div>
          <div>
            <dt>Status</dt>
            <dd>
              <span className="badge">{statusLabels[finding.status]}</span>
            </dd>
          </div>
          <div>
            <dt>Detected</dt>
            <dd>{formatDate(finding.detectedAt)}</dd>
          </div>
        </dl>
      </button>

      {expanded && (
        <div className="autopilot-detail">
          {rowError && (
            <p className="message" role="alert">
              {rowError}
            </p>
          )}

          <dl className="autopilot-detail-grid">
            <div>
              <dt>Subject</dt>
              <dd>
                {subjectHref(finding.subjectType, finding.subjectId) ? (
                  <Link href={subjectHref(finding.subjectType, finding.subjectId)!}>
                    {finding.subjectType} ({finding.subjectId.slice(0, 8)})
                  </Link>
                ) : (
                  `${finding.subjectType} (${finding.subjectId.slice(0, 8)})`
                )}
              </dd>
            </div>
            <div>
              <dt>Freshness</dt>
              <dd>{formatDate(finding.freshnessAsOf)}</dd>
            </div>
            <div>
              <dt>Confidence</dt>
              <dd>{Math.round(finding.confidence * 100)}%</dd>
            </div>
            {finding.snoozedUntil && (
              <div>
                <dt>Snoozed until</dt>
                <dd>{formatDate(finding.snoozedUntil)}</dd>
              </div>
            )}
          </dl>

          {finding.impact && (
            <p className="autopilot-impact">
              <strong>{finding.impact.category}:</strong> {finding.impact.description}
              {finding.impact.estimatedAmount != null
                ? ` (${money(finding.impact.estimatedAmount)})`
                : " — no dollar estimate available for this finding."}
            </p>
          )}

          {finding.inputs.length > 0 && (
            <div className="autopilot-evidence">
              <h3>Calculation inputs</h3>
              <ul>
                {finding.inputs.map((input) => (
                  <li key={input.name}>
                    <span>{input.name}</span>
                    <span>{input.value}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {finding.sourceLinks.length > 0 && (
            <div className="autopilot-evidence">
              <h3>Source records</h3>
              <ul>
                {finding.sourceLinks.map((link) => {
                  const href = subjectHref(link.entityType, link.entityId);
                  return (
                    <li key={`${link.entityType}-${link.entityId}`}>
                      {href ? (
                        <Link href={href}>
                          {link.entityType} ({link.entityId.slice(0, 8)})
                        </Link>
                      ) : (
                        `${link.entityType} (${link.entityId.slice(0, 8)})`
                      )}
                    </li>
                  );
                })}
              </ul>
            </div>
          )}

          <div className="autopilot-actions">
            {isNew && (
              <button className="secondary" disabled={busy} onClick={() => void run(onReview)}>
                Mark reviewed
              </button>
            )}
            {!isTerminal && (
              <>
                <button
                  className="secondary"
                  disabled={busy}
                  onClick={() => onDismiss(dismissReason)}
                >
                  Dismiss
                </button>
                <button className="secondary" disabled={busy} onClick={onResolve}>
                  Resolve
                </button>
                <button className="secondary" disabled={busy} onClick={() => onSnooze(snoozeUntil)}>
                  Snooze
                </button>
              </>
            )}
            {canReopen && (
              <button className="secondary" disabled={busy} onClick={() => void run(onReopen)}>
                Reopen
              </button>
            )}
          </div>

          {!isTerminal && (
            <label className="autopilot-dismiss-reason">
              Reason (used if you dismiss)
              <input
                type="text"
                value={dismissReason}
                maxLength={1000}
                onChange={(event) => setDismissReason(event.target.value)}
                placeholder="Not applicable to this property"
              />
            </label>
          )}

          {!isTerminal && (
            <label className="autopilot-dismiss-reason">
              Snooze until
              <input
                type="date"
                value={snoozeUntil}
                onChange={(event) => setSnoozeUntil(event.target.value)}
              />
            </label>
          )}

          <div className="autopilot-feedback">
            <label className="autopilot-dismiss-reason">
              Feedback note (optional)
              <input
                type="text"
                value={feedbackComment}
                onChange={(event) => setFeedbackComment(event.target.value)}
                placeholder="Why was this helpful or not?"
              />
            </label>
            <div className="autopilot-actions">
              <button
                className="secondary"
                disabled={busy}
                onClick={() =>
                  void run(async () => {
                    await onFeedback("Helpful", feedbackComment);
                    setFeedbackSent("Helpful");
                  })
                }
              >
                👍 Helpful
              </button>
              <button
                className="secondary"
                disabled={busy}
                onClick={() =>
                  void run(async () => {
                    await onFeedback("NotHelpful", feedbackComment);
                    setFeedbackSent("NotHelpful");
                  })
                }
              >
                👎 Not helpful
              </button>
              {feedbackSent && <span className="message">Feedback recorded.</span>}
            </div>
          </div>

          <RecommendationsPanel findingId={finding.id} />
        </div>
      )}
    </li>
  );
}

// CPM-8.09. A finding's recommendations, and — for each Approved one — the governed actions
// proposed against it. Fetched only once the finding row is expanded (the brief response has
// neither), the same lazy-load shape the evidence drawer already uses.
function RecommendationsPanel({ findingId }: { findingId: string }) {
  const [recommendations, setRecommendations] = useState<AutopilotRecommendation[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [description, setDescription] = useState("");
  const [creating, setCreating] = useState(false);

  async function load() {
    setLoading(true);
    setError("");
    try {
      setRecommendations(await api.autopilot.recommendations.list(findingId));
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "Unable to load recommendations.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [findingId]);

  async function create() {
    if (!description.trim()) return;
    setCreating(true);
    setError("");
    try {
      await api.autopilot.recommendations.create(findingId, description.trim());
      setDescription("");
      await load();
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "Unable to create the recommendation.");
    } finally {
      setCreating(false);
    }
  }

  return (
    <div className="autopilot-recommendations">
      <h3>Recommendations</h3>
      {error && (
        <p className="message" role="alert">
          {error}
        </p>
      )}
      <label className="autopilot-dismiss-reason">
        New recommendation
        <input
          type="text"
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          placeholder="Assign a vendor to unblock this."
        />
      </label>
      <div className="autopilot-actions">
        <button
          className="secondary"
          disabled={creating || !description.trim()}
          onClick={() => void create()}
        >
          {creating ? "Adding…" : "Add recommendation"}
        </button>
      </div>

      {loading ? (
        <p>Loading…</p>
      ) : !recommendations || recommendations.length === 0 ? (
        <p>No recommendations yet.</p>
      ) : (
        <ul className="autopilot-recommendation-list">
          {recommendations.map((recommendation) => (
            <RecommendationCard
              key={recommendation.id}
              recommendation={recommendation}
              onChanged={load}
            />
          ))}
        </ul>
      )}
    </div>
  );
}

function RecommendationCard({
  recommendation,
  onChanged,
}: {
  recommendation: AutopilotRecommendation;
  onChanged: () => Promise<void>;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [reason, setReason] = useState("");

  async function run(action: () => Promise<unknown>) {
    setBusy(true);
    setError("");
    try {
      await action();
      await onChanged();
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "That could not be completed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <li className="autopilot-recommendation-card">
      <div className="autopilot-card-heading">
        <span className="badge">{recommendation.status}</span>
        <p>{recommendation.description}</p>
      </div>
      {error && (
        <p className="message" role="alert">
          {error}
        </p>
      )}
      {recommendation.status === "Proposed" && (
        <>
          <label className="autopilot-dismiss-reason">
            Reason (used if you reject)
            <input type="text" value={reason} onChange={(event) => setReason(event.target.value)} />
          </label>
          <div className="autopilot-actions">
            <button
              className="secondary"
              disabled={busy}
              onClick={() =>
                void run(() => api.autopilot.recommendations.approve(recommendation.id))
              }
            >
              Approve
            </button>
            <button
              className="secondary"
              disabled={busy}
              onClick={() =>
                void run(() => api.autopilot.recommendations.reject(recommendation.id, reason))
              }
            >
              Reject
            </button>
          </div>
        </>
      )}
      {recommendation.status === "Approved" && (
        <ActionsPanel recommendationId={recommendation.id} />
      )}
    </li>
  );
}

function ActionsPanel({ recommendationId }: { recommendationId: string }) {
  const [proposals, setProposals] = useState<AutopilotActionProposal[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actionType, setActionType] = useState<string>(Object.keys(actionTypeFields)[0]);
  const [fields, setFields] = useState<Record<string, string>>({});
  const [proposing, setProposing] = useState(false);

  async function load() {
    setLoading(true);
    setError("");
    try {
      setProposals(await api.autopilot.actions.list(recommendationId));
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "Unable to load actions.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [recommendationId]);

  function setField(key: string, value: string) {
    setFields((current) => ({ ...current, [key]: value }));
  }

  async function propose() {
    setProposing(true);
    setError("");
    try {
      const actions = api.autopilot.actions;
      switch (actionType) {
        case "AssignVendor":
          await actions.proposeAssignVendor(
            recommendationId,
            fields.workId ?? "",
            fields.vendorId ?? "",
          );
          break;
        case "AssignEmployee":
          await actions.proposeAssignEmployee(
            recommendationId,
            fields.workId ?? "",
            fields.employeeId ?? "",
          );
          break;
        case "ScheduleWork": {
          // datetime-local carries no offset — convert to a real instant before it leaves the browser.
          const start = fields.scheduledStart ? new Date(fields.scheduledStart).toISOString() : "";
          const end = fields.scheduledEnd ? new Date(fields.scheduledEnd).toISOString() : "";
          await actions.proposeScheduleWork(recommendationId, fields.workId ?? "", start, end);
          break;
        }
        case "CreateFollowUp":
          await actions.proposeFollowUp(
            recommendationId,
            fields.propertyId ?? "",
            fields.title ?? "",
            fields.description,
            fields.dueDate,
          );
          break;
        case "DraftCommunication":
          await actions.proposeCommunicationDraft(
            recommendationId,
            fields.recipientType === "Vendor" ? "Vendor" : "Resident",
            fields.recipientId ?? "",
            fields.channel === "Email" ? "Email" : "Sms",
            fields.draftText ?? "",
          );
          break;
        case "RequestApproval":
          await actions.proposeRequestApproval(
            recommendationId,
            fields.subjectType ?? "",
            fields.subjectId ?? "",
            fields.note,
          );
          break;
        case "CreatePurchaseOrderDraft":
          await actions.proposePurchaseOrderDraft(
            recommendationId,
            fields.vendorId ?? "",
            fields.number ?? "",
            Number(fields.amount || "0"),
            Number(fields.approvalThreshold || "0"),
            fields.propertyId,
            fields.workItemId,
          );
          break;
      }
      setFields({});
      await load();
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "Unable to propose that action.");
    } finally {
      setProposing(false);
    }
  }

  return (
    <div className="autopilot-actions-panel">
      <h4>Actions</h4>
      {error && (
        <p className="message" role="alert">
          {error}
        </p>
      )}
      <label className="autopilot-dismiss-reason">
        Action type
        <select
          value={actionType}
          onChange={(event) => {
            setActionType(event.target.value);
            setFields({});
          }}
        >
          {Object.keys(actionTypeFields).map((type) => (
            <option key={type} value={type}>
              {actionTypeLabel(type)}
            </option>
          ))}
        </select>
      </label>
      {actionTypeFields[actionType].map((field) => (
        <label key={field.key} className="autopilot-dismiss-reason">
          {field.label}
          {field.optional ? " (optional)" : ""}
          {field.kind === "select" ? (
            <select
              value={fields[field.key] ?? field.options![0]}
              onChange={(event) => setField(field.key, event.target.value)}
            >
              {field.options!.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          ) : (
            <input
              type={
                field.kind === "datetime"
                  ? "datetime-local"
                  : field.kind === "date"
                    ? "date"
                    : field.kind === "number"
                      ? "number"
                      : "text"
              }
              value={fields[field.key] ?? ""}
              onChange={(event) => setField(field.key, event.target.value)}
            />
          )}
        </label>
      ))}
      <div className="autopilot-actions">
        <button className="secondary" disabled={proposing} onClick={() => void propose()}>
          {proposing ? "Proposing…" : "Propose action"}
        </button>
      </div>

      {loading ? (
        <p>Loading…</p>
      ) : !proposals || proposals.length === 0 ? (
        <p>No actions proposed yet.</p>
      ) : (
        <ul className="autopilot-action-list">
          {proposals.map((proposal) => (
            <ActionProposalCard key={proposal.id} proposal={proposal} onChanged={load} />
          ))}
        </ul>
      )}
    </div>
  );
}

function ActionProposalCard({
  proposal,
  onChanged,
}: {
  proposal: AutopilotActionProposal;
  onChanged: () => Promise<void>;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [reason, setReason] = useState("");

  async function run(action: () => Promise<unknown>) {
    setBusy(true);
    setError("");
    try {
      await action();
      await onChanged();
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "That could not be completed.");
    } finally {
      setBusy(false);
    }
  }

  // "Deep links into field/work records" (CPM-8.09's own scope line) — the payload's WorkId
  // field, when the adapter carries one, is the same real work item every other Autopilot deep
  // link already points at.
  const workId = proposal.payload.find((field) => field.name === "WorkId")?.value;
  const workHref = workId ? `/work/${workId}` : null;

  return (
    <li className="autopilot-action-card">
      <div className="autopilot-card-heading">
        <span className="badge">{actionTypeLabel(proposal.actionType)}</span>
        <span className="badge">{proposal.status}</span>
      </div>
      <p>{proposal.previewDescription}</p>
      {workHref && <Link href={workHref}>View work item</Link>}
      {proposal.previewChanges.length > 0 && (
        <ul className="autopilot-evidence-inline">
          {proposal.previewChanges.map((change) => (
            <li key={change.field}>
              <span>{change.field}</span>
              <span>
                {change.before ?? "—"} → {change.after}
              </span>
            </li>
          ))}
        </ul>
      )}
      {proposal.executionOutcome && (
        <p
          className={proposal.status === "Failed" ? "message" : "autopilot-impact"}
          role={proposal.status === "Failed" ? "alert" : undefined}
        >
          {proposal.executionOutcome}
        </p>
      )}
      {error && (
        <p className="message" role="alert">
          {error}
        </p>
      )}
      {proposal.status === "Proposed" && (
        <>
          <label className="autopilot-dismiss-reason">
            Reason (used if you reject)
            <input type="text" value={reason} onChange={(event) => setReason(event.target.value)} />
          </label>
          <div className="autopilot-actions">
            <button
              className="secondary"
              disabled={busy}
              onClick={() => void run(() => api.autopilot.actions.approve(proposal.id))}
            >
              Approve
            </button>
            <button
              className="secondary"
              disabled={busy}
              onClick={() => void run(() => api.autopilot.actions.reject(proposal.id, reason))}
            >
              Reject
            </button>
          </div>
        </>
      )}
      {proposal.status === "Approved" && (
        <div className="autopilot-actions">
          <button
            className="secondary"
            disabled={busy}
            onClick={() => void run(() => api.autopilot.actions.execute(proposal.id))}
          >
            Execute
          </button>
        </div>
      )}
    </li>
  );
}

function ConfirmModal({
  confirm,
  onCancel,
  onConfirmed,
}: {
  confirm: ConfirmState;
  onCancel: () => void;
  onConfirmed: () => void;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [until, setUntil] = useState(confirm.action === "snooze" ? confirm.until : "");

  async function confirmAction() {
    setBusy(true);
    setError("");
    try {
      if (confirm.action === "dismiss")
        await api.autopilot.dismiss(confirm.finding.id, confirm.reason);
      else if (confirm.action === "resolve") await api.autopilot.resolve(confirm.finding.id);
      else await api.autopilot.snooze(confirm.finding.id, new Date(until).toISOString());
      onConfirmed();
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "That action could not be completed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="modal-backdrop">
      <section className="modal" role="dialog" aria-modal="true">
        <h2>
          {confirm.action === "dismiss"
            ? "Dismiss this finding?"
            : confirm.action === "resolve"
              ? "Resolve this finding?"
              : "Snooze this finding?"}
        </h2>
        <p>{confirm.finding.summary}</p>
        {confirm.action === "snooze" && (
          <label className="autopilot-dismiss-reason">
            Until
            <input type="date" value={until} onChange={(event) => setUntil(event.target.value)} />
          </label>
        )}
        {error && (
          <p className="message" role="alert">
            {error}
          </p>
        )}
        <div className="modal-actions">
          <button className="secondary" onClick={onCancel} disabled={busy}>
            Cancel
          </button>
          <button onClick={() => void confirmAction()} disabled={busy}>
            {busy ? "Saving…" : "Confirm"}
          </button>
        </div>
      </section>
    </div>
  );
}

function defaultSnoozeDate() {
  const date = new Date();
  date.setDate(date.getDate() + 3);
  return date.toISOString().slice(0, 10);
}

function money(amount: number) {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(amount);
}

function formatDate(value?: string | null) {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? "—"
    : new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(date);
}
