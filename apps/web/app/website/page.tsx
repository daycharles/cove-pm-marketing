import Link from "next/link";
import Image from "next/image";

const capabilities = [
  {
    number: "01",
    title: "One queue for the work that matters",
    text: "Bring maintenance, vendor follow-up, resident requests, and turn risk into one calm operating view.",
    tone: "blue",
  },
  {
    number: "02",
    title: "Move every job forward",
    text: "Assign, schedule, notify, and document work in bulk—without losing the history behind each decision.",
    tone: "mint",
  },
  {
    number: "03",
    title: "Autopilot with a paper trail",
    text: "Surface evidence-backed recommendations, then keep a human in control of every consequential action.",
    tone: "peach",
  },
];

const plans = [
  { name: "Operations", price: "$2.50", note: "per unit / month", body: "The daily system for maintenance and field execution.", featured: false },
  { name: "Automation", price: "$4", note: "per unit / month", body: "Rules, resident communication, assets, and attention—connected.", featured: true },
  { name: "Platform", price: "$6–8", note: "per unit / month", body: "Reconciliation, API access, governance, and priority support.", featured: false },
];

export default function WebsitePage() {
  return (
    <main className="site-page">
      <nav className="site-nav" aria-label="Marketing navigation">
        <Link className="site-brand" href="/website" aria-label="Cove PM home">
          <Image className="site-brand-logo" src="/brand/cove-logo-light.png" alt="Cove PM — Property Management Software" width={270} height={118} priority />
        </Link>
        <div className="site-nav-links">
          <a href="#product">Product</a>
          <a href="#autopilot">Autopilot</a>
          <a href="#plans">Plans</a>
        </div>
      </nav>

      <section className="site-hero">
        <div className="site-hero-copy">
          <p className="site-kicker"><span /> Property operations, with a pulse</p>
          <h1>Make the day<br /><i>move.</i></h1>
          <p className="site-hero-lede">Cove gives property teams one clear place to see what needs attention, act with confidence, and keep residents in the loop.</p>
          <div className="site-hero-actions">
            <a className="site-button site-button-primary" href="#plans">Explore plans <span aria-hidden="true">→</span></a>
            <a className="site-text-link" href="#product">See how it works <span aria-hidden="true">↓</span></a>
          </div>
          <p className="site-proof"><span className="site-proof-dot" /> Built for the people who keep properties moving</p>
        </div>
        <div className="site-hero-visual" aria-label="Cove operations dashboard preview">
          <div className="site-orbit site-orbit-one" />
          <div className="site-orbit site-orbit-two" />
          <div className="site-dashboard-card">
            <div className="site-dashboard-top"><span className="site-window-dots"><b /><b /><b /></span><span>Today · Tidewater</span><span className="site-live"><i /> Live</span></div>
            <div className="site-dashboard-heading"><div><small>Wednesday, September 16</small><strong>Good morning, Maya</strong></div><span className="site-avatar">MS</span></div>
            <div className="site-metrics"><div><small>Open work</small><strong>24</strong><em>↓ 18% this week</em></div><div><small>Needs attention</small><strong className="site-warm">7</strong><em>3 critical</em></div></div>
            <div className="site-queue-label"><span>Priority queue</span><span>View all →</span></div>
            <div className="site-queue-item"><span className="site-status site-status-red" /><div><strong>Water heater · Unit 304</strong><small>Harbor Point · Waiting on vendor</small></div><b>Critical</b></div>
            <div className="site-queue-item"><span className="site-status site-status-yellow" /><div><strong>Turn risk · Unit 12B</strong><small>Parkside Commons · Due tomorrow</small></div><b>Review</b></div>
            <div className="site-queue-item"><span className="site-status site-status-blue" /><div><strong>Resident follow-up</strong><small>Oak Terrace · 2 hours ago</small></div><b>Assigned</b></div>
            <div className="site-dashboard-footer"><span>Autopilot brief ready</span><span>Open brief <strong>→</strong></span></div>
          </div>
          <div className="site-float-card"><span className="site-sparkle">✦</span><div><small>Autopilot found</small><strong>3 repeat-repair risks</strong></div><span className="site-float-arrow">↗</span></div>
        </div>
      </section>

      <section className="site-strip"><span>One operating rhythm</span><i /><span>Clear ownership</span><i /><span>Better resident moments</span><i /><span>Evidence, not guesswork</span></section>

      <section className="site-section site-product" id="product">
        <div className="site-section-intro"><p className="site-kicker"><span /> The operating layer</p><h2>Less hunting.<br /><i>More doing.</i></h2><p>When every request, asset, vendor, and resident conversation has a place, the team spends less time coordinating work and more time completing it.</p></div>
        <div className="site-capability-grid">{capabilities.map((item, index) => <article className={`site-capability site-capability-${item.tone}`} key={item.number}><span>{item.number}</span><h3>{item.title}</h3><p>{item.text}</p><a href={`#capability-${index + 1}`} aria-label={`Learn about ${item.title}`}>Learn more <b>↗</b></a></article>)}</div>
        <div className="site-capability-details">
          <article id="capability-1" className="site-capability-detail">
            <div><span className="site-detail-label">01 · One queue</span><h3>See the work before it becomes noise.</h3></div>
            <div><p>Cove brings requests, maintenance, turns, inspections, and vendor follow-up into one prioritized view. Every item has an owner, a next step, and the context needed to move it forward.</p><ul><li>Prioritize by urgency, SLA, property, or resident impact</li><li>Keep conversations, photos, documents, and updates with the work</li><li>Give every team member a clear next action</li></ul></div>
          </article>
          <article id="capability-2" className="site-capability-detail">
            <div><span className="site-detail-label">02 · Move work forward</span><h3>Make execution feel lighter.</h3></div>
            <div><p>From assignment through completion, Cove keeps the handoffs visible. Teams can schedule work in bulk, coordinate vendors, and communicate with residents without rebuilding the story at every step.</p><ul><li>Assign and schedule work across properties and teams</li><li>Automate updates while keeping a human review point</li><li>Capture completion history for faster follow-through</li></ul></div>
          </article>
          <article id="capability-3" className="site-capability-detail">
            <div><span className="site-detail-label">03 · Autopilot</span><h3>Use intelligence without losing control.</h3></div>
            <div><p>Autopilot highlights the signals most likely to affect your day, explains the evidence behind each recommendation, and waits for approval before consequential actions happen.</p><ul><li>Surface repeat repairs, stalled work, turn risk, and SLA risk</li><li>Trace recommendations back to source records</li><li>Keep approvals, permissions, and audit trails in place</li></ul></div>
          </article>
        </div>
      </section>

      <section className="site-autopilot" id="autopilot">
        <div className="site-autopilot-visual"><div className="site-signal signal-one">SLA risk <b>↓</b></div><div className="site-signal signal-two">Vendor follow-up <b>↗</b></div><div className="site-signal signal-three">Turn risk <b>!</b></div><div className="site-autopilot-core"><span>✦</span><strong>autopilot</strong><small>your daily operating brief</small></div></div>
        <div className="site-autopilot-copy"><p className="site-kicker"><span /> Meet your next best action</p><h2>Intelligence that<br /><i>knows its place.</i></h2><p>Autopilot connects the signals already inside your portfolio and turns them into a brief your team can trust. It explains the why, shows the evidence, and asks before it acts.</p><div className="site-check-list"><span>✓</span><p><strong>See the signal.</strong> SLA risk, stalled work, turn deadlines, vendor follow-up, and more.</p><span>✓</span><p><strong>Understand the impact.</strong> Every recommendation links back to the source records.</p><span>✓</span><p><strong>Stay in control.</strong> Approvals, permissions, consent, and audit trails are built in.</p></div><a className="site-text-link site-text-link-light" href="#autopilot-details">Explore Autopilot <span aria-hidden="true">→</span></a></div>
      </section>

      <section className="site-autopilot-details" id="autopilot-details">
        <div className="site-autopilot-details-intro"><p className="site-kicker"><span /> A closer look</p><h2>The right help,<br /><i>at the right moment.</i></h2><p>Autopilot works in a simple loop that keeps your team informed and in charge.</p></div>
        <div className="site-autopilot-steps">
          <article><span>01</span><h3>Notice</h3><p>Cove watches the signals across your portfolio—patterns, deadlines, stalled work, and resident impact.</p></article>
          <article><span>02</span><h3>Explain</h3><p>Each recommendation shows the evidence behind it, so your team can make a fast, informed decision.</p></article>
          <article><span>03</span><h3>Assist</h3><p>Autopilot prepares the next step, then pauses for the approval, permission, or context only your team can provide.</p></article>
        </div>
        <div className="site-autopilot-promise"><strong>Built for confident action.</strong><span>Human approval · Source-linked evidence · Clear audit trail</span></div>
      </section>

      <section className="site-section site-plans" id="plans"><div className="site-section-intro site-plans-intro"><p className="site-kicker"><span /> A calmer way to scale</p><h2>Start with the work.<br /><i>Grow into more.</i></h2><p>Simple packages for the way property teams actually grow. Every plan includes the foundation you need to keep work moving.</p></div><div className="site-plan-grid">{plans.map((plan) => <article className={`site-plan ${plan.featured ? "site-plan-featured" : ""}`} key={plan.name}>{plan.featured && <span className="site-plan-badge">Most popular</span>}<h3>{plan.name}</h3><p>{plan.body}</p><div className="site-plan-price"><strong>{plan.price}</strong><span>{plan.note}</span></div><a className={plan.featured ? "site-button site-button-primary" : "site-button site-button-outline"} href="mailto:hello@cove.pm">Talk to us <span aria-hidden="true">→</span></a><small>Migration support available</small></article>)}</div><p className="site-plan-footnote">All plans are organization-based. Autopilot model-backed features are available with transparent usage limits. <a href="mailto:hello@cove.pm">Ask about your portfolio →</a></p></section>

      <section className="site-final-cta"><div><p className="site-kicker"><span /> Make room for better work</p><h2>Your portfolio has<br /><i>momentum.</i></h2></div><a className="site-button site-button-light" href="mailto:hello@cove.pm">Start a conversation <span aria-hidden="true">→</span></a></section>
      <footer className="site-footer"><Link className="site-brand" href="/website" aria-label="Cove PM home"><Image className="site-brand-logo" src="/brand/cove-logo-light.png" alt="Cove PM — Property Management Software" width={190} height={83} /></Link><span>Property operations by Averion Software</span><span>© 2026 Cove PM</span></footer>
    </main>
  );
}
