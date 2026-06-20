import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';

// ── Konstanten ────────────────────────────────────────────────────────────────

const BLOCK_H   = 24;   // px pro 30-min-Block
const TIME_W    = 52;   // px für die Zeitspalte
const BLOCK_MIN = 30;
const DAYS      = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'];

// ── Hilfsfunktionen ───────────────────────────────────────────────────────────

function getMonday(date) {
  const d = new Date(date);
  const day = d.getDay();
  d.setDate(d.getDate() - (day === 0 ? 6 : day - 1));
  d.setHours(0, 0, 0, 0);
  return d;
}

function toZurich(utcString) {
  const parts = new Intl.DateTimeFormat('de-CH', {
    timeZone: 'Europe/Zurich',
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', hour12: false,
  }).formatToParts(new Date(utcString));
  const g = type => parseInt(parts.find(p => p.type === type).value, 10);
  return { year: g('year'), month: g('month'), day: g('day'), hour: g('hour'), minute: g('minute') };
}

function fmtTime(utcString) {
  const z = toZurich(utcString);
  return `${String(z.hour).padStart(2, '0')}:${String(z.minute).padStart(2, '0')}`;
}

function fmtDate(utcString) {
  const z = toZurich(utcString);
  return `${String(z.day).padStart(2, '0')}.${String(z.month).padStart(2, '0')}.${z.year}`;
}

function fmtWeekRange(monday) {
  const sunday = new Date(monday);
  sunday.setDate(sunday.getDate() + 6);
  const months = ['Januar','Februar','März','April','Mai','Juni',
                  'Juli','August','September','Oktober','November','Dezember'];
  const d1 = `${monday.getDate()}. ${months[monday.getMonth()]}`;
  const d2 = `${sunday.getDate()}. ${months[sunday.getMonth()]} ${sunday.getFullYear()}`;
  return `${d1} – ${d2}`;
}

function statusLabel(status) {
  return { Provisional: 'Provisorisch', Confirmed: 'Bestätigt', Cancelled: 'Storniert' }[status] ?? status;
}

function statusClass(slot) {
  if (slot.isRecurringSlot) return 'recurring';
  return (slot.status ?? '').toLowerCase();
}

// ── Komponente ────────────────────────────────────────────────────────────────

class ReservationenView extends LitElement {

