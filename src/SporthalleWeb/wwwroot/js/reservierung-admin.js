/* Admin-Kalender – initialisiert via Blazor JSInterop (SporthalleAdmin.initKalender) */
window.SporthalleAdmin = (function () {
  'use strict';

  // Konfiguration (wird von /api/reservierung/konfiguration überschrieben)
  var OPENING_HOUR_START = 7;
  var OPENING_HOUR_END = 23;
  var BLOCK_MINUTES = 30;
  var TOTAL_BLOCKS = (OPENING_HOUR_END - OPENING_HOUR_START) * (60 / BLOCK_MINUTES);
  var CELL_HEIGHT = 20;
  var CELL_GAP = 1;
  var CELL_STEP = CELL_HEIGHT + CELL_GAP;

  // State
  var _dotNet = null;
  var _handlers = [];
  var currentMonday = getMonday(new Date());
  var lastSlots = [];
  var resizeTimer;
  var dragState = null;
  var selectionEl = null;
  var selectedSlot = null;
  var isDragging = false;

  // ── Datum-Hilfsfunktionen ────────────────────────────────────────────────────

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

  // ── UTC → Zürich ─────────────────────────────────────────────────────────────

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
    var probe = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), h, m, 0));
    var zurichParts = getZurichParts(probe);
    var offsetMin = (zurichParts.hour * 60 + zurichParts.minute) - (h * 60 + m);
    var utc = new Date(probe.getTime() - offsetMin * 60000);
    return utc.toISOString();
  }

  // ── Grid-Layout ──────────────────────────────────────────────────────────────

  function computeGridLayout() {
    var grid = document.getElementById('admin-calendar-grid');
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

  // ── Grid rendern ─────────────────────────────────────────────────────────────

  function renderGrid(slots) {
    var grid = document.getElementById('admin-calendar-grid');
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

  // ── Booking-Overlays (admin: zeigt Titel + Mieter) ───────────────────────────

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
      // Admin: vergangene Tage werden trotzdem angezeigt

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
      if (slot.color) el.style.background = slot.color;

      if (height >= 18) {
        var label = document.createElement('div');
        label.className = 'booking-overlay__label';

        if (slot.title && height >= 30) {
          var titleEl = document.createElement('span');
          titleEl.className = 'bol-title';
          titleEl.textContent = slot.title;
          label.appendChild(titleEl);
        }

        if (slot.mieterName && height >= 44) {
          var mieterEl = document.createElement('span');
          mieterEl.className = 'bol-time';
          mieterEl.textContent = slot.mieterName;
          label.appendChild(mieterEl);
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
    if (slot.type === 'Booked') return 'confirmed';
    if (slot.type === 'Blocker') return 'recurring';
    return 'provisional'; // Reserved
  }

  // ── Drag-to-Select ───────────────────────────────────────────────────────────

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
    var grid = document.getElementById('admin-calendar-grid');
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
      ? (durationMin % 60 === 0 ? (durationMin / 60) + ' h' : Math.floor(durationMin / 60) + ' h ' + (durationMin % 60) + ' min')
      : durationMin + ' min';
    el.innerHTML = '<span class="cal-selection__label">' + durationText + '</span>';
    grid.appendChild(el);
    selectionEl = el;
  }

  function clearSelectionOverlay() {
    if (selectionEl) { selectionEl.remove(); selectionEl = null; }
    selectedSlot = null;
    var detail = document.getElementById('admin-slot-panel-detail');
    var hint = document.getElementById('admin-slot-panel-hint');
    if (detail) detail.hidden = true;
    if (hint) hint.hidden = false;
  }

  function getClientXY(e) {
    if (e.touches && e.touches.length > 0) return { x: e.touches[0].clientX, y: e.touches[0].clientY };
    if (e.changedTouches && e.changedTouches.length > 0) return { x: e.changedTouches[0].clientX, y: e.changedTouches[0].clientY };
    return { x: e.clientX, y: e.clientY };
  }

  function onDragStart(e) {
    var target = e.target;
    if (!target.classList.contains('cal-cell')) return;
    if (target.classList.contains('is-past')) return;
    // Admin: today is allowed

    var layout = computeGridLayout();
    if (!layout) return;
    var xy = getClientXY(e);
    var relX = xy.x - layout.gridRect.left;
    var relY = xy.y - layout.gridRect.top;
    var dayIdx = dayIdxFromX(relX, layout.cols);
    var days = [];
    for (var i = 0; i < 7; i++) days.push(addDays(currentMonday, i));
    if (dayIdx < 0 || dayIdx >= days.length) return;
    if (isPastDay(days[dayIdx])) return;
    var startBlock = blockFromY(relY, layout.contentTop);
    if (isBlockOccupied(dayIdx, startBlock, days)) return;
    e.preventDefault();
    isDragging = true;
    dragState = { dayIdx: dayIdx, startBlock: startBlock, currentEndBlock: startBlock + 2, days: days, layout: layout };
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
    var endBlock = Math.max(dragState.startBlock + 2, Math.min(TOTAL_BLOCKS, rawBlock));
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
    if (endBlock - startBlock < 2) endBlock = startBlock + 2;
    for (var b = startBlock; b < endBlock; b++) {
      if (isBlockOccupied(dayIdx, b, dragState.days)) { endBlock = b; break; }
    }
    dragState = null;
    if (endBlock - startBlock < 2) { clearSelectionOverlay(); return; }
    var startMin = blockToMinutes(startBlock);
    var endMin = blockToMinutes(endBlock);
    selectedSlot = {
      day: day, dayIdx: dayIdx,
      startBlock: startBlock, endBlock: endBlock,
      startMin: startMin, endMin: endMin,
      startUtcIso: localDateToUtcIso(day, startMin),
      endUtcIso: localDateToUtcIso(day, endMin)
    };
    var freshLayout = computeGridLayout();
    renderSelectionOverlay(dayIdx, startBlock, endBlock, freshLayout || layout);
    showSlotPanel(selectedSlot);
  }

  // ── Slot-Panel ───────────────────────────────────────────────────────────────

  function showSlotPanel(slot) {
    var hint = document.getElementById('admin-slot-panel-hint');
    var detail = document.getElementById('admin-slot-panel-detail');
    var dateLabel = document.getElementById('admin-slot-date-label');
    var timeDisplay = document.getElementById('admin-slot-time-display');
    var metaEl = document.getElementById('admin-slot-meta');
    if (!detail) return;
    if (hint) hint.hidden = true;
    detail.hidden = false;
    if (dateLabel) dateLabel.textContent = formatDateLong(slot.day);
    if (timeDisplay) timeDisplay.textContent = minutesToTimeStr(slot.startMin) + ' – ' + minutesToTimeStr(slot.endMin) + ' Uhr';
    var durationMin = slot.endMin - slot.startMin;
    if (metaEl) metaEl.textContent = 'Dauer: ' + (durationMin >= 60 ? (durationMin / 60) + ' h' : durationMin + ' min');
    var panel = document.getElementById('admin-slot-panel');
    if (panel) {
      setTimeout(function () {
        var rect = panel.getBoundingClientRect();
        if (rect.top < 60 || rect.bottom > window.innerHeight) {
          panel.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      }, 50);
    }
  }

  // ── Admin-Modal ───────────────────────────────────────────────────────────────

  var ORG_LABELS = { 'Verein': 'Vereinsname', 'Firma': 'Firmenname', 'Behörde': 'Behördenname' };
  var _isBlocker = true;

  function openAdminModal() {
    if (!selectedSlot) return;
    var modal = document.getElementById('admin-booking-modal');
    if (!modal) return;
    var summary = document.getElementById('admin-bm-slot-summary');
    if (summary) {
      summary.textContent = formatDateLong(selectedSlot.day) + ' · ' +
        minutesToTimeStr(selectedSlot.startMin) + ' – ' + minutesToTimeStr(selectedSlot.endMin) + ' Uhr';
    }
    var title = document.getElementById('admin-bm-title');
    if (title) title.textContent = 'Slot erfassen';
    resetAdminModal();
    setModalTyp(true);
    modal.removeAttribute('hidden');
    document.body.style.overflow = 'hidden';
    setTimeout(function () {
      var f = document.getElementById('admin-bm-titel');
      if (f) f.focus();
    }, 60);
  }

  function closeAdminModal() {
    var modal = document.getElementById('admin-booking-modal');
    if (modal) modal.setAttribute('hidden', '');
    document.body.style.overflow = '';
  }

  function resetAdminModal() {
    setEl('admin-bm-titel', '');
    setEl('admin-bm-notizen-blocker', '');
    setEl('admin-bm-anlass', '');
    setEl('admin-bm-renter-type', 'Privatperson');
    setEl('admin-bm-org-name', '');
    setEl('admin-bm-firstname', '');
    setEl('admin-bm-lastname', '');
    setEl('admin-bm-email', '');
    setEl('admin-bm-phone', '');
    setEl('admin-bm-notizen', '');
    hideError('admin-bm-error-blocker');
    hideError('admin-bm-error');
    var success = document.getElementById('admin-bm-success');
    if (success) success.hidden = true;
    var bodyBlocker = document.getElementById('admin-bm-body-blocker');
    if (bodyBlocker) bodyBlocker.hidden = false;
    var bodyEvent = document.getElementById('admin-bm-body-event');
    if (bodyEvent) bodyEvent.hidden = true;
    enableSubmit('admin-bm-submit-blocker', 'Blocker speichern');
    enableSubmit('admin-bm-submit', 'Buchungsanfrage erstellen');
  }

  function setModalTyp(isBlocker) {
    _isBlocker = isBlocker;
    var btnBlocker = document.getElementById('admin-typ-blocker');
    var btnEvent = document.getElementById('admin-typ-event');
    var bodyBlocker = document.getElementById('admin-bm-body-blocker');
    var bodyEvent = document.getElementById('admin-bm-body-event');
    if (btnBlocker) btnBlocker.className = isBlocker ? 'bm-btn-primary' : 'bm-btn-secondary';
    if (btnEvent) btnEvent.className = isBlocker ? 'bm-btn-secondary' : 'bm-btn-primary';
    if (bodyBlocker) bodyBlocker.hidden = !isBlocker;
    if (bodyEvent) bodyEvent.hidden = isBlocker;
    if (!isBlocker) updateOrgField();
  }

  function updateOrgField() {
    var type = getVal('admin-bm-renter-type');
    var orgRow = document.getElementById('admin-bm-org-row');
    var orgLabel = document.getElementById('admin-bm-org-label');
    var isOrg = type !== 'Privatperson';
    if (orgRow) orgRow.hidden = !isOrg;
    if (isOrg && orgLabel) orgLabel.textContent = ORG_LABELS[type] || 'Organisationsname';
  }

  function submitAdminBooking() {
    if (!selectedSlot || !_dotNet) return;
    hideError(_isBlocker ? 'admin-bm-error-blocker' : 'admin-bm-error');
    var payload;
    if (_isBlocker) {
      var titel = getVal('admin-bm-titel');
      if (!titel) { showError('admin-bm-error-blocker', 'Bezeichnung ist erforderlich.'); return; }
      payload = {
        isBlocker: true,
        startUtc: selectedSlot.startUtcIso,
        endUtc: selectedSlot.endUtcIso,
        titel: titel,
        notizen: getVal('admin-bm-notizen-blocker') || null
      };
      disableSubmit('admin-bm-submit-blocker', 'Speichern…');
    } else {
      var renterType = getVal('admin-bm-renter-type') || 'Privatperson';
      var isOrg = renterType !== 'Privatperson';
      var orgName = isOrg ? getVal('admin-bm-org-name') : '';
      var firstname = getVal('admin-bm-firstname');
      var lastname = getVal('admin-bm-lastname');
      var email = getVal('admin-bm-email');
      var anlass = getVal('admin-bm-anlass');
      if (isOrg && !orgName) { showError('admin-bm-error', 'Bitte ' + (ORG_LABELS[renterType] || 'Organisationsname') + ' eingeben.'); return; }
      if (!firstname) { showError('admin-bm-error', 'Vorname ist erforderlich.'); return; }
      if (!lastname) { showError('admin-bm-error', 'Nachname ist erforderlich.'); return; }
      if (!email || !email.includes('@')) { showError('admin-bm-error', 'Gültige E-Mail-Adresse erforderlich.'); return; }
      if (!anlass) { showError('admin-bm-error', 'Anlass ist erforderlich.'); return; }
      payload = {
        isBlocker: false,
        startUtc: selectedSlot.startUtcIso,
        endUtc: selectedSlot.endUtcIso,
        anlass: anlass,
        renterType: renterType,
        orgName: orgName || null,
        firstname: firstname,
        lastname: lastname,
        email: email,
        phone: getVal('admin-bm-phone') || null,
        notizen: getVal('admin-bm-notizen') || null
      };
      disableSubmit('admin-bm-submit', 'Wird gespeichert…');
    }

    _dotNet.invokeMethodAsync('BuchungAnlegenAsync', JSON.stringify(payload))
      .then(function (resultJson) {
        var result = JSON.parse(resultJson);
        if (result.ok) {
          var bodyBlocker = document.getElementById('admin-bm-body-blocker');
          var bodyEvent = document.getElementById('admin-bm-body-event');
          var success = document.getElementById('admin-bm-success');
          var successTitle = document.getElementById('admin-bm-success-title');
          var successMsg = document.getElementById('admin-bm-success-msg');
          if (bodyBlocker) bodyBlocker.hidden = true;
          if (bodyEvent) bodyEvent.hidden = true;
          if (successTitle) successTitle.textContent = result.isBlocker ? 'Blocker gespeichert' : 'Buchungsanfrage erstellt';
          if (successMsg) successMsg.textContent = result.isBlocker
            ? 'Der Blocker erscheint nun im Kalender.'
            : 'Die Buchungsanfrage erscheint unter «Pendente».';
          if (success) success.hidden = false;
          var title = document.getElementById('admin-bm-title');
          if (title) title.textContent = result.isBlocker ? 'Blocker gespeichert' : 'Buchungsanfrage erstellt';
          clearSelectionOverlay();
          selectedSlot = null;
          loadWeek();
        } else {
          if (_isBlocker) {
            enableSubmit('admin-bm-submit-blocker', 'Blocker speichern');
            showError('admin-bm-error-blocker', result.error || 'Fehler beim Speichern.');
          } else {
            enableSubmit('admin-bm-submit', 'Buchungsanfrage erstellen');
            showError('admin-bm-error', result.error || 'Fehler beim Speichern.');
          }
        }
      })
      .catch(function () {
        if (_isBlocker) {
          enableSubmit('admin-bm-submit-blocker', 'Blocker speichern');
          showError('admin-bm-error-blocker', 'Verbindungsfehler. Bitte erneut versuchen.');
        } else {
          enableSubmit('admin-bm-submit', 'Buchungsanfrage erstellen');
          showError('admin-bm-error', 'Verbindungsfehler. Bitte erneut versuchen.');
        }
      });
  }

  // ── Hilfsfunktionen ──────────────────────────────────────────────────────────

  function getVal(id) {
    var el = document.getElementById(id);
    return el ? el.value.trim() : '';
  }

  function setEl(id, val) {
    var el = document.getElementById(id);
    if (el) el.value = val;
  }

  function showError(id, msg) {
    var el = document.getElementById(id);
    if (el) { el.textContent = msg; el.hidden = false; }
  }

  function hideError(id) {
    var el = document.getElementById(id);
    if (el) { el.textContent = ''; el.hidden = true; }
  }

  function disableSubmit(id, label) {
    var el = document.getElementById(id);
    if (el) { el.disabled = true; el.textContent = label; }
  }

  function enableSubmit(id, label) {
    var el = document.getElementById(id);
    if (el) { el.disabled = false; el.textContent = label; }
  }

  // ── Daten laden ───────────────────────────────────────────────────────────────

  function loadWeek() {
    if (!_dotNet) return;
    var grid = document.getElementById('admin-calendar-grid');
    if (!grid) return;
    clearSelectionOverlay();
    grid.innerHTML = '<div class="calendar-loading" style="grid-column:1/-1">Lade Kalender …</div>';
    var von = toLocalDateStr(currentMonday);
    _dotNet.invokeMethodAsync('GetWochenSlotsAsync', von)
      .then(function (json) {
        lastSlots = JSON.parse(json);
        renderGrid(lastSlots);
      })
      .catch(function () {
        if (grid) grid.innerHTML = '<div class="calendar-loading" style="grid-column:1/-1">Kalender konnte nicht geladen werden.</div>';
      });
  }

  function navigateWeek(delta) {
    currentMonday = addDays(currentMonday, delta * 7);
    updateWeekLabel();
    loadWeek();
  }

  function updateWeekLabel() {
    var label = document.getElementById('admin-week-label');
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
        }
        callback();
      })
      .catch(function () { callback(); });
  }

  // ── Event-Handler-Tracking ────────────────────────────────────────────────────

  function addHandler(target, event, fn, opts) {
    if (!target) return;
    target.addEventListener(event, fn, opts || false);
    _handlers.push({ target: target, event: event, fn: fn });
  }

  function removeAllHandlers() {
    _handlers.forEach(function (h) { h.target.removeEventListener(h.event, h.fn); });
    _handlers = [];
  }

  // ── Public API ────────────────────────────────────────────────────────────────

  return {
    initKalender: function (dotNetRef) {
      _dotNet = dotNetRef;
      currentMonday = getMonday(new Date());
      lastSlots = [];
      dragState = null;
      selectionEl = null;
      selectedSlot = null;
      isDragging = false;

      addHandler(document.getElementById('admin-prev-week'), 'click', function () { navigateWeek(-1); });
      addHandler(document.getElementById('admin-next-week'), 'click', function () { navigateWeek(+1); });

      addHandler(document, 'mousemove', onDragMove);
      addHandler(document, 'touchmove', onDragMove, { passive: false });
      addHandler(document, 'mouseup', onDragEnd);
      addHandler(document, 'touchend', onDragEnd);

      var grid = document.getElementById('admin-calendar-grid');
      addHandler(grid, 'mousedown', onDragStart);
      addHandler(grid, 'touchstart', onDragStart, { passive: false });

      // Slot-Panel-Button
      addHandler(document.getElementById('admin-btn-erfassen'), 'click', openAdminModal);

      // Modal-Buttons
      addHandler(document.getElementById('admin-bm-close'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-cancel'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-cancel2'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-submit-blocker'), 'click', submitAdminBooking);
      addHandler(document.getElementById('admin-bm-submit'), 'click', submitAdminBooking);
      addHandler(document.getElementById('admin-bm-done'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-typ-blocker'), 'click', function () { setModalTyp(true); });
      addHandler(document.getElementById('admin-typ-event'), 'click', function () { setModalTyp(false); });
      addHandler(document.getElementById('admin-bm-renter-type'), 'change', updateOrgField);

      var modal = document.getElementById('admin-booking-modal');
      addHandler(modal, 'click', function (e) { if (e.target === modal) closeAdminModal(); });
      addHandler(document, 'keydown', function (e) { if (e.key === 'Escape') closeAdminModal(); });

      addHandler(window, 'resize', function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function () {
          var g = document.getElementById('admin-calendar-grid');
          if (!g || !lastSlots.length) return;
          var days = [];
          for (var i = 0; i < 7; i++) days.push(addDays(currentMonday, i));
          renderBookingOverlays(lastSlots, days, g);
          if (selectedSlot) {
            var layout = computeGridLayout();
            if (layout) renderSelectionOverlay(selectedSlot.dayIdx, selectedSlot.startBlock, selectedSlot.endBlock, layout);
          }
        }, 150);
      });

      updateWeekLabel();
      loadConfig(function () { loadWeek(); });
    },

    destroyKalender: function () {
      removeAllHandlers();
      _dotNet = null;
      lastSlots = [];
      dragState = null;
      selectionEl = null;
      selectedSlot = null;
      isDragging = false;
    }
  };
})();
