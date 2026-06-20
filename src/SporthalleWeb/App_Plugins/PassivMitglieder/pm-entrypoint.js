const CSS = `
  :host { display: block; width: 100%; height: 100%; box-sizing: border-box; font-family: sans-serif; font-size: 14px; color: #1a1a2e; }
  * { box-sizing: border-box; }
  .subnav { background: #1A5FAD; display: flex; align-items: stretch; padding: 0 0.5rem; }
  .subnav-btn { color: rgba(255,255,255,0.75); background: none; border: none; border-bottom: 3px solid transparent; padding: 0.7rem 1.25rem; font-size: 0.875rem; font-weight: 500; cursor: pointer; }
  .subnav-btn:hover { color: #fff; background: rgba(255,255,255,0.1); }
  .subnav-btn.active { color: #fff; border-bottom-color: #fff; }
  .content { padding: 1.5rem; overflow-y: auto; height: calc(100% - 44px); background: #f5f6fa; }
  .msg { color: #888; padding: 1rem 0; }
  .error { color: #c62828; padding: 1rem 0; }
  .count { color: #666; margin: 0 0 1rem; }
  .table-wrap { overflow-x: auto; border-radius: 8px; box-shadow: 0 2px 12px rgba(0,0,0,0.06); }
  table { width: 100%; border-collapse: collapse; background: #fff; font-size: 0.85rem; }
  th { background: #1A5FAD; color: #fff; padding: 0.7rem 0.75rem; text-align: left; white-space: nowrap; }
  th.sort { cursor: pointer; user-select: none; }
  th.sort:hover { background: #1550a0; }
  td { padding: 0.6rem 0.75rem; border-bottom: 1px solid #eef0f5; vertical-align: top; }
  tr:last-child td { border-bottom: none; }
  tr.paid td { background: #f0f9f0; }
  .badge { display: inline-block; padding: 0.15rem 0.5rem; border-radius: 3px; font-size: 0.75rem; font-weight: 700; }
  .badge-bronze { background: #cd7f32; color: #fff; }
  .badge-silber { background: #aaa; color: #fff; }
  .badge-gold { background: #d4a017; color: #fff; }
  .paid-check { color: #2d8a4e; font-weight: 600; font-size: 0.8rem; }
  .btn { display: inline-block; padding: 0.45rem 1rem; border-radius: 5px; font-size: 0.85rem; font-weight: 600; cursor: pointer; border: none; text-decoration: none; transition: opacity 0.15s; }
  .btn:hover { opacity: 0.85; }
  .btn:disabled { opacity: 0.4; cursor: default; }
  .btn-pay { background: #1A5FAD; color: #fff; padding: 0.3rem 0.6rem; font-size: 0.78rem; }
  .btn-sec { background: #eaeef6; color: #1A5FAD; }
  .exports { display: flex; gap: 1rem; flex-wrap: wrap; padding-top: 0.5rem; }
  textarea.notes { width: 100%; min-width: 160px; border: 1px solid #dde1ec; border-radius: 4px; padding: 0.3rem; font-family: inherit; font-size: 0.82rem; resize: vertical; }
  textarea.notes:focus { outline: 2px solid #1A5FAD; border-color: transparent; }
  .toast { position: fixed; bottom: 1.5rem; right: 1.5rem; padding: 0.75rem 1.25rem; border-radius: 6px; font-weight: 600; font-size: 0.85rem; box-shadow: 0 4px 16px rgba(0,0,0,0.12); z-index: 1000; animation: fadein 0.2s; }
  .toast-ok { background: #2d8a4e; color: #fff; }
  .toast-err { background: #c0392b; color: #fff; }
  @keyframes fadein { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: none; } }
`;

class PmAdminElement extends HTMLElement {
  #shadow;
  #members = [];
  #sortCol = 'FieldNumber';
  #sortAsc = true;
  #tab = 'Mitglieder';
  #busy = new Set();

