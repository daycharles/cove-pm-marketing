"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";
import { AppShell } from "../components/app-shell";
import { ProtectedPage } from "../components/protected-page";
import {
  api,
  type BillingBalance,
  type BillingCharge,
  type BillingCredit,
  type DelinquencyCase,
  type LateFeeRule,
  type Lease,
  type PaymentMethod,
  type Reconciliation,
  type RecurringCharge,
  type ResidentReference,
  type Session,
} from "../../lib/api";

const money = (value: number | undefined) =>
  new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(value ?? 0);
const today = () => new Date().toISOString().slice(0, 10);

export default function BillingPage() {
  return (
    <ProtectedPage capability="Billing.Manage">
      {(session) => <BillingContent session={session} />}
    </ProtectedPage>
  );
}

function BillingContent({ session }: { session: Session }) {
  const [leases, setLeases] = useState<Lease[]>([]);
  const [residents, setResidents] = useState<ResidentReference[]>([]);
  const [selectedLeaseId, setSelectedLeaseId] = useState("");
  const [balance, setBalance] = useState<BillingBalance | null>(null);
  const [charges, setCharges] = useState<BillingCharge[]>([]);
  const [recurring, setRecurring] = useState<RecurringCharge[]>([]);
  const [credits, setCredits] = useState<BillingCredit[]>([]);
  const [methods, setMethods] = useState<PaymentMethod[]>([]);
  const [rules, setRules] = useState<LateFeeRule[]>([]);
  const [reconciliation, setReconciliation] = useState<Reconciliation[]>([]);
  const [delinquency, setDelinquency] = useState<DelinquencyCase[]>([]);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [paymentAmount, setPaymentAmount] = useState<Record<string, string>>({});
  const [schedule, setSchedule] = useState({
    description: "Monthly rent",
    amount: "",
    dayOfMonth: "1",
    startsOn: today(),
  });
  const [credit, setCredit] = useState({ amount: "", reason: "", issuedOn: today() });
  const [method, setMethod] = useState({ type: "Card", label: "", lastFour: "" });
  const [rule, setRule] = useState({
    name: "Standard late fee",
    graceDays: "5",
    flatAmount: "",
    percentOfOutstanding: "0",
    maximumAmount: "",
  });

  const residentNames = useMemo(
    () => new Map(residents.map((r) => [r.id, r.fullName])),
    [residents],
  );
  const selectedLease = leases.find((lease) => lease.id === selectedLeaseId);

  const loadQueues = async () => {
    const [ruleList, reconciliationList, delinquencyList] = await Promise.all([
      api.billing.lateFeeRules.list(),
      api.billing.reconciliation.list(),
      api.billing.delinquency.list(),
    ]);
    setRules(ruleList);
    setReconciliation(reconciliationList);
    setDelinquency(delinquencyList);
  };
  const loadLease = async (leaseId: string) => {
    const lease = leases.find((item) => item.id === leaseId);
    const [nextBalance, nextCharges, nextRecurring, nextCredits, nextMethods] = await Promise.all([
      api.billing.leases.balance(leaseId),
      api.billing.leases.charges(leaseId),
      api.billing.leases.recurringCharges(leaseId),
      api.billing.leases.credits(leaseId),
      lease ? api.billing.residents.paymentMethods(lease.residentId) : Promise.resolve([]),
    ]);
    setBalance(nextBalance);
    setCharges(nextCharges);
    setRecurring(nextRecurring);
    setCredits(nextCredits);
    setMethods(nextMethods);
  };
  const refresh = async () => {
    setError("");
    try {
      const [leaseList, residentList] = await Promise.all([
        api.leasing.leases.list(),
        api.residents.list(),
      ]);
      setLeases(leaseList);
      setResidents(residentList);
      const nextLeaseId = selectedLeaseId || leaseList[0]?.id || "";
      setSelectedLeaseId(nextLeaseId);
      await Promise.all([nextLeaseId ? loadLease(nextLeaseId) : Promise.resolve(), loadQueues()]);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Unable to load billing.");
    }
  };
  useEffect(() => {
    const timer = window.setTimeout(() => void refresh(), 0);
    return () => window.clearTimeout(timer);
  }, []);
  useEffect(() => {
    if (!selectedLeaseId || !leases.length) return;
    const timer = window.setTimeout(
      () =>
        void loadLease(selectedLeaseId).catch((caught) =>
          setError(caught instanceof Error ? caught.message : "Unable to load lease billing."),
        ),
      0,
    );
    return () => window.clearTimeout(timer);
  }, [selectedLeaseId]);

  const run = async (action: () => Promise<unknown>, success: string) => {
    setError("");
    try {
      await action();
      setMessage(success);
      await refresh();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Billing action failed.");
    }
  };
  const pay = async (charge: BillingCharge) => {
    const amount = Number(paymentAmount[charge.id] || charge.outstanding);
    await run(() => api.billing.charges.pay(charge.id, { amount }), "Payment recorded.");
  };
  const createSchedule = async (event: FormEvent) => {
    event.preventDefault();
    if (!selectedLeaseId) return;
    await run(
      () =>
        api.billing.recurringCharges.create({
          leaseId: selectedLeaseId,
          description: schedule.description,
          amount: Number(schedule.amount),
          dayOfMonth: Number(schedule.dayOfMonth),
          startsOn: schedule.startsOn,
        }),
      "Recurring charge created.",
    );
  };
  const createCredit = async (event: FormEvent) => {
    event.preventDefault();
    if (!selectedLeaseId) return;
    await run(
      () =>
        api.billing.credits.create(selectedLeaseId, {
          amount: Number(credit.amount),
          reason: credit.reason,
          issuedOn: credit.issuedOn,
        }),
      "Credit issued.",
    );
  };
  const addMethod = async (event: FormEvent) => {
    event.preventDefault();
    if (!selectedLease?.residentId) return;
    await run(
      () =>
        api.billing.residents.addPaymentMethod(selectedLease.residentId, {
          ...method,
          lastFour: method.lastFour || undefined,
        }),
      "Payment method added.",
    );
  };
  const createRule = async (event: FormEvent) => {
    event.preventDefault();
    await run(
      () =>
        api.billing.lateFeeRules.create({
          propertyId: null,
          name: rule.name,
          graceDays: Number(rule.graceDays),
          flatAmount: Number(rule.flatAmount),
          percentOfOutstanding: Number(rule.percentOfOutstanding),
          maximumAmount: rule.maximumAmount ? Number(rule.maximumAmount) : null,
        }),
      "Late-fee rule created.",
    );
  };
  const leaseLabel = (lease: Lease) =>
    `${residentNames.get(lease.residentId) ?? "Resident"} · ${lease.status} · ${lease.id.slice(0, 8)}`;

  return (
    <AppShell session={session}>
      <section className="panel">
        <h1>Billing</h1>
        <p>
          Run the property ledger: balances, recurring charges, payments, credits, fees, and
          collection exceptions.
        </p>
        {message && <p className="message">{message}</p>}
        {error && (
          <p className="message" role="alert">
            {error}
          </p>
        )}
        <div className="form-grid">
          <label>
            Lease
            <select
              value={selectedLeaseId}
              onChange={(event) => setSelectedLeaseId(event.target.value)}
            >
              <option value="">Select a lease</option>
              {leases.map((lease) => (
                <option key={lease.id} value={lease.id}>
                  {leaseLabel(lease)}
                </option>
              ))}
            </select>
          </label>
          <button className="secondary" type="button" onClick={() => void refresh()}>
            Refresh billing
          </button>
        </div>
      </section>

      <section className="panel">
        <h2>Portfolio snapshot</h2>
        <div className="metric-grid">
          <div>
            <span>Selected outstanding</span>
            <strong>{money(balance?.outstanding)}</strong>
          </div>
          <div>
            <span>Charged</span>
            <strong>{money(balance?.charged)}</strong>
          </div>
          <div>
            <span>Applied</span>
            <strong>{money(balance?.applied)}</strong>
          </div>
          <div>
            <span>Credits remaining</span>
            <strong>{money(balance?.creditsRemaining)}</strong>
          </div>
          <div>
            <span>Open delinquency</span>
            <strong>{delinquency.length}</strong>
          </div>
        </div>
      </section>

      <section className="panel">
        <h2>Lease ledger</h2>
        {!selectedLeaseId ? (
          <p>Select a lease to view its ledger.</p>
        ) : (
          <>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Description</th>
                    <th>Due</th>
                    <th>Amount</th>
                    <th>Outstanding</th>
                    <th>Payment</th>
                  </tr>
                </thead>
                <tbody>
                  {charges.map((charge) => (
                    <tr key={charge.id}>
                      <td>
                        {charge.description}
                        <br />
                        <small>
                          {charge.type} · {charge.status}
                        </small>
                      </td>
                      <td>{charge.dueOn}</td>
                      <td>{money(charge.amount)}</td>
                      <td>{money(charge.outstanding)}</td>
                      <td>
                        {charge.outstanding > 0 && (
                          <span className="inline-actions">
                            <input
                              aria-label={`Payment amount for ${charge.description}`}
                              type="number"
                              min="0.01"
                              step="0.01"
                              value={paymentAmount[charge.id] ?? ""}
                              placeholder={String(charge.outstanding)}
                              onChange={(event) =>
                                setPaymentAmount({
                                  ...paymentAmount,
                                  [charge.id]: event.target.value,
                                })
                              }
                            />
                            <button type="button" onClick={() => void pay(charge)}>
                              Collect
                            </button>
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="split-panels">
              <div>
                <h3>Recurring charges</h3>
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>Description</th>
                        <th>Amount</th>
                        <th>Schedule</th>
                        <th />
                      </tr>
                    </thead>
                    <tbody>
                      {recurring.map((item) => (
                        <tr key={item.id}>
                          <td>{item.description}</td>
                          <td>{money(item.amount)}</td>
                          <td>
                            Day {item.dayOfMonth} · {item.status}
                          </td>
                          <td>
                            <button
                              className="secondary"
                              type="button"
                              onClick={() =>
                                void run(
                                  () =>
                                    item.status === "Active"
                                      ? api.billing.recurringCharges.pause(item.id)
                                      : api.billing.recurringCharges.resume(item.id),
                                  item.status === "Active"
                                    ? "Schedule paused."
                                    : "Schedule resumed.",
                                )
                              }
                            >
                              {item.status === "Active" ? "Pause" : "Resume"}
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <form className="form-grid" onSubmit={createSchedule}>
                  <input
                    aria-label="Charge description"
                    placeholder="Description"
                    value={schedule.description}
                    onChange={(e) => setSchedule({ ...schedule, description: e.target.value })}
                    required
                  />
                  <input
                    aria-label="Charge amount"
                    type="number"
                    step="0.01"
                    placeholder="Amount"
                    value={schedule.amount}
                    onChange={(e) => setSchedule({ ...schedule, amount: e.target.value })}
                    required
                  />
                  <input
                    aria-label="Day of month"
                    type="number"
                    min="1"
                    max="28"
                    value={schedule.dayOfMonth}
                    onChange={(e) => setSchedule({ ...schedule, dayOfMonth: e.target.value })}
                    required
                  />
                  <input
                    aria-label="Start date"
                    type="date"
                    value={schedule.startsOn}
                    onChange={(e) => setSchedule({ ...schedule, startsOn: e.target.value })}
                    required
                  />
                  <button type="submit">Add recurring charge</button>
                </form>
                <button
                  className="secondary"
                  type="button"
                  onClick={() =>
                    void run(
                      () => api.billing.recurringCharges.run(today(), selectedLeaseId),
                      "Recurring charges generated.",
                    )
                  }
                >
                  Generate through today
                </button>
              </div>
              <div>
                <h3>Credits</h3>
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>Reason</th>
                        <th>Issued</th>
                        <th>Remaining</th>
                      </tr>
                    </thead>
                    <tbody>
                      {credits.map((item) => (
                        <tr key={item.id}>
                          <td>{item.reason}</td>
                          <td>{item.issuedOn}</td>
                          <td>{money(item.remaining)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <form className="form-grid" onSubmit={createCredit}>
                  <input
                    aria-label="Credit amount"
                    type="number"
                    step="0.01"
                    placeholder="Amount"
                    value={credit.amount}
                    onChange={(e) => setCredit({ ...credit, amount: e.target.value })}
                    required
                  />
                  <input
                    aria-label="Credit reason"
                    placeholder="Reason"
                    value={credit.reason}
                    onChange={(e) => setCredit({ ...credit, reason: e.target.value })}
                    required
                  />
                  <button type="submit">Issue credit</button>
                </form>
              </div>
            </div>
          </>
        )}
      </section>

      <section className="panel">
        <h2>Payment operations</h2>
        <div className="split-panels">
          <div>
            <h3>Payment methods</h3>
            {methods.map((item) => (
              <p key={item.id}>
                {item.label} {item.lastFour ? `•••• ${item.lastFour}` : ""} ·{" "}
                {item.isActive ? "Active" : "Inactive"}{" "}
                {item.isActive && (
                  <button
                    className="secondary"
                    type="button"
                    onClick={() =>
                      void run(
                        () => api.billing.paymentMethods.deactivate(item.id),
                        "Payment method deactivated.",
                      )
                    }
                  >
                    Deactivate
                  </button>
                )}
              </p>
            ))}
            {selectedLease && (
              <form className="form-grid" onSubmit={addMethod}>
                <input
                  aria-label="Payment method label"
                  placeholder="Label"
                  value={method.label}
                  onChange={(e) => setMethod({ ...method, label: e.target.value })}
                  required
                />
                <input
                  aria-label="Last four digits"
                  placeholder="Last four"
                  maxLength={4}
                  value={method.lastFour}
                  onChange={(e) => setMethod({ ...method, lastFour: e.target.value })}
                />
                <button type="submit">Add method</button>
              </form>
            )}
          </div>
          <div>
            <h3>Reconciliation queue</h3>
            {reconciliation.length === 0 ? (
              <p>No unmatched payments.</p>
            ) : (
              reconciliation.map((item) => (
                <p key={item.id}>
                  {item.providerReference} · {money(item.amount)}{" "}
                  <button
                    className="secondary"
                    type="button"
                    onClick={() =>
                      void run(
                        () => api.billing.reconciliation.resolve(item.id, "Reviewed in billing"),
                        "Reconciliation resolved.",
                      )
                    }
                  >
                    Resolve
                  </button>
                </p>
              ))
            )}
          </div>
        </div>
      </section>

      <section className="panel">
        <h2>Controls & collections</h2>
        <div className="split-panels">
          <div>
            <h3>Late-fee rules</h3>
            {rules.map((item) => (
              <p key={item.id}>
                {item.name} · {item.graceDays} day grace · {money(item.flatAmount)} flat
              </p>
            ))}
            <form className="form-grid" onSubmit={createRule}>
              <input
                aria-label="Rule name"
                value={rule.name}
                onChange={(e) => setRule({ ...rule, name: e.target.value })}
                required
              />
              <input
                aria-label="Grace days"
                type="number"
                min="0"
                value={rule.graceDays}
                onChange={(e) => setRule({ ...rule, graceDays: e.target.value })}
                required
              />
              <input
                aria-label="Flat fee"
                type="number"
                step="0.01"
                placeholder="Flat fee"
                value={rule.flatAmount}
                onChange={(e) => setRule({ ...rule, flatAmount: e.target.value })}
                required
              />
              <button type="submit">Add rule</button>
            </form>
            <button
              className="secondary"
              type="button"
              onClick={() =>
                void run(() => api.billing.lateFees.run(today()), "Late fees assessed.")
              }
            >
              Run late fees
            </button>
          </div>
          <div>
            <h3>Delinquency</h3>
            {delinquency.length === 0 ? (
              <p>No open delinquency cases.</p>
            ) : (
              delinquency.map((item) => (
                <p key={item.id}>
                  Lease {item.leaseId.slice(0, 8)} · {money(item.balance)}{" "}
                  <button
                    className="secondary"
                    type="button"
                    onClick={() =>
                      void run(() => api.billing.delinquency.contact(item.id), "Contact logged.")
                    }
                  >
                    Log contact
                  </button>
                  <button
                    className="secondary"
                    type="button"
                    onClick={() =>
                      void run(() => api.billing.delinquency.resolve(item.id), "Case resolved.")
                    }
                  >
                    Resolve
                  </button>
                </p>
              ))
            )}
            <button
              className="secondary"
              type="button"
              onClick={() =>
                void run(() => api.billing.delinquency.run(today()), "Delinquency scan complete.")
              }
            >
              Run delinquency scan
            </button>
          </div>
        </div>
      </section>
    </AppShell>
  );
}
