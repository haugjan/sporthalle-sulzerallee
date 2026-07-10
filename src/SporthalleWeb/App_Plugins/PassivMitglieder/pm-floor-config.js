import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';

const COLS = 40;
const ROWS = 25;

const DEFAULT_REGION = { x0: 0.14515, y0: 0.05714, x1: 0.91906, y1: 0.93968 };
const DEFAULT_SPECIAL = [
  { from: 408, to: 611, label: 'Torraum' },
  { from: 434, to: 637, label: 'Torraum' },
  { from: 502, to: 502, label: 'Mittelpunkt' },
];

const colOf = (fn) => (fn - 1) % COLS;
const rowOf = (fn) => Math.floor((fn - 1) / COLS);
const fieldNo = (col, row) => row * COLS + col + 1;

function expandArea(area) {
  const c0 = Math.min(colOf(area.from), colOf(area.to));
  const c1 = Math.max(colOf(area.from), colOf(area.to));
  const r0 = Math.min(rowOf(area.from), rowOf(area.to));
  const r1 = Math.max(rowOf(area.from), rowOf(area.to));
  const label = (area.label && area.label.trim()) || 'Spezialfeld';
  const out = {};
  for (let r = r0; r <= r1; r++) {
    for (let c = c0; c <= c1; c++) out[fieldNo(c, r)] = label;
  }
  return out;
}

class PmFloorConfigElement extends LitElement {
  static properties = {
    value: { type: Object },
    _bgUrl: { state: true },
    _mode: { state: true },
    _region: { state: true },
    _special: { state: true },
    _label: { state: true },
    _drag: { state: true },
  };

  constructor() {
    super();
    this._bgUrl = '/img/hallenboden.png';
    this._mode = 'region';
    this._label = 'Spezialfeld';
    this._region = { ...DEFAULT_REGION };
    this._special = {};
    this._drag = null;
    this._selfUpdate = false;
    this._loaded = false;
  }

  connectedCallback() {
    super.connectedCallback();
    fetch('/passivmitglieder/hallenboden/config', { credentials: 'include' })
      .then((r) => (r.ok ? r.json() : null))
      .then((d) => { if (d && d.backgroundUrl) this._bgUrl = d.backgroundUrl; })
      .catch(() => {});
  }

  firstUpdated() {
    if (!this._loaded) this.#loadFromValue();
  }

  willUpdate(changed) {
    if (changed.has('value') && !this._selfUpdate) this.#loadFromValue();
    this._selfUpdate = false;
  }