  connectedCallback() {
    this.#shadow = this.attachShadow({ mode: 'open' });
    this.#shadow.innerHTML = `<style>${CSS}</style>
      <nav class="subnav">
        <button class="subnav-btn active" data-tab="Mitglieder">Mitglieder</button>
        <button class="subnav-btn"        data-tab="Exporte">Exporte</button>
      </nav>
      <div class="content" id="content"><p class="msg">Lade Mitglieder…</p></div>`;

    this.#shadow.querySelectorAll('.subnav-btn').forEach(btn =>
      btn.addEventListener('click', () => this.#switchTab(btn.dataset.tab)));

    this.#loadMembers();
  }

  // ── Data loading ──────────────────────────────────────────────────────────

  async #loadMembers() {
    try {
      const res = await fetch('/api/passivmitglieder/admin/members', { credentials: 'include' });
      if (res.status === 401 || res.status === 403)
        return this.#setContent('<p class="error">Kein Zugriff. Bitte im Backoffice anmelden.</p>');
      if (!res.ok)
        return this.#setContent(`<p class="error">Fehler ${res.status} beim Laden der Mitglieder.</p>`);
      this.#members = await res.json();
      this.#renderMitglieder();
    } catch (e) {
      this.#setContent(`<p class="error">Netzwerkfehler: ${e.message}</p>`);
    }
  }

  // ── Rendering ─────────────────────────────────────────────────────────────

  #switchTab(tab) {
    this.#tab = tab;
    this.#shadow.querySelectorAll('.subnav-btn').forEach(b =>
      b.classList.toggle('active', b.dataset.tab === tab));
    tab === 'Mitglieder' ? this.#renderMitglieder() : this.#renderExporte();
  }

  #renderMitglieder() {
    if (this.#members.length === 0) {
      this.#setContent('<p class="msg">Noch keine Passivmitglieder registriert.</p>');
      return;
    }
    const sorted = this.#sorted();
    const rows = sorted.map(m => `
      <tr class="${m.paidAt ? 'paid' : ''}" data-id="${m.id}">
        <td>${m.fieldNumber}</td>
        <td>${m.vipLabel ?? ''}</td>
        <td><span class="badge badge-${m.levelKey}">${m.levelKey}</span><br><small>${m.level}</small></td>
        <td>${m.firstName} ${m.lastName}</td>
        <td><a href="mailto:${m.email}">${m.email}</a></td>
        <td>${m.addressLine}, ${m.postalCode} ${m.city}</td>
        <td>${m.createdAt}</td>
        <td>${m.paidAt
          ? `<span class="paid-check">✓ ${m.paidAt}</span>`
          : `<button class="btn btn-pay pay-btn" data-id="${m.id}">Als bezahlt markieren</button>`}</td>
        <td><textarea class="notes" rows="2" data-id="${m.id}">${this.#esc(m.notes ?? '')}</textarea></td>
      </tr>`).join('');

    this.#setContent(`
      <p class="count">${this.#members.length} Mitglied${this.#members.length === 1 ? '' : 'er'}</p>
      <div class="table-wrap">
        <table>
          <thead><tr>
            <th class="sort" data-col="FieldNumber">Feld ${this.#ind('FieldNumber')}</th>
            <th>VIP</th>
            <th class="sort" data-col="LevelKey">Stufe ${this.#ind('LevelKey')}</th>
            <th class="sort" data-col="LastName">Name ${this.#ind('LastName')}</th>
            <th>E-Mail</th>
            <th>Adresse</th>
            <th class="sort" data-col="CreatedAt">Angemeldet ${this.#ind('CreatedAt')}</th>
            <th>Bezahlt</th>
            <th>Notizen</th>
          </tr></thead>
          <tbody>${rows}</tbody>
        </table>
      </div>`);

    this.#shadow.querySelectorAll('th.sort').forEach(th =>
      th.addEventListener('click', () => this.#sort(th.dataset.col)));

    this.#shadow.querySelectorAll('.pay-btn').forEach(btn =>
      btn.addEventListener('click', () => this.#markPaid(+btn.dataset.id)));

    this.#shadow.querySelectorAll('textarea.notes').forEach(ta => {
      ta.addEventListener('blur', () => this.#saveNotes(+ta.dataset.id, ta.value));
    });
  }

  #renderExporte() {
    this.#setContent(`
      <div class="exports">
        <a href="/api/passivmitglieder/admin/export/excel"   class="btn btn-sec" target="_blank">Excel-Export</a>
        <a href="/api/passivmitglieder/admin/export/abaninja" class="btn btn-sec" target="_blank">AbaNinja-CSV</a>
      </div>`);
  }

  // ── Actions ───────────────────────────────────────────────────────────────

  async #markPaid(id) {
    if (this.#busy.has(id)) return;
    this.#busy.add(id);
    const btn = this.#shadow.querySelector(`.pay-btn[data-id="${id}"]`);
    if (btn) btn.disabled = true;
    try {
      const res = await fetch(`/api/passivmitglieder/${id}/paid`, { method: 'POST', credentials: 'include' });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      await this.#loadMembers();
      this.#toast('Zahlung gespeichert.', false);
    } catch (e) {
      this.#toast(e.message, true);
      if (btn) btn.disabled = false;
    } finally {
      this.#busy.delete(id);
    }
  }

  async #saveNotes(id, notes) {
    try {
      const res = await fetch(`/api/passivmitglieder/${id}/notes`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ notes })
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const m = this.#members.find(x => x.id === id);
      if (m) m.notes = notes;
      this.#toast('Notiz gespeichert.', false);
    } catch (e) {
      this.#toast(e.message, true);
    }
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  #sort(col) {
    this.#sortAsc = this.#sortCol === col ? !this.#sortAsc : true;
    this.#sortCol = col;
    this.#renderMitglieder();
  }

  #sorted() {
    return [...this.#members].sort((a, b) => {
      const av = a[this.#sortCol[0].toLowerCase() + this.#sortCol.slice(1)] ?? '';
      const bv = b[this.#sortCol[0].toLowerCase() + this.#sortCol.slice(1)] ?? '';
      return this.#sortAsc
        ? String(av).localeCompare(String(bv), 'de')
        : String(bv).localeCompare(String(av), 'de');
    });
  }

  #ind(col) { return this.#sortCol === col ? (this.#sortAsc ? '▲' : '▼') : ''; }
  #setContent(html) { this.#shadow.getElementById('content').innerHTML = html; }
  #esc(s) { return s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;'); }

  #toast(msg, isErr) {
    const el = document.createElement('div');
    el.className = `toast ${isErr ? 'toast-err' : 'toast-ok'}`;
    el.textContent = msg;
    this.#shadow.appendChild(el);
    setTimeout(() => el.remove(), 3000);
  }
}

customElements.define('pm-admin', PmAdminElement);
