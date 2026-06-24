(function () {
  'use strict';

  // Konstanten — werden von /api/reservierung/konfiguration überschrieben
  var OPENING_HOUR_START = 7;
  var OPENING_HOUR_END = 23;
  var BLOCK_MINUTES = 30;
  var TOTAL_BLOCKS = (OPENING_HOUR_END - OPENING_HOUR_START) * (60 / BLOCK_MINUTES);
  var CELL_HEIGHT = 20;
  var CELL_GAP = 1;
  var CELL_STEP = CELL_HEIGHT + CELL_GAP;

  var DAYS_TO_SHOW = 7;
  var VORLAUFZEIT_TAGE = 3; // min days advance notice; overridden from API
  var BUCHUNGEN_MAX_TAGE = null; // max days ahead for public booking; null = no limit
  var currentMonday = getMonday(new Date());
  var lastSlots = [];
  var resizeTimer;

  // Drag-State
  var dragState = null;
  var selectionEl = null;
  var selectedSlot = null;
  var isDragging = false;

  // Turnstile CAPTCHA
  var _bmTsId = null;

  // Datepicker-State
  var dpCurrentMonth = null;
  var dpOpen = false;
  var DP_MONTH_NAMES = ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni',
                        'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];

  // ── Datum-Hilfsfunktionen ─────────────────────────────────────────────────

  function getMonday(d) {
    var date = new Date(d);
    date.setHours(0, 0, 0, 0);
    var day = date.getDay();
    date.setDate(date.getDate() - (day + 6) % 7);
    return date;
  }

  function addDays(d, n) {
    var date = new Date(d);
    date.setDate(date.getDate() + n);
    return date;
  }

  function toLocalDateStr(d) {
    var y = d.getFullYear();
    var m = String(d.getMonth() + 1).padStart(2, '0');
    var day = String(d.getDate()).padStart(2, '0');
    return y + '-' + m + '-' + day;
  }

  function checkMobile() {
    DAYS_TO_SHOW = window.innerWidth < 768 ? 3 : 7;
  }

  function formatWeekLabel(start) {
    var end = addDays(start, DAYS_TO_SHOW - 1);
    var opts = { day: '2-digit', month: '2-digit', year: 'numeric' };
    return start.toLocaleDateString('de-CH', opts) + ' – ' + end.toLocaleDateString('de-CH', opts);
  }

  function formatDayHeader(date) {
    var dayNames = ['So', 'Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa'];
    var d = String(date.getDate()).padStart(2, '0');
    var m = String(date.getMonth() + 1).padStart(2, '0');
    return dayNames[date.getDay()] + ' ' + d + '.' + m + '.';
  }

  function isToday(date) {
    var now = new Date();
    return date.getFullYear() === now.getFullYear() &&
      date.getMonth() === now.getMonth() &&
      date.getDate() === now.getDate();
  }

  function isPastDay(date) {
    var today = new Date();
    today.setHours(0, 0, 0, 0);
    return date < today;
  }

  function isShortNotice(date) {
    var today = new Date();
    today.setHours(0, 0, 0, 0);
    var diff = Math.round((date - today) / 86400000);
    return diff >= 0 && diff < VORLAUFZEIT_TAGE;
  }

  function isBeyondMaxDays(date) {
    if (!BUCHUNGEN_MAX_TAGE) return false;
    var today = new Date();
    today.setHours(0, 0, 0, 0);
    var cutoff = new Date(today);
    cutoff.setDate(cutoff.getDate() + BUCHUNGEN_MAX_TAGE);
    return date > cutoff;
  }

  function blockToTimeLabel(blockIdx) {
    var totalMin = OPENING_HOUR_START * 60 + blockIdx * BLOCK_MINUTES;
    var h = String(Math.floor(totalMin / 60)).padStart(2, '0');
    var m = String(totalMin % 60).padStart(2, '0');
    return h + ':' + m;
  }

  function blockToMinutes(blockIdx) {
    return OPENING_HOUR_START * 60 + blockIdx * BLOCK_MINUTES;
  }

  function minutesToTimeStr(totalMin) {
    var h = String(Math.floor(totalMin / 60)).padStart(2, '0');
    var m = String(totalMin % 60).padStart(2, '0');
    return h + ':' + m;
  }

  function fmtTime(h, m) {
    return String(h).padStart(2, '0') + ':' + String(m).padStart(2, '0');
  }

  function formatDateLong(date) {
    var dayNames = ['Sonntag', 'Montag', 'Dienstag', 'Mittwoch', 'Donnerstag', 'Freitag', 'Samstag'];
    var months = ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni',
                  'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];
    return dayNames[date.getDay()] + ', ' + date.getDate() + '. ' + months[date.getMonth()] + ' ' + date.getFullYear();
  }

  // ── UTC → Zürich ──────────────────────────────────────────────────────────

  function getZurichParts(utcDate) {
    var parts = new Intl.DateTimeFormat('de-CH', {
      timeZone: 'Europe/Zurich',
      year: 'numeric', month: 'numeric', day: 'numeric',
      hour: 'numeric', minute: 'numeric',
      hour12: false
    }).formatToParts(utcDate);
    var p = {};
    parts.forEach(function (part) {
      if (part.type !== 'literal') p[part.type] = parseInt(part.value, 10);
    });
    if (p.hour === 24) p.hour = 0;
    return p;
  }

  function localDateToUtcIso(date, totalMinutes) {
    var h = Math.floor(totalMinutes / 60);
    var m = totalMinutes % 60;
    // Erzeuge lokale Zeit als UTC-Basis, dann korrigiere um Zürich-Offset
    var probe = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), h, m, 0));
    var zurichParts = getZurichParts(probe);
    var offsetMin = (zurichParts.hour * 60 + zurichParts.minute) - (h * 60 + m);
    var utc = new Date(probe.getTime() - offsetMin * 60000);
    return utc.toISOString();
  }

  // ── Kalender-Grid-Layout berechnen ────────────────────────────────────────

  function computeGridLayout() {
    var grid = document.getElementById('calendar-grid');
    if (!grid) return null;
    var headerCells = grid.querySelectorAll('.cal-header-day');
    if (!headerCells.length) return null;
    var gridRect = grid.getBoundingClientRect();
    var cols = [];
    headerCells.forEach(function (h) {
      var r = h.getBoundingClientRect();
      cols.push({ left: r.left - gridRect.left, width: r.width });
    });
    var headerRect = headerCells[0].getBoundingClientRect();
    var contentTop = headerRect.bottom - gridRect.top + CELL_GAP;
    return { cols: cols, contentTop: contentTop, gridRect: gridRect };
  }

  // ── Kalender-Grid rendern ─────────────────────────────────────────────────

  function renderGrid(slots) {
    var grid = document.getElementById('calendar-grid');
    if (!grid) return;
    grid.innerHTML = '';
    grid.style.gridTemplateColumns = '48px repeat(' + DAYS_TO_SHOW + ', 1fr)';
    grid.style.minWidth = DAYS_TO_SHOW === 3 ? '280px' : '560px';
    selectionEl = null;

    var days = [];
    for (var i = 0; i < DAYS_TO_SHOW; i++) days.push(addDays(currentMonday, i));

    // Kopfzeile
    var timeCorner = document.createElement('div');
    timeCorner.className = 'cal-header-time';
    grid.appendChild(timeCorner);

    days.forEach(function (day) {
      var cell = document.createElement('div');
      cell.className = 'cal-header-day';
      if (isPastDay(day)) cell.classList.add('is-past');
      else if (isToday(day)) cell.classList.add('is-today');
      else if (isBeyondMaxDays(day)) cell.classList.add('is-beyond-cutoff');
      cell.textContent = formatDayHeader(day);
      grid.appendChild(cell);
    });

    // Zeitzeilen
    for (var b = 0; b < TOTAL_BLOCKS; b++) {
      var isHourStart = b % 2 === 0;
      var timeLabel = document.createElement('div');
      timeLabel.className = 'cal-time' + (isHourStart ? ' hour-start' : '');
      timeLabel.textContent = isHourStart ? blockToTimeLabel(b) : '';
      grid.appendChild(timeLabel);

      days.forEach(function (day) {
        var cell = document.createElement('div');
        cell.className = 'cal-cell' + (isHourStart ? ' hour-start' : '');
        if (isPastDay(day)) cell.classList.add('is-past');
        else if (isToday(day)) cell.classList.add('is-today');
        else if (isBeyondMaxDays(day)) cell.classList.add('is-beyond-cutoff');
        grid.appendChild(cell);
      });
    }

    requestAnimationFrame(function () {
      renderBookingOverlays(slots, days, grid);
    });
  }

  // ── Booking-Overlays ──────────────────────────────────────────────────────

  function renderBookingOverlays(slots, days, grid) {
    var old = grid.querySelectorAll('.booking-overlay');
    for (var i = 0; i < old.length; i++) old[i].remove();

    var layout = computeGridLayout();
    if (!layout) return;

    slots.forEach(function (slot) {
      var slotStartUtc = new Date(slot.startUtc);
      var slotEndUtc = new Date(slot.endUtc);
      var startZ = getZurichParts(slotStartUtc);
      var endZ = getZurichParts(slotEndUtc);

      var dayIdx = -1;
      for (var i = 0; i < days.length; i++) {
        var d = days[i];
        if (startZ.year === d.getFullYear() &&
            startZ.month === d.getMonth() + 1 &&
            startZ.day === d.getDate()) {
          dayIdx = i;
          break;
        }
      }
      if (dayIdx < 0 || dayIdx >= layout.cols.length) return;
      if (isPastDay(days[dayIdx])) return;

      var openStart = OPENING_HOUR_START * 60;
      var slotStartMin = startZ.hour * 60 + startZ.minute;
      var slotEndMin = endZ.hour * 60 + endZ.minute;
      var startBlock = Math.round((slotStartMin - openStart) / BLOCK_MINUTES);
      var endBlock = Math.round((slotEndMin - openStart) / BLOCK_MINUTES);
      startBlock = Math.max(0, startBlock);
      endBlock = Math.min(TOTAL_BLOCKS, endBlock);
      var numBlocks = endBlock - startBlock;
      if (numBlocks <= 0) return;

      var col = layout.cols[dayIdx];
      var top = layout.contentTop + startBlock * CELL_STEP;
      var height = numBlocks * CELL_STEP - CELL_GAP;
      var left = col.left + 2;
      var width = col.width - 4;

      var el = document.createElement('div');
      el.className = 'booking-overlay booking-overlay--' + getOverlayMod(slot);
      el.style.top = top + 'px';
      el.style.height = height + 'px';
      el.style.left = left + 'px';
      el.style.width = width + 'px';
      el.style.background = slot.type === 'Reserved' ? '#0078D4' : (slot.color || getDefaultColor(slot));

      if (height >= 24) {
        var label = document.createElement('div');
        label.className = 'booking-overlay__label';
        if (slot.type === 'Blocker') {
          var nbEl = document.createElement('span');
          nbEl.className = 'bol-title';
          nbEl.textContent = 'Nicht buchbar';
          label.appendChild(nbEl);
        }
        var timeEl = document.createElement('span');
        timeEl.className = 'bol-time';
        timeEl.textContent = fmtTime(startZ.hour, startZ.minute) + '–' + fmtTime(endZ.hour, endZ.minute);
        label.appendChild(timeEl);
        el.appendChild(label);
      }

      grid.appendChild(el);
    });
  }

  function getOverlayMod(slot) {
    if (slot.type === 'Booked' || slot.type === 'Blocker' || slot.type === 'Recurring') return 'confirmed';
    return 'provisional'; // Reserved
  }

  function getDefaultColor(slot) {
    if (slot.type === 'Reserved') return '#0078D4';
    return '#444';
  }

  // ── Drag-to-Select ────────────────────────────────────────────────────────

  function blockFromY(y, contentTop) {
    var raw = (y - contentTop) / CELL_STEP;
    return Math.max(0, Math.min(TOTAL_BLOCKS - 1, Math.floor(raw)));
  }

  function dayIdxFromX(x, cols) {
    for (var i = cols.length - 1; i >= 0; i--) {
      if (x >= cols[i].left) return i;
    }
    return 0;
  }

  function isBlockOccupied(dayIdx, block, days) {
    var day = days[dayIdx];
    var blockStartMin = blockToMinutes(block);
    var blockEndMin = blockStartMin + BLOCK_MINUTES;

    for (var i = 0; i < lastSlots.length; i++) {
      var slot = lastSlots[i];
      var startZ = getZurichParts(new Date(slot.startUtc));
      var endZ = getZurichParts(new Date(slot.endUtc));

      if (startZ.year !== day.getFullYear() ||
          startZ.month !== day.getMonth() + 1 ||
          startZ.day !== day.getDate()) continue;

      var slotStartMin = startZ.hour * 60 + startZ.minute;
      var slotEndMin = endZ.hour * 60 + endZ.minute;

      if (blockStartMin < slotEndMin && blockEndMin > slotStartMin) return true;
    }
    return false;
  }

  function renderSelectionOverlay(dayIdx, startBlock, endBlock, layout) {
    var grid = document.getElementById('calendar-grid');
    if (!grid || !layout) return;

    if (selectionEl) selectionEl.remove();
    selectionEl = null;

    if (endBlock <= startBlock) return;

    var col = layout.cols[dayIdx];
    if (!col) return;

    var top = layout.contentTop + startBlock * CELL_STEP;
    var height = (endBlock - startBlock) * CELL_STEP - CELL_GAP;

    var el = document.createElement('div');
    el.className = 'cal-selection';
    el.style.top = top + 'px';
    el.style.height = height + 'px';
    el.style.left = (col.left + 2) + 'px';
    el.style.width = (col.width - 4) + 'px';

    var durationMin = (endBlock - startBlock) * BLOCK_MINUTES;
    var durationText = durationMin >= 60
      ? (durationMin % 60 === 0
          ? (durationMin / 60) + ' h'
          : Math.floor(durationMin / 60) + ' h ' + (durationMin % 60) + ' min')
      : durationMin + ' min';

    el.innerHTML = '<span class="cal-selection__label">' + durationText + '</span>';
    grid.appendChild(el);
    selectionEl = el;
  }

  function clearSelectionOverlay() {
    if (selectionEl) {
      selectionEl.remove();
      selectionEl = null;
    }
    selectedSlot = null;
    var detail = document.getElementById('slot-panel-detail');
    var hint = document.getElementById('slot-panel-hint');
    if (detail) detail.hidden = true;
    if (hint) hint.hidden = false;
  }

  function getClientXY(e) {
    if (e.touches && e.touches.length > 0) {
      return { x: e.touches[0].clientX, y: e.touches[0].clientY };
    }
    if (e.changedTouches && e.changedTouches.length > 0) {
      return { x: e.changedTouches[0].clientX, y: e.changedTouches[0].clientY };
    }
    return { x: e.clientX, y: e.clientY };
  }

  function onDragStart(e) {
    var target = e.target;
    if (!target.classList.contains('cal-cell')) return;
    if (target.classList.contains('is-past')) return;

    var layout = computeGridLayout();
    if (!layout) return;

    var xy = getClientXY(e);
    var relX = xy.x - layout.gridRect.left;
    var relY = xy.y - layout.gridRect.top;

    var dayIdx = dayIdxFromX(relX, layout.cols);
    var days = [];
    for (var i = 0; i < DAYS_TO_SHOW; i++) days.push(addDays(currentMonday, i));

    if (dayIdx < 0 || dayIdx >= days.length) return;
    if (isPastDay(days[dayIdx])) return;

    var startBlock = blockFromY(relY, layout.contentTop);
    if (isBlockOccupied(dayIdx, startBlock, days)) return;

    e.preventDefault();
    isDragging = true;
    dragState = {
      dayIdx: dayIdx,
      startBlock: startBlock,
      currentEndBlock: startBlock + 2,
      days: days,
      layout: layout
    };

    renderSelectionOverlay(dayIdx, startBlock, startBlock + 2, layout);
  }

  function onDragMove(e) {
    if (!isDragging || !dragState) return;
    e.preventDefault();

    var layout = computeGridLayout();
    if (!layout) return;

    var xy = getClientXY(e);
    var relY = xy.y - layout.gridRect.top;
    var rawBlock = blockFromY(relY, layout.contentTop) + 1;
    var endBlock = Math.min(TOTAL_BLOCKS, Math.max(dragState.startBlock + 2, Math.min(TOTAL_BLOCKS, rawBlock)));

    dragState.currentEndBlock = endBlock;
    dragState.layout = layout;
    renderSelectionOverlay(dragState.dayIdx, dragState.startBlock, endBlock, layout);
  }

  function onDragEnd(e) {
    if (!isDragging || !dragState) return;
    isDragging = false;

    var startBlock = dragState.startBlock;
    var endBlock = dragState.currentEndBlock;
    var dayIdx = dragState.dayIdx;
    var day = dragState.days[dayIdx];
    var layout = dragState.layout;

    // Mindestdauer 60 min
    if (endBlock - startBlock < 2) endBlock = startBlock + 2;

    // Abschneiden an Schliesszeit
    endBlock = Math.min(TOTAL_BLOCKS, endBlock);

    // Abschneiden an belegten Blöcken
    for (var b = startBlock; b < endBlock; b++) {
      if (isBlockOccupied(dayIdx, b, dragState.days)) {
        endBlock = b;
        break;
      }
    }

    dragState = null;

    // Mindestdauer nach Clamp erneut prüfen
    if (endBlock - startBlock < 2) {
      clearSelectionOverlay();
      return;
    }

    var startMin = blockToMinutes(startBlock);
    var endMin = blockToMinutes(endBlock);

    selectedSlot = {
      day: day,
      dayIdx: dayIdx,
      startBlock: startBlock,
      endBlock: endBlock,
      startMin: startMin,
      endMin: endMin,
      startUtcIso: localDateToUtcIso(day, startMin),
      endUtcIso: localDateToUtcIso(day, endMin)
    };

    var freshLayout = computeGridLayout();
    renderSelectionOverlay(dayIdx, startBlock, endBlock, freshLayout || layout);
    showSlotPanel(selectedSlot);
  }

  // ── Slot-Panel ────────────────────────────────────────────────────────────

  function showSlotPanel(slot) {
    var hint = document.getElementById('slot-panel-hint');
    var detail = document.getElementById('slot-panel-detail');
    var dateLabel = document.getElementById('slot-date-label');
    var vonEl = document.getElementById('slot-von');
    var bisEl = document.getElementById('slot-bis');
    var buchBtn = document.getElementById('btn-jetzt-buchen');
    var notice = document.getElementById('slot-short-notice');

    if (!detail) return;

    if (hint) hint.hidden = true;
    detail.hidden = false;

    if (dateLabel) dateLabel.textContent = formatDateLong(slot.day);
    if (vonEl) vonEl.textContent = minutesToTimeStr(slot.startMin) + ' Uhr';
    if (bisEl) bisEl.textContent = minutesToTimeStr(slot.endMin) + ' Uhr';

    var shortNotice = isShortNotice(slot.day);
    var beyondCutoff = isBeyondMaxDays(slot.day);
    var hideBookBtn = shortNotice || beyondCutoff;
    if (buchBtn) buchBtn.hidden = hideBookBtn;
    if (notice) notice.hidden = !shortNotice;
    var beyondNotice = document.getElementById('slot-beyond-cutoff');
    if (beyondNotice) beyondNotice.hidden = !beyondCutoff;
    var noticeText = document.getElementById('slot-short-notice-text');
    if (noticeText && VORLAUFZEIT_TAGE > 0)
      noticeText.textContent = 'Für Reservationen weniger als ' + VORLAUFZEIT_TAGE + ' Tage im Voraus bitte direkt per E-Mail melden:';

    // Panel ins Sichtfeld scrollen (auf Mobilgeräten scrollt es nach unten,
    // auf Desktop wird es sichtbar gemacht falls es ausserhalb des Viewports ist)
    var panel = document.getElementById('slot-panel');
    if (panel) {
      setTimeout(function () {
        var rect = panel.getBoundingClientRect();
        if (rect.top < 60 || rect.bottom > window.innerHeight) {
          panel.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      }, 50);
    }
  }


  // ── Turnstile CAPTCHA ─────────────────────────────────────────────────────

  function initBmTurnstile(retries) {
    var r = retries || 0;
    var container = document.getElementById('bm-turnstile');
    if (!container) return;
    var siteKey = container.dataset.sitekey;
    if (!siteKey) return;
    if (!window.turnstile) {
      if (r < 30) setTimeout(function () { initBmTurnstile(r + 1); }, 100);
      return;
    }
    if (_bmTsId !== null) {
      window.turnstile.reset(_bmTsId);
      return;
    }
    _bmTsId = window.turnstile.render('#bm-turnstile', { sitekey: siteKey });
  }

  function getBmTurnstileToken() {
    var el = document.querySelector('#bm-turnstile [name="cf-turnstile-response"]');
    return el ? el.value : '';
  }

  // ── Buchungs-Modal ────────────────────────────────────────────────────────

  var ORG_LABELS = { 'Verein': 'Vereinsname', 'Firma': 'Firmenname', 'Behörde': 'Behördenname' };

  function updateOrgField() {
    var type = getVal('bm-renter-type');
    var orgRow = document.getElementById('bm-org-row');
    var orgLabel = document.getElementById('bm-org-label');
    var firstLabel = document.getElementById('bm-firstname-label');
    var lastLabel = document.getElementById('bm-lastname-label');
    var isOrg = type !== 'Privatperson';
    if (orgRow) orgRow.hidden = !isOrg;
    if (isOrg && orgLabel) orgLabel.textContent = ORG_LABELS[type] || 'Organisationsname';
    if (firstLabel) firstLabel.textContent = isOrg ? 'Vorname Kontaktperson' : 'Vorname';
    if (lastLabel) lastLabel.textContent = isOrg ? 'Nachname Kontaktperson' : 'Nachname';
  }

  function openBookingModal() {
    if (!selectedSlot) return;

    var modal = document.getElementById('booking-modal');
    if (!modal) return;

    var body = document.getElementById('bm-body');
    var footer = document.getElementById('bm-footer');
    var success = document.getElementById('bm-success');
    var errEl = document.getElementById('bm-error');
    var summary = document.getElementById('bm-slot-summary');

    if (summary) {
      summary.textContent = formatDateLong(selectedSlot.day) + ' · ' +
        minutesToTimeStr(selectedSlot.startMin) + ' – ' + minutesToTimeStr(selectedSlot.endMin) + ' Uhr';
    }
    if (body) body.hidden = false;
    if (footer) footer.hidden = false;
    if (success) success.hidden = true;
    if (errEl) { errEl.hidden = true; errEl.textContent = ''; }

    var submitBtn = document.getElementById('bm-submit');
    if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = 'Buchungsanfrage senden'; }

    updateOrgField();
    modal.removeAttribute('hidden');
    document.body.style.overflow = 'hidden';

    initBmTurnstile();

    setTimeout(function () {
      var f = document.getElementById('bm-anlass');
      if (f) f.focus();
    }, 60);
  }

  function closeBookingModal() {
    var modal = document.getElementById('booking-modal');
    if (modal) modal.setAttribute('hidden', '');
    document.body.style.overflow = '';
    if (_bmTsId !== null && window.turnstile) window.turnstile.reset(_bmTsId);
  }

  function showModalError(msg) {
    var errEl = document.getElementById('bm-error');
    if (errEl) { errEl.textContent = msg; errEl.hidden = false; }
  }

  function getVal(id) {
    var el = document.getElementById(id);
    return el ? el.value.trim() : '';
  }

  function submitGuestBooking() {
    if (!selectedSlot) return;

    var renterType = getVal('bm-renter-type') || 'Privatperson';
    var isOrg = renterType !== 'Privatperson';
    var orgName = isOrg ? getVal('bm-org-name') : '';
    var firstname = getVal('bm-firstname');
    var lastname = getVal('bm-lastname');
    var phone = getVal('bm-phone');
    var email = getVal('bm-email');
    var billingStreet = getVal('bm-billing-street');
    var billingExtra = getVal('bm-billing-extra');
    var billingPlz = getVal('bm-billing-plz');
    var billingCity = getVal('bm-billing-city');
    var anlass = getVal('bm-anlass');
    var notizen = getVal('bm-notizen');

    var captchaToken = getBmTurnstileToken();

    var errEl = document.getElementById('bm-error');
    if (errEl) { errEl.hidden = true; errEl.textContent = ''; }

    if (!captchaToken) { showModalError('Bitte das CAPTCHA ausfüllen.'); return; }

    if (isOrg && !orgName) {
      showModalError('Bitte gib den ' + (ORG_LABELS[renterType] || 'Organisationsnamen') + ' ein.');
      return;
    }
    if (!firstname) { showModalError('Bitte gib den Vornamen ein.'); return; }
    if (!lastname) { showModalError('Bitte gib den Nachnamen ein.'); return; }
    if (!phone) { showModalError('Bitte gib die Handynummer ein (erreichbar während des Events).'); return; }
    if (!email || !email.includes('@')) { showModalError('Bitte gib eine gültige E-Mail-Adresse ein.'); return; }
    if (!billingStreet) { showModalError('Bitte gib die Strasse ein.'); return; }
    if (!billingPlz) { showModalError('Bitte gib die PLZ ein.'); return; }
    if (!billingCity) { showModalError('Bitte gib den Ort ein.'); return; }
    if (!anlass) { showModalError('Bitte gib die Bezeichnung ein.'); return; }

    var submitBtn = document.getElementById('bm-submit');
    if (submitBtn) { submitBtn.disabled = true; submitBtn.textContent = 'Wird gesendet…'; }

    fetch('/api/reservierung/gast-buchung', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' },
      body: JSON.stringify({
        contactFirstName: firstname,
        contactLastName: lastname,
        name: isOrg ? orgName : null,
        guestEmail: email,
        guestPhone: phone,
        renterType: renterType,
        billingAddress: billingStreet,
        addressLine2: billingExtra || null,
        billingPostalCode: billingPlz,
        billingCity: billingCity,
        startUtc: selectedSlot.startUtcIso,
        endUtc: selectedSlot.endUtcIso,
        title: anlass,
        notizen: notizen || null,
        captchaToken: captchaToken
      })
    })
    .then(function (res) {
      return res.text().then(function (text) {
        var data = null;
        try { data = JSON.parse(text); } catch (_) { data = { error: text.substring(0, 300) }; }
        return { ok: res.ok, status: res.status, data: data };
      });
    })
    .then(function (result) {
      if (result.ok) {
        var body = document.getElementById('bm-body');
        var footer = document.getElementById('bm-footer');
        var success = document.getElementById('bm-success');
        if (body) body.hidden = true;
        if (footer) footer.hidden = true;
        if (success) success.hidden = false;
        if (_bmTsId !== null && window.turnstile) window.turnstile.reset(_bmTsId);
        clearSelectionOverlay();
        selectedSlot = null;
        loadWeek();
      } else if (result.status === 409) {
        if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = 'Buchungsanfrage senden'; }
        showModalError('Dieser Zeitslot ist leider nicht mehr verfügbar. Bitte wähle einen anderen Termin.');
      } else {
        if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = 'Buchungsanfrage senden'; }
        showModalError((result.data && result.data.error) ? result.data.error : 'Fehler ' + result.status + '. Bitte versuche es erneut.');
      }
    })
    .catch(function (err) {
      if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = 'Buchungsanfrage senden'; }
      showModalError('Verbindungsfehler: ' + (err && err.message ? err.message : 'Bitte prüfe deine Internetverbindung.'));
    });
  }

  // ── Daten laden ───────────────────────────────────────────────────────────

  function loadWeek() {
    var grid = document.getElementById('calendar-grid');
    if (!grid) return;

    var savedScrollY = window.scrollY;
    clearSelectionOverlay();
    grid.innerHTML = '<div class="calendar-loading" style="grid-column:1/-1">Lade Kalender …</div>';

    var von = toLocalDateStr(currentMonday);
    fetch('/api/reservierung/wochen-slots?von=' + von)
      .then(function (res) {
        if (!res.ok) throw new Error('HTTP ' + res.status);
        return res.json();
      })
      .then(function (slots) {
        lastSlots = slots;
        renderGrid(slots);
        window.scrollTo({ top: savedScrollY, behavior: 'instant' });
      })
      .catch(function () {
        grid.innerHTML = '<div class="calendar-loading" style="grid-column:1/-1">Kalender konnte nicht geladen werden.</div>';
        window.scrollTo({ top: savedScrollY, behavior: 'instant' });
      });
  }

  function navigateWeek(delta) {
    currentMonday = addDays(currentMonday, delta * DAYS_TO_SHOW);
    updateWeekLabel();
    if (dpOpen) {
      dpCurrentMonth = new Date(currentMonday.getFullYear(), currentMonday.getMonth(), 1);
      renderDatePicker();
    }
    loadWeek();
  }

  function updateWeekLabel() {
    var label = document.getElementById('week-label');
    if (label) label.textContent = formatWeekLabel(currentMonday);
  }

  function loadConfig(callback) {
    fetch('/api/reservierung/konfiguration')
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(function (cfg) {
        if (cfg) {
          if (cfg.oeffnungVon !== undefined) OPENING_HOUR_START = cfg.oeffnungVon;
          if (cfg.oeffnungBis !== undefined) OPENING_HOUR_END = cfg.oeffnungBis;
          TOTAL_BLOCKS = (OPENING_HOUR_END - OPENING_HOUR_START) * (60 / BLOCK_MINUTES);
          if (cfg.vorlaufzeitTage !== undefined) VORLAUFZEIT_TAGE = cfg.vorlaufzeitTage;
          BUCHUNGEN_MAX_TAGE = cfg.buchungenMaxTage || null;
          if (cfg.preisText) {
            var panel = document.getElementById('preis-panel');
            var text = document.getElementById('preis-panel-text');
            if (panel && text) {
              text.textContent = cfg.preisText;
              panel.hidden = false;
            }
          }
        }
        callback();
      })
      .catch(function () { callback(); });
  }

  // ── Datepicker ────────────────────────────────────────────────────────────

  function isInCurrentWeek(date) {
    var monday = getMonday(new Date(date));
    return toLocalDateStr(monday) === toLocalDateStr(currentMonday);
  }

  function openDatePicker() {
    dpCurrentMonth = new Date(currentMonday.getFullYear(), currentMonday.getMonth(), 1);
    renderDatePicker();
    var popup = document.getElementById('date-picker');
    if (popup) popup.classList.add('dp-open');
    var toggle = document.getElementById('dp-toggle');
    if (toggle) toggle.classList.add('dp-active');
    dpOpen = true;
  }

  function closeDatePicker() {
    var popup = document.getElementById('date-picker');
    if (popup) popup.classList.remove('dp-open');
    var toggle = document.getElementById('dp-toggle');
    if (toggle) toggle.classList.remove('dp-active');
    dpOpen = false;
  }

  function toggleDatePicker() {
    if (dpOpen) closeDatePicker(); else openDatePicker();
  }

  function jumpToDate(date) {
    currentMonday = getMonday(date);
    updateWeekLabel();
    closeDatePicker();
    loadWeek();
  }

  function renderDatePicker() {
    var popup = document.getElementById('date-picker');
    if (!popup || !dpCurrentMonth) return;

    var year = dpCurrentMonth.getFullYear();
    var month = dpCurrentMonth.getMonth();

    var html = '<div class="dp-header">';
    html += '<button type="button" class="dp-nav-btn" id="dp-prev-month">&#8249;</button>';
    html += '<span class="dp-month-label">' + DP_MONTH_NAMES[month] + ' ' + year + '</span>';
    html += '<button type="button" class="dp-nav-btn" id="dp-next-month">&#8250;</button>';
    html += '</div><div class="dp-grid">';

    ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'].forEach(function (d) {
      html += '<div class="dp-weekday">' + d + '</div>';
    });

    var firstDay = new Date(year, month, 1);
    var startPad = (firstDay.getDay() + 6) % 7;
    for (var p = 0; p < startPad; p++) html += '<div class="dp-day dp-day--empty"></div>';

    var daysInMonth = new Date(year, month + 1, 0).getDate();
    for (var d = 1; d <= daysInMonth; d++) {
      var date = new Date(year, month, d);
      var past = isPastDay(date);
      var sn   = !past && isShortNotice(date);
      var bc   = !past && !sn && isBeyondMaxDays(date);
      var inW  = isInCurrentWeek(date);
      var tod  = isToday(date);

      var cls = 'dp-day';
      if (past)    cls += ' dp-day--past';
      else if (sn) cls += ' dp-day--short-notice';
      else if (bc) cls += ' dp-day--beyond-cutoff';
      else         cls += ' dp-day--bookable';
      if (inW) cls += ' dp-day--in-week';
      if (tod) cls += ' dp-day--today';

      var attr = (!past && !bc) ? (' data-date="' + toLocalDateStr(date) + '"') : '';
      html += '<div class="' + cls + '"' + attr + '>' + d + '</div>';
    }
    html += '</div>';

    if (VORLAUFZEIT_TAGE > 0 || BUCHUNGEN_MAX_TAGE) {
      html += '<div class="dp-legend">';
      if (VORLAUFZEIT_TAGE > 0) {
        html += '<span class="dp-legend-item"><span class="dp-legend-dot dp-legend-dot--sn"></span>Kurzfristig (&lt;' + VORLAUFZEIT_TAGE + 'T)</span>';
      }
      if (BUCHUNGEN_MAX_TAGE) {
        html += '<span class="dp-legend-item"><span class="dp-legend-dot dp-legend-dot--bc"></span>Max. Vorausbuchung</span>';
      }
      html += '</div>';
    }

    popup.innerHTML = html;

    document.getElementById('dp-prev-month').addEventListener('click', function (e) {
      e.stopPropagation();
      dpCurrentMonth = new Date(dpCurrentMonth.getFullYear(), dpCurrentMonth.getMonth() - 1, 1);
      renderDatePicker();
    });
    document.getElementById('dp-next-month').addEventListener('click', function (e) {
      e.stopPropagation();
      dpCurrentMonth = new Date(dpCurrentMonth.getFullYear(), dpCurrentMonth.getMonth() + 1, 1);
      renderDatePicker();
    });

    popup.querySelectorAll('.dp-day[data-date]').forEach(function (dayEl) {
      dayEl.addEventListener('click', function () {
        var parts = dayEl.dataset.date.split('-');
        jumpToDate(new Date(+parts[0], +parts[1] - 1, +parts[2]));
      });
    });
  }

  // ── Init ──────────────────────────────────────────────────────────────────

  function init() {
    checkMobile();
    if (DAYS_TO_SHOW === 3) {
      currentMonday = new Date();
      currentMonday.setHours(0, 0, 0, 0);
    }

    var prevBtn = document.getElementById('prev-week');
    var nextBtn = document.getElementById('next-week');
    if (prevBtn) prevBtn.addEventListener('click', function () { navigateWeek(-1); });
    if (nextBtn) nextBtn.addEventListener('click', function () { navigateWeek(+1); });

    window.addEventListener('resize', function () {
      clearTimeout(resizeTimer);
      resizeTimer = setTimeout(function () {
        var prevDays = DAYS_TO_SHOW;
        checkMobile();
        var grid = document.getElementById('calendar-grid');
        if (!grid) return;
        if (prevDays !== DAYS_TO_SHOW) {
          if (DAYS_TO_SHOW === 7) currentMonday = getMonday(currentMonday);
          updateWeekLabel();
          renderGrid(lastSlots);
          return;
        }
        if (!lastSlots.length) return;
        var days = [];
        for (var i = 0; i < DAYS_TO_SHOW; i++) days.push(addDays(currentMonday, i));
        renderBookingOverlays(lastSlots, days, grid);
        if (selectedSlot) {
          var layout = computeGridLayout();
          if (layout) renderSelectionOverlay(selectedSlot.dayIdx, selectedSlot.startBlock, selectedSlot.endBlock, layout);
        }
      }, 150);
    });

    // Drag-Events
    var grid = document.getElementById('calendar-grid');
    if (grid) {
      grid.addEventListener('mousedown', onDragStart);
      grid.addEventListener('touchstart', onDragStart, { passive: false });
    }
    document.addEventListener('mousemove', onDragMove);
    document.addEventListener('touchmove', onDragMove, { passive: false });
    document.addEventListener('mouseup', onDragEnd);
    document.addEventListener('touchend', onDragEnd);

    // Slot-Panel-Button
    var buchBtn = document.getElementById('btn-jetzt-buchen');
    if (buchBtn) buchBtn.addEventListener('click', openBookingModal);

    // Mietertyp-Auswahl → Organisationsfeld ein-/ausblenden
    var renterTypeSelect = document.getElementById('bm-renter-type');
    if (renterTypeSelect) renterTypeSelect.addEventListener('change', updateOrgField);

    // Modal-Buttons
    var closeBtn = document.getElementById('bm-close');
    var cancelBtn = document.getElementById('bm-cancel');
    var submitBtn = document.getElementById('bm-submit');
    var doneBtn = document.getElementById('bm-done');
    if (closeBtn) closeBtn.addEventListener('click', closeBookingModal);
    if (cancelBtn) cancelBtn.addEventListener('click', closeBookingModal);
    if (submitBtn) submitBtn.addEventListener('click', submitGuestBooking);
    if (doneBtn) doneBtn.addEventListener('click', function () { closeBookingModal(); });

    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape') { closeBookingModal(); closeDatePicker(); }
    });

    var dpToggle = document.getElementById('dp-toggle');
    if (dpToggle) dpToggle.addEventListener('click', function (e) {
      e.stopPropagation();
      toggleDatePicker();
    });
    document.addEventListener('click', function (e) {
      if (!dpOpen) return;
      var popup = document.getElementById('date-picker');
      var toggle = document.getElementById('dp-toggle');
      if (popup && !popup.contains(e.target) && toggle && !toggle.contains(e.target)) {
        closeDatePicker();
      }
    });

    updateWeekLabel();
    loadConfig(function () { loadWeek(); });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