  #loadFromValue() {
    let model = this.value;
    if (typeof model === 'string') {
      const t = model.trim();
      if (!t) { model = null; } else { try { model = JSON.parse(t); } catch { model = null; } }
    }
    if (model && typeof model === 'object') {
      const r = model.region;
      this._region = r
        ? {
            x0: this.#num(r.x0, DEFAULT_REGION.x0),
            y0: this.#num(r.y0, DEFAULT_REGION.y0),
            x1: this.#num(r.x1, DEFAULT_REGION.x1),
            y1: this.#num(r.y1, DEFAULT_REGION.y1),
          }
        : { ...DEFAULT_REGION };
      const special = {};
      if (Array.isArray(model.special)) {
        for (const a of model.special) {
          if (a && Number.isFinite(a.from)) Object.assign(special, expandArea({ from: a.from, to: a.to ?? a.from, label: a.label }));
        }
      }
      this._special = special;
    } else {
      this._region = { ...DEFAULT_REGION };
      this._special = DEFAULT_SPECIAL.reduce((acc, a) => Object.assign(acc, expandArea(a)), {});
    }
    this._loaded = true;
  }

  #num(v, fallback) { return typeof v === 'number' && isFinite(v) ? v : fallback; }

  #buildModel() {
    const special = Object.keys(this._special).map((k) => {
      const fn = Number(k);
      return { from: fn, to: fn, label: this._special[k] || 'Spezialfeld' };
    });
    return { region: this._region, special };
  }

  #emit() {
    this._selfUpdate = true;
    this.value = this.#buildModel();
    this.dispatchEvent(new CustomEvent('property-value-change', { bubbles: true, composed: true }));
    this.dispatchEvent(new CustomEvent('change', { bubbles: true, composed: true }));
  }

  #fractionFromEvent(e) {
    const rect = this.renderRoot.querySelector('.pm-stage').getBoundingClientRect();
    let fx = (e.clientX - rect.left) / rect.width;
    let fy = (e.clientY - rect.top) / rect.height;
    fx = Math.min(1, Math.max(0, fx));
    fy = Math.min(1, Math.max(0, fy));
    return { fx, fy };
  }

  #onPointerDown(e) {
    if (this._mode !== 'region') return;
    e.preventDefault();
    const p = this.#fractionFromEvent(e);
    this._drag = { x0: p.fx, y0: p.fy, x1: p.fx, y1: p.fy };
    this.renderRoot.querySelector('.pm-stage').setPointerCapture?.(e.pointerId);
  }

  #onPointerMove(e) {
    if (this._mode !== 'region' || !this._drag) return;
    const p = this.#fractionFromEvent(e);
    this._drag = { ...this._drag, x1: p.fx, y1: p.fy };
  }

  #onPointerUp() {
    if (this._mode !== 'region' || !this._drag) return;
    const d = this._drag;
    this._drag = null;
    const x0 = Math.min(d.x0, d.x1), x1 = Math.max(d.x0, d.x1);
    const y0 = Math.min(d.y0, d.y1), y1 = Math.max(d.y0, d.y1);
    if (x1 - x0 < 0.02 || y1 - y0 < 0.02) return;
    this._region = { x0, y0, x1, y1 };
    this.#emit();
  }

  #onCellClick(fn) {
    if (this._mode !== 'special') return;
    const next = { ...this._special };
    if (next[fn]) delete next[fn];
    else next[fn] = (this._label && this._label.trim()) || 'Spezialfeld';
    this._special = next;
    this.#emit();
  }

  #reset() {
    this._region = { ...DEFAULT_REGION };
    this._special = DEFAULT_SPECIAL.reduce((acc, a) => Object.assign(acc, expandArea(a)), {});
    this.#emit();
  }

  #clearSpecial() {
    this._special = {};
    this.#emit();
  }

  #renderGrid() {
    const reg = this._region;
    const cw = (reg.x1 - reg.x0) / COLS;
    const ch = (reg.y1 - reg.y0) / ROWS;
    const cells = [];
    for (let row = 0; row < ROWS; row++) {
      for (let col = 0; col < COLS; col++) {
        const fn = fieldNo(col, row);
        const x = reg.x0 + col * cw;
        const y = reg.y0 + row * ch;
        const isSpecial = !!this._special[fn];
        cells.push(html`<rect
          x=${x} y=${y} width=${cw} height=${ch}
          vector-effect="non-scaling-stroke"
          class="cell ${isSpecial ? 'cell--special' : ''} ${this._mode === 'special' ? 'cell--clickable' : ''}"
          @click=${() => this.#onCellClick(fn)}></rect>`);
      }
    }
    return cells;
  }

  #renderRegionOutline() {
    const r = this._drag || this._region;
    const x0 = Math.min(r.x0, r.x1), y0 = Math.min(r.y0, r.y1);
    const w = Math.abs(r.x1 - r.x0), h = Math.abs(r.y1 - r.y0);
    return html`<rect x=${x0} y=${y0} width=${w} height=${h}
      vector-effect="non-scaling-stroke" class="region"></rect>`;
  }

  render() {
    const specialCount = Object.keys(this._special).length;
    return html`
      <div class="toolbar">
        <div class="modes">
          <button type="button" class="${this._mode === 'region' ? 'active' : ''}"
                  @click=${() => (this._mode = 'region')}>Rasterbereich ziehen</button>
          <button type="button" class="${this._mode === 'special' ? 'active' : ''}"
                  @click=${() => (this._mode = 'special')}>Spezialfelder klicken</button>
        </div>
        <label class="lbl">Label:
          <input type="text" .value=${this._label}
                 @input=${(e) => (this._label = e.target.value)} />
        </label>
        <span class="count">${specialCount} Spezialfeld(er)</span>
        <button type="button" @click=${() => this.#clearSpecial()}>Spezialfelder leeren</button>
        <button type="button" @click=${() => this.#reset()}>Auf Standard zurücksetzen</button>
      </div>
      <div class="hint">
        ${this._mode === 'region'
          ? 'Ziehe ein Rechteck über den blauen Spielfeldbereich. Das Raster wird nur dort eingeblendet.'
          : 'Klicke einzelne Felder an, um sie als Spezialfeld zu markieren (erneut klicken zum Entfernen).'}
      </div>
      <div class="pm-stage stage ${this._mode === 'region' ? 'stage--region' : ''}"
           @pointerdown=${this.#onPointerDown}
           @pointermove=${this.#onPointerMove}
           @pointerup=${this.#onPointerUp}>
        <img src=${this._bgUrl} alt="" draggable="false" />
        <svg viewBox="0 0 1 1" preserveAspectRatio="none">
          ${this.#renderGrid()}
          ${this.#renderRegionOutline()}
        </svg>
      </div>
    `;
  }

  static styles = css`
    :host { display: block; }
    .toolbar { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; margin-bottom: 8px; }
    .toolbar button { padding: 6px 10px; cursor: pointer; border: 1px solid var(--uui-color-border, #ccc);
      background: var(--uui-color-surface, #fff); border-radius: 4px; }
    .toolbar button.active { background: var(--uui-color-selected, #3544b1); color: #fff; border-color: transparent; }
    .modes { display: flex; gap: 4px; }
    .lbl { display: inline-flex; align-items: center; gap: 4px; font-size: 0.85rem; }
    .lbl input { padding: 4px 6px; border: 1px solid var(--uui-color-border, #ccc); border-radius: 4px; }
    .count { font-size: 0.85rem; opacity: 0.75; }
    .hint { font-size: 0.8rem; opacity: 0.7; margin-bottom: 6px; }
    .stage { position: relative; width: 100%; max-width: 900px; user-select: none; touch-action: none;
      border: 1px solid var(--uui-color-border, #ccc); border-radius: 4px; overflow: hidden; }
    .stage--region { cursor: crosshair; }
    .stage img { display: block; width: 100%; height: auto; }
    .stage svg { position: absolute; inset: 0; width: 100%; height: 100%; }
    .cell { fill: transparent; stroke: rgba(53,68,177,0.35); stroke-width: 1; }
    .cell--clickable { cursor: pointer; }
    .cell--clickable:hover { fill: rgba(53,68,177,0.2); }
    .cell--special { fill: rgba(201,162,39,0.45); stroke: rgba(201,162,39,0.9); }
    .region { fill: rgba(53,68,177,0.08); stroke: #3544b1; stroke-width: 2; stroke-dasharray: 4 3; }
  `;
}

customElements.define('pm-floor-config', PmFloorConfigElement);
export default PmFloorConfigElement;
