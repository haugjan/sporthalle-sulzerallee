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

  var currentMonday = getMonday(new Date());
  var lastSlots = [];
  var resizeTimer;

  // Drag-State
  var dragState = null;
  var selectionEl = null;
  var selectedSlot = null;
  var isDragging = false;

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

  function formatWeekLabel(monday) {
    var sunday = addDays(monday, 6);
    var opts = { day: '2-digit', month: '2-digit', year: 'numeric' };
    return monday.toLocaleDateString('de-CH', opts) + ' – ' + sunday.toLocaleDateString('de-CH', opts);
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
    return diff === 1 || diff === 2;
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
    selectionEl = null;

    var days = [];
    for (var i = 0; i < 7; i++) days.push(addDays(currentMonday, i));

    // Kopfzeile
    var timeCorner = document.createElement('div');
    timeCorner.className = 'cal-header-time';
    grid.appendChild(timeCorner);

    days.forEach(function (day) {
      var cell = document.createElement('div');
      cell.className = 'cal-header-day';
      if (isPastDay(day)) cell.classList.add('is-past');
      else if (isToday(day)) cell.classList.add('is-today');
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
      el.style.background = slot.color || getDefaultColor(slot);

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
    if (slot.type === 'Booked' || slot.type === 'Blocker') return 'confirmed';
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
    if (target.classList.contains('is-today')) return;

    var layout = computeGridLayout();
    if (!layout) return;

    var xy = getClientXY(e);
    var relX = xy.x - layout.gridRect.left;
    var relY = xy.y - layout.gridRect.top;

    var dayIdx = dayIdxFromX(relX, layout.cols);
    var days = [];
    for (var i = 0; i < 7; i++) days.push(addDays(currentMonday, i));

    if (dayIdx < 0 || dayIdx >= days.length) return;
    if (isPastDay(days[dayIdx]) || isToday(days[dayIdx])) return;

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
    var timeDisplay = document.getElementById('slot-time-display');
    var buchBtn = document.getElementById('btn-jetzt-buchen');
    var notice = document.getElementById('slot-short-notice');

    if (!detail) return;

    if (hint) hint.hidden = true;
    detail.hidden = false;

    if (dateLabel) dateLabel.textContent = formatDateLong(slot.day);
    if (timeDisplay) timeDisplay.textContent =
      minutesToTimeStr(slot.startMin) + ' – ' + minutesToTimeStr(slot.endMin) + ' Uhr';

    var shortNotice = isShortNotice(slot.day);
    if (buchBtn) buchBtn.hidden = shortNotice;
    if (notice) notice.hidden = !shortNotice;

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

    setTimeout(function () {
      var f = document.getElementById('bm-anlass');
      if (f) f.focus();
    }, 60);
  }

  function closeBookingModal() {
    var modal = document.getElementById('booking-modal');
    if (modal) modal.setAttribute('hidden', '');
    document.body.style.overflow = '';
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

    var errEl = document.getElementById('bm-error');
    if (errEl) { errEl.hidden = true; errEl.textContent = ''; }

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

    var guestName = firstname + ' ' + lastname;
    var billingName = isOrg ? orgName : guestName;
    var billingAddress = billingStreet + (billingExtra ? ', ' + billingExtra : '');

    var submitBtn = document.getElementById('bm-submit');
    if (submitBtn) { submitBtn.disabled = true; submitBtn.textContent = 'Wird gesendet…'; }

    fetch('/api/reservierung/gast-buchung', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' },
      body: JSON.stringify({
        firstName: firstname,
        lastName: lastname,
        contactPerson: guestName,
        guestEmail: email,
        guestPhone: phone,
        renterType: renterType,
        billingName: billingName,
        billingAddress: billingAddress,
        billingPostalCode: billingPlz,
        billingCity: billingCity,
        startUtc: selectedSlot.startUtcIso,
        endUtc: selectedSlot.endUtcIso,
        title: anlass,
        notizen: notizen || null
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
    currentMonday = addDays(currentMonday, delta * 7);
    updateWeekLabel();
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

  // ── Init ──────────────────────────────────────────────────────────────────

  function init() {
    var prevBtn = document.getElementById('prev-week');
    var nextBtn = document.getElementById('next-week');
    if (prevBtn) prevBtn.addEventListener('click', function () { navigateWeek(-1); });
    if (nextBtn) nextBtn.addEventListener('click', function () { navigateWeek(+1); });

    window.addEventListener('resize', function () {
      clearTimeout(resizeTimer);
      resizeTimer = setTimeout(function () {
        var grid = document.getElementById('calendar-grid');
        if (!grid || !lastSlots.length) return;
        var days = [];
        for (var i = 0; i < 7; i++) days.push(addDays(currentMonday, i));
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

    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeBookingModal(); });

    updateWeekLabel();
    loadConfig(function () { loadWeek(); });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