  static styles = css`
    *, *::before, *::after { box-sizing: border-box; }

    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      font-family: 'Lato', sans-serif;
      font-size: 14px;
      color: #1a1a1a;
      background: #fff;
    }

    /* ── Toolbar ── */
    .toolbar {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 10px 16px;
      border-bottom: 1px solid #e0e0e0;
      flex-shrink: 0;
      background: #fff;
    }
    .week-label { font-size: 15px; font-weight: 600; flex: 1; }
    .btn {
      background: #f3f3f3;
      border: 1px solid #d0d0d0;
      border-radius: 4px;
      padding: 5px 12px;
      cursor: pointer;
      font-size: 14px;
      line-height: 1;
      white-space: nowrap;
    }
    .btn:hover { background: #e5e5e5; }
    .btn:disabled { opacity: 0.5; cursor: default; }

    /* ── Body ── */
    .body {
      display: flex;
      flex: 1;
      overflow: hidden;
    }

    /* ── Kalender ── */
    .cal-col {
      flex: 1;
      overflow: auto;
    }
    .cal-sticky-header {
      display: flex;
      position: sticky;
      top: 0;
      z-index: 10;
      background: #fff;
      border-bottom: 2px solid #d0d0d0;
    }
    .cal-time-header { width: ${TIME_W}px; flex-shrink: 0; }
    .cal-day-header {
      flex: 1;
      text-align: center;
      padding: 6px 2px;
      font-size: 12px;
      font-weight: 700;
      border-left: 1px solid #e8e8e8;
    }
    .cal-day-header.today { color: #c0392b; }

    .cal-inner { display: flex; }

    .time-col {
      width: ${TIME_W}px;
      flex-shrink: 0;
      position: relative;
    }
    .time-tick {
      position: absolute;
      right: 6px;
      font-size: 10px;
      color: #aaa;
      line-height: 1;
      transform: translateY(-50%);
    }

    .day-col {
      flex: 1;
      position: relative;
      border-left: 1px solid #e8e8e8;
    }
    .grid-line {
      position: absolute;
      left: 0; right: 0;
      border-bottom: 1px solid;
    }
    .grid-line.full  { border-color: #e0e0e0; }
    .grid-line.half  { border-color: #f0f0f0; }
    .day-col.past { background: #fafafa; }

    .cal-booking {
      position: absolute;
      left: 2px; right: 2px;
      border-radius: 3px;
      padding: 2px 5px;
      font-size: 11px;
      overflow: hidden;
      cursor: pointer;
      z-index: 2;
      border-left: 3px solid transparent;
      transition: filter 0.1s;
    }
    .cal-booking:hover { filter: brightness(0.9); }
    .cal-booking.selected { outline: 2px solid #1a1a1a; outline-offset: 1px; }
    .cal-booking.provisional { background: #fff3cd; border-color: #e6a817; color: #7a5000; }
    .cal-booking.confirmed   { background: #d4edda; border-color: #28a745; color: #155724; }
    .cal-booking.recurring   { background: #ebebeb; border-color: #999;    color: #444; }

    .bk-title { font-weight: 700; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .bk-time  { font-size: 10px; opacity: 0.8; }

    /* ── Detail-Panel ── */
    .detail-panel {
      width: 280px;
      flex-shrink: 0;
      border-left: 1px solid #e0e0e0;
      display: flex;
      flex-direction: column;
      overflow-y: auto;
    }
    .detail-empty {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #bbb;
      font-size: 13px;
      padding: 2rem;
      text-align: center;
    }
    .detail-loading {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #aaa;
      font-size: 13px;
    }
    .detail-header {
      padding: 14px 16px 10px;
      border-bottom: 1px solid #eee;
    }
    .detail-header h3 { margin: 0 0 6px; font-size: 14px; }
    .status-badge {
      display: inline-block;
      padding: 2px 9px;
      border-radius: 10px;
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0.03em;
    }
    .status-badge.provisional { background: #fff3cd; color: #7a5000; }
    .status-badge.confirmed   { background: #d4edda; color: #155724; }
    .status-badge.cancelled   { background: #f8d7da; color: #721c24; }
    .status-badge.recurring   { background: #ebebeb; color: #444; }

    .detail-section {
      padding: 10px 16px;
      border-bottom: 1px solid #f0f0f0;
    }
    .detail-section h4 {
      margin: 0 0 6px;
      font-size: 10px;
      text-transform: uppercase;
      letter-spacing: 0.07em;
      color: #999;
    }
    .detail-row {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      font-size: 12px;
      margin-bottom: 3px;
      gap: 8px;
    }
    .detail-row .lbl { color: #888; flex-shrink: 0; }
    .detail-row .val { font-weight: 600; text-align: right; }

    .detail-actions {
      padding: 12px 16px;
      display: flex;
      flex-direction: column;
      gap: 7px;
    }
    .btn-confirm {
      background: #28a745; color: #fff;
      border: none; border-radius: 4px;
      padding: 9px; font-size: 13px; font-weight: 700;
      cursor: pointer; width: 100%;
    }
    .btn-confirm:hover:not(:disabled) { background: #218838; }
    .btn-reject {
      background: #dc3545; color: #fff;
      border: none; border-radius: 4px;
      padding: 9px; font-size: 13px; font-weight: 700;
      cursor: pointer; width: 100%;
    }
    .btn-reject:hover:not(:disabled) { background: #c82333; }
    .btn-cancel-bk {
      background: transparent; color: #666;
      border: 1px solid #ccc; border-radius: 4px;
      padding: 9px; font-size: 12px;
      cursor: pointer; width: 100%;
    }
    .btn-cancel-bk:hover:not(:disabled) { background: #f5f5f5; }
    .btn-confirm:disabled,
    .btn-reject:disabled,
    .btn-cancel-bk:disabled { opacity: 0.55; cursor: default; }
    .action-error   { color: #dc3545; font-size: 12px; text-align: center; }
    .action-success { color: #28a745; font-size: 12px; text-align: center; }

    /* ── Ablehnen-Dialog ── */
    .reject-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.55);
      z-index: 9000;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .reject-dialog {
      background: #fff;
      border-radius: 8px;
      padding: 24px;
      width: 400px;
      max-width: 90vw;
      box-shadow: 0 12px 40px rgba(0,0,0,0.3);
    }
    .reject-dialog h3 { margin: 0 0 14px; font-size: 16px; }
    .reject-dialog textarea {
      width: 100%;
      border: 1px solid #ccc;
      border-radius: 4px;
      padding: 8px;
      font-size: 13px;
      font-family: inherit;
      resize: vertical;
      min-height: 80px;
    }
    .reject-dialog textarea:focus { outline: 2px solid #dc3545; border-color: transparent; }
    .reject-dialog-btns {
      display: flex;
      gap: 8px;
      justify-content: flex-end;
      margin-top: 14px;
    }

    /* ── Legende ── */
    .legend {
      display: flex;
      gap: 1.2rem;
      padding: 7px 16px;
      border-top: 1px solid #e0e0e0;
      flex-shrink: 0;
      font-size: 12px;
      color: #555;
      background: #fafafa;
    }
    .legend-item { display: flex; align-items: center; gap: 5px; }
    .legend-dot {
      width: 10px; height: 10px;
      border-radius: 2px; flex-shrink: 0;
    }
    .legend-dot.provisional { background: #e6a817; }
    .legend-dot.confirmed   { background: #28a745; }
    .legend-dot.recurring   { background: #999; }
  `;

