"use client";

import { useEffect, useState } from "react";
import { AppShell } from "../components/app-shell";
import { ProtectedPage } from "../components/protected-page";
import { api, type Announcement, type Session } from "../../lib/api";

export default function AnnouncementsPage() {
  return (
    <ProtectedPage capability="Leasing.Manage">
      {(session) => <AnnouncementsContent session={session} />}
    </ProtectedPage>
  );
}

function AnnouncementsContent({ session }: { session: Session }) {
  const [announcements, setAnnouncements] = useState<Announcement[]>([]);
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const refresh = () =>
    api.announcements
      .list()
      .then(setAnnouncements)
      .catch(() => setError("Unable to load announcements."));
  useEffect(() => {
    void refresh();
  }, []);
  const create = async (event: React.FormEvent) => {
    event.preventDefault();
    setError("");
    try {
      await api.announcements.create({
        title,
        body,
        expiresAt: expiresAt ? new Date(expiresAt).toISOString() : null,
      });
      setTitle("");
      setBody("");
      setExpiresAt("");
      await refresh();
      setMessage("Announcement created.");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Unable to create announcement.");
    }
  };
  const change = async (announcement: Announcement) => {
    try {
      if (announcement.status === "Draft") await api.announcements.publish(announcement.id);
      else await api.announcements.archive(announcement.id);
      await refresh();
      setMessage(
        announcement.status === "Draft" ? "Announcement published." : "Announcement archived.",
      );
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Unable to update announcement.");
    }
  };
  return (
    <AppShell session={session}>
      <section className="panel">
        <div className="work-heading">
          <div>
            <p className="eyebrow">Resident communications</p>
            <h1>Announcement queue</h1>
            <p>Review drafts, keep ownership visible, and publish only when the message is ready.</p>
          </div>
          <span className="badge">{announcements.filter((item) => item.status === "Draft").length} drafts</span>
        </div>
        {message && <p className="message">{message}</p>}
        {error && (
          <p className="message" role="alert">
            {error}
          </p>
        )}
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Title</th>
                <th>Status</th>
                <th>Expires</th>
                <th>Next action</th>
              </tr>
            </thead>
            <tbody>
              {announcements.map((announcement) => (
                <tr key={announcement.id}>
                  <td>{announcement.title}</td>
                  <td><span className={`badge ${announcement.status === "Published" ? "badge-complete" : ""}`}>{announcement.status}</span></td>
                  <td>
                    {announcement.expiresAt
                      ? new Date(announcement.expiresAt).toLocaleString()
                      : "Never"}
                  </td>
                  <td>
                    {announcement.status !== "Archived" && (
                      <button type="button" onClick={() => void change(announcement)}>
                        {announcement.status === "Draft" ? "Review & publish" : "Archive update"}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!announcements.length && <p className="queue-empty"><strong>No announcements yet.</strong><span>Create a draft for the next resident-facing update.</span></p>}
        </div>
      </section>
      <section className="panel">
        <h2>Draft an announcement</h2>
        <p className="hint">Drafts remain internal until you publish them. The timeline of delivery is retained in the resident portal.</p>
        <form className="form-grid" onSubmit={(event) => void create(event)}>
          <input
            aria-label="Announcement title"
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            placeholder="Title"
            required
          />
          <textarea
            aria-label="Announcement body"
            value={body}
            onChange={(event) => setBody(event.target.value)}
            placeholder="Message for residents"
            required
          />
          <input
            aria-label="Announcement expiry"
            type="datetime-local"
            value={expiresAt}
            onChange={(event) => setExpiresAt(event.target.value)}
          />
          <button type="submit">Create announcement</button>
        </form>
      </section>
    </AppShell>
  );
}