  static properties = {
    _monday:        { state: true },
    _slots:         { state: true },
    _config:        { state: true },
    _loading:       { state: true },
    _selected:      { state: true },  // Detail-Objekt von /api/admin/reservierungen/{id}
    _detailLoading: { state: true },
    _actionLoading: { state: true },
    _actionError:   { state: true },
    _actionSuccess: { state: true },
    _showReject:    { state: true },
    _rejectReason:  { state: true },
  };

  constructor() {
    super();
    this._monday        = getMonday(new Date());
    this._slots         = [];
    this._config        = { oeffnungVon: 7, oeffnungBis: 22 };
    this._loading       = false;
    this._selected      = null;
    this._detailLoading = false;
    this._actionLoading = false;
    this._actionError   = null;
    this._actionSuccess = null;
    this._showReject    = false;
    this._rejectReason  = '';
  }

  connectedCallback() {
    super.connectedCallback();
    this._loadConfig();
  }

  // ── Daten laden ──────────────────────────────────────────────────────────────

  async _loadConfig() {
    try {
      const r = await fetch('/api/reservierung/konfiguration');
      if (r.ok) this._config = await r.json();
    } catch { /* Fallback-Werte bleiben */ }
    await this._loadWeek();
  }

  async _loadWeek() {
    this._loading = true;
    const von = this._monday.toISOString().slice(0, 10);
    try {
      const r = await fetch(`/api/reservierung/wochen-slots?von=${von}`);
      this._slots = r.ok ? await r.json() : [];
    } catch { this._slots = []; }
    this._loading = false;
  }

  async _selectSlot(id) {
    this._selected      = null;
    this._actionError   = null;
    this._actionSuccess = null;
    this._detailLoading = true;
    try {
      const r = await fetch(`/api/admin/reservierungen/${id}`);
      this._selected = r.ok ? await r.json() : null;
    } catch { /* Panel bleibt leer */ }
    this._detailLoading = false;
  }

  // ── Navigation ───────────────────────────────────────────────────────────────

  _prevWeek() {
    const d = new Date(this._monday);
    d.setDate(d.getDate() - 7);
    this._monday = d;
    this._selected = null;
    this._loadWeek();
  }

  _nextWeek() {
    const d = new Date(this._monday);
    d.setDate(d.getDate() + 7);
    this._monday = d;
    this._selected = null;
    this._loadWeek();
  }

  _goToday() {
    this._monday = getMonday(new Date());
    this._selected = null;
    this._loadWeek();
  }

  // ── Aktionen ─────────────────────────────────────────────────────────────────

  async _confirm() {
    if (!this._selected || this._actionLoading) return;
    this._actionLoading = true;
    this._actionError   = null;
    this._actionSuccess = null;
    try {
      const r = await fetch(`/api/admin/reservierungen/${this._selected.id}/bestaetigen`, { method: 'POST' });
      if (r.ok) {
        this._actionSuccess = 'Buchung bestätigt.';
        this._selected = { ...this._selected, status: 'Confirmed' };
        await this._loadWeek();
      } else {
        const e = await r.json();
        this._actionError = e.error ?? 'Fehler beim Bestätigen.';
      }
    } catch { this._actionError = 'Netzwerkfehler.'; }
    this._actionLoading = false;
  }

  _openRejectDialog() {
    this._rejectReason = '';
    this._showReject   = true;
  }

  async _submitReject() {
    if (!this._selected || !this._rejectReason.trim()) return;
    this._showReject    = false;
    this._actionLoading = true;
    this._actionError   = null;
    this._actionSuccess = null;
    try {
      const r = await fetch(`/api/admin/reservierungen/${this._selected.id}/ablehnen`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ grund: this._rejectReason.trim() }),
      });
      if (r.ok) {
        this._actionSuccess = 'Buchung abgelehnt.';
        this._selected = { ...this._selected, status: 'Cancelled' };
        await this._loadWeek();
      } else {
        const e = await r.json();
        this._actionError = e.error ?? 'Fehler beim Ablehnen.';
      }
    } catch { this._actionError = 'Netzwerkfehler.'; }
    this._actionLoading = false;
  }

  async _cancelBooking() {
    if (!this._selected || this._actionLoading) return;
    if (!confirm('Buchung wirklich stornieren?')) return;
    this._actionLoading = true;
    this._actionError   = null;
    this._actionSuccess = null;
    try {
      const r = await fetch(`/api/admin/reservierungen/${this._selected.id}/abbrechen`, { method: 'POST' });
      if (r.ok) {
        this._actionSuccess = 'Buchung storniert.';
        this._selected = { ...this._selected, status: 'Cancelled' };
        await this._loadWeek();
      } else {
        const e = await r.json();
        this._actionError = e.error ?? 'Fehler beim Stornieren.';
      }
    } catch { this._actionError = 'Netzwerkfehler.'; }
    this._actionLoading = false;
  }

  // ── Render ───────────────────────────────────────────────────────────────────

  render() {
    return html`
      ${this._renderToolbar()}
      <div class="body">
        ${this._renderCalendar()}
        ${this._renderDetailPanel()}
      </div>
      ${this._renderLegend()}
      ${this._showReject ? this._renderRejectDialog() : ''}
    `;
  }

  _renderToolbar() {
    return html`
      <div class="toolbar">
        <button class="btn" @click=${this._prevWeek}>&#8249;</button>
        <button class="btn" @click=${this._nextWeek}>&#8250;</button>
        <span class="week-label">${fmtWeekRange(this._monday)}</span>
        <button class="btn" @click=${this._goToday}>Heute</button>
        <button class="btn" @click=${this._loadWeek} ?disabled=${this._loading}>
          ${this._loading ? 'Lädt…' : '↻'}
        </button>
      </div>
    `;
  }

  _renderCalendar() {
    const { oeffnungVon: startH, oeffnungBis: endH } = this._config;
    const totalBlocks  = (endH - startH) * 2;
    const totalHeightPx = totalBlocks * BLOCK_H;
    const now          = new Date();
    const todayStr     = now.toDateString();

    // Slots nach Wochentag gruppieren (0 = Mo, 6 = So)
    const slotsByDay = Array.from({ length: 7 }, () => []);
    for (const slot of this._slots) {
      const z = toZurich(slot.startUtc);
      const slotDate   = new Date(z.year, z.month - 1, z.day);
      const mondayDate = new Date(this._monday.getFullYear(), this._monday.getMonth(), this._monday.getDate());
      const dayIdx     = Math.round((slotDate - mondayDate) / 86_400_000);
      if (dayIdx >= 0 && dayIdx < 7) slotsByDay[dayIdx].push(slot);
    }

    return html`
      <div class="cal-col">

        <!-- Sticky Tag-Header -->
        <div class="cal-sticky-header">
          <div class="cal-time-header"></div>
          ${DAYS.map((label, i) => {
            const d = new Date(this._monday);
            d.setDate(d.getDate() + i);
            const isToday = d.toDateString() === todayStr;
            return html`
              <div class="cal-day-header ${isToday ? 'today' : ''}">
                ${label} ${d.getDate()}.
              </div>
            `;
          })}
        </div>

        <!-- Kalender-Raster -->
        <div class="cal-inner">

          <!-- Zeitspalte -->
          <div class="time-col" style="height:${totalHeightPx}px">
            ${Array.from({ length: totalBlocks + 1 }, (_, i) => {
              const totalMin = startH * 60 + i * BLOCK_MIN;
              const h = Math.floor(totalMin / 60);
              const m = totalMin % 60;
              if (m !== 0) return '';
              return html`
                <div class="time-tick" style="top:${i * BLOCK_H}px">
                  ${String(h).padStart(2, '0')}:00
                </div>
              `;
            })}
          </div>

          <!-- 7 Tages-Spalten -->
          ${Array.from({ length: 7 }, (_, dayIdx) => {
            const d = new Date(this._monday);
            d.setDate(d.getDate() + dayIdx);
            const isPast = d < new Date(now.getFullYear(), now.getMonth(), now.getDate());

            return html`
              <div class="day-col ${isPast ? 'past' : ''}" style="height:${totalHeightPx}px">

                <!-- Rasterlinien -->
                ${Array.from({ length: totalBlocks }, (_, i) => html`
                  <div class="grid-line ${i % 2 === 1 ? 'full' : 'half'}"
                       style="top:${(i + 1) * BLOCK_H}px"></div>
                `)}

                <!-- Buchungen -->
                ${slotsByDay[dayIdx].map(slot => {
                  const zs     = toZurich(slot.startUtc);
                  const ze     = toZurich(slot.endUtc);
                  const startM = zs.hour * 60 + zs.minute;
                  const endM   = ze.hour * 60 + ze.minute;
                  const openM  = startH * 60;
                  const top    = ((startM - openM) / BLOCK_MIN) * BLOCK_H;
                  const height = ((endM - startM)  / BLOCK_MIN) * BLOCK_H;
                  const sc     = statusClass(slot);
                  const isSel  = this._selected?.id === slot.id;

                  return html`
                    <div class="cal-booking ${sc} ${isSel ? 'selected' : ''}"
                         style="top:${top}px; height:${height}px"
                         @click=${() => this._selectSlot(slot.id)}>
                      <div class="bk-title">${slot.eventType ?? statusLabel(slot.status)}</div>
                      <div class="bk-time">${fmtTime(slot.startUtc)}–${fmtTime(slot.endUtc)}</div>
                    </div>
                  `;
                })}
              </div>
            `;
          })}

        </div>
      </div>
    `;
  }

  _renderDetailPanel() {
    if (this._detailLoading) {
      return html`<div class="detail-panel"><div class="detail-loading">Lädt…</div></div>`;
    }
    if (!this._selected) {
      return html`
        <div class="detail-panel">
          <div class="detail-empty">Buchung im Kalender anklicken, um Details zu sehen.</div>
        </div>
      `;
    }

    const s  = this._selected;
    const sc = s.isRecurringSlot ? 'recurring' : (s.status ?? '').toLowerCase();
    const m  = s.mitglied;

    return html`
      <div class="detail-panel">

        <div class="detail-header">
          <h3>${s.anlass ?? 'Buchung #' + s.id}</h3>
          <span class="status-badge ${sc}">${statusLabel(s.status)}</span>
        </div>

        <div class="detail-section">
          <h4>Termin</h4>
          <div class="detail-row">
            <span class="lbl">Datum</span>
            <span class="val">${fmtDate(s.startUtc)}</span>
          </div>
          <div class="detail-row">
            <span class="lbl">Zeit</span>
            <span class="val">${fmtTime(s.startUtc)} – ${fmtTime(s.endUtc)}</span>
          </div>
          ${s.preisProBlock != null ? html`
            <div class="detail-row">
              <span class="lbl">Preis/Block</span>
              <span class="val">CHF ${s.preisProBlock.toFixed(2)}</span>
            </div>
          ` : ''}
          ${s.notizen ? html`
            <div class="detail-row" style="flex-direction:column; gap:2px">
              <span class="lbl">Bemerkungen</span>
              <span class="val" style="text-align:left">${s.notizen}</span>
            </div>
          ` : ''}
        </div>

        ${m ? html`
          <div class="detail-section">
            <h4>Mieter</h4>
            <div class="detail-row">
              <span class="lbl">Name</span>
              <span class="val">${m.name}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">E-Mail</span>
              <span class="val" style="word-break:break-all">${m.email}</span>
            </div>
            ${m.telefon ? html`
              <div class="detail-row">
                <span class="lbl">Telefon</span>
                <span class="val">${m.telefon}</span>
              </div>
            ` : ''}
          </div>
          <div class="detail-section">
            <h4>Rechnungsadresse</h4>
            <div class="detail-row">
              <span class="lbl">Name</span>
              <span class="val">${m.rechnungsName}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">Adresse</span>
              <span class="val">${m.strasse}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">Ort</span>
              <span class="val">${m.plz} ${m.ort}</span>
            </div>
          </div>
        ` : ''}

        <!-- Aktionen -->
        ${s.status === 'Provisional' ? html`
          <div class="detail-actions">
            ${this._actionSuccess ? html`<p class="action-success">${this._actionSuccess}</p>` : ''}
            ${this._actionError   ? html`<p class="action-error">${this._actionError}</p>` : ''}
            <button class="btn-confirm" ?disabled=${this._actionLoading} @click=${this._confirm}>
              ${this._actionLoading ? 'Wird verarbeitet…' : '✓ Bestätigen'}
            </button>
            <button class="btn-reject" ?disabled=${this._actionLoading} @click=${this._openRejectDialog}>
              ✕ Ablehnen
            </button>
          </div>
        ` : s.status === 'Confirmed' ? html`
          <div class="detail-actions">
            ${this._actionSuccess ? html`<p class="action-success">${this._actionSuccess}</p>` : ''}
            ${this._actionError   ? html`<p class="action-error">${this._actionError}</p>` : ''}
            <button class="btn-cancel-bk" ?disabled=${this._actionLoading} @click=${this._cancelBooking}>
              ${this._actionLoading ? 'Wird verarbeitet…' : 'Stornieren'}
            </button>
          </div>
        ` : ''}

      </div>
    `;
  }

  _renderRejectDialog() {
    return html`
      <div class="reject-overlay" @click=${e => e.target === e.currentTarget && (this._showReject = false)}>
        <div class="reject-dialog">
          <h3>Buchung ablehnen</h3>
          <textarea
            placeholder="Begründung für die Ablehnung (wird dem Mieter per E-Mail mitgeteilt)…"
            .value=${this._rejectReason}
            @input=${e => this._rejectReason = e.target.value}
          ></textarea>
          <div class="reject-dialog-btns">
            <button class="btn" @click=${() => this._showReject = false}>Abbrechen</button>
            <button class="btn-reject"
                    ?disabled=${!this._rejectReason.trim()}
                    @click=${this._submitReject}>
              Ablehnen &amp; E-Mail senden
            </button>
          </div>
        </div>
      </div>
    `;
  }

  _renderLegend() {
    return html`
      <div class="legend">
        <div class="legend-item"><div class="legend-dot provisional"></div> Provisorisch</div>
        <div class="legend-item"><div class="legend-dot confirmed"></div> Bestätigt</div>
        <div class="legend-item"><div class="legend-dot recurring"></div> Dauerbelegung</div>
      </div>
    `;
  }
}

customElements.define('reservationen-view', ReservationenView);
