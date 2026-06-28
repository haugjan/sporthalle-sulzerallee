/* Admin calendar – initialised via Blazor JSInterop (SporthalleAdmin.initCalendar) */
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
  var _docEditMouseUp = null;
  var _docEditMouseMove = null;
  var editSelEl = null;
  var currentMonday = getMonday(new Date());
  var lastSlots = [];
  var resizeTimer;
  var dragState = null;
  var selectionEl = null;
  var selectedSlot = null;
  var isDragging = false;

  // Datepicker-State
  var dpCurrentMonth = null;
  var dpOpen = false;
  var DP_MONTH_NAMES = ['Januar','Februar','März','April','Mai','Juni','Juli','August','September','Oktober','November','Dezember'];

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

  function timeStrToMinutes(str) {
    var m = /^(\d{1,2}):(\d{2})$/.exec(str || '');
    if (!m) return null;
    var h = parseInt(m[1], 10), mi = parseInt(m[2], 10);
    if (h < 0 || h > 23 || mi < 0 || mi > 59) return null;
    return h * 60 + mi;
  }

  // Build start/end UTC ISO from the admin single-form time inputs (5-minute precision),
  // anchored to the dragged day. Returns null when invalid (bis must be after von).
  function adminSlotTimesUtc(startId, endId) {
    if (!selectedSlot) return null;
    var sm = timeStrToMinutes(getVal(startId));
    var em = timeStrToMinutes(getVal(endId));
    if (sm === null || em === null || em <= sm) return null;
    return {
      startUtc: localDateToUtcIso(selectedSlot.day, sm),
      endUtc: localDateToUtcIso(selectedSlot.day, em)
    };
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

  // ── Grid rendern ─────────────────────────────────────────────────────────────

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
      if (isPastDay(day)) cell.classList.add('is-past-admin');
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
        if (isPastDay(day)) cell.classList.add('is-past-admin');
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
      el.style.background = slot.color || getAdminDefaultColor(slot);

      if (height >= 18) {
        var label = document.createElement('div');
        label.className = 'booking-overlay__label';

        if (slot.title && height >= 30) {
          var titleEl = document.createElement('span');
          titleEl.className = 'bol-title';
          titleEl.textContent = slot.title;
          label.appendChild(titleEl);
        }

        if (slot.renterName && height >= 44) {
          var renterEl = document.createElement('span');
          renterEl.className = 'bol-time';
          renterEl.textContent = slot.renterName;
          label.appendChild(renterEl);
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
    if (slot.type === 'Booked' || slot.type === 'Recurring') return 'confirmed';
    if (slot.type === 'Blocker') return 'recurring';
    if (slot.type === 'Serie') return 'confirmed';
    return 'provisional'; // Reserved
  }

  function getAdminDefaultColor(slot) {
    if (slot.type === 'Blocker') return '#78909C';
    if (slot.type === 'Recurring') return '#C62828';
    if (slot.type === 'Serie') return '#C62828';
    if (slot.type === 'Reserved') return '#F1C40F';
    return '#444';
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
      ? (durationMin % 60 === 0 ? (durationMin / 60) + ' h' : Math.floor(durationMin / 60) + ' h ' + (durationMin % 60) + ' min')
      : durationMin + ' min';
    el.innerHTML = '<span class="cal-selection__label">' + durationText + '</span>';
    grid.appendChild(el);
    selectionEl = el;
  }

  function clearSelectionOverlay() {
    if (selectionEl) { selectionEl.remove(); selectionEl = null; }
    selectedSlot = null;
    var detail = document.getElementById('slot-panel-detail');
    var hint = document.getElementById('slot-panel-hint');
    if (detail) detail.hidden = true;
    if (hint) hint.hidden = false;
  }

  function getClientXY(e) {
    if (e.touches && e.touches.length > 0) return { x: e.touches[0].clientX, y: e.touches[0].clientY };
    if (e.changedTouches && e.changedTouches.length > 0) return { x: e.changedTouches[0].clientX, y: e.changedTouches[0].clientY };
    return { x: e.clientX, y: e.clientY };
  }

  function onDragStart(e) {
    e.preventDefault(); // prevent text selection anywhere inside the calendar grid
    var target = e.target;
    if (!target.classList.contains('cal-cell')) return;

    var layout = computeGridLayout();
    if (!layout) return;
    var xy = getClientXY(e);
    var relX = xy.x - layout.gridRect.left;
    var relY = xy.y - layout.gridRect.top;
    var dayIdx = dayIdxFromX(relX, layout.cols);
    var days = [];
    for (var i = 0; i < 7; i++) days.push(addDays(currentMonday, i));
    if (dayIdx < 0 || dayIdx >= days.length) return;
    var startBlock = blockFromY(relY, layout.contentTop);
    if (isBlockOccupied(dayIdx, startBlock, days)) return;
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
    var hint = document.getElementById('slot-panel-hint');
    var detail = document.getElementById('slot-panel-detail');
    var dateLabel = document.getElementById('slot-date-label');
    var vonEl = document.getElementById('slot-von');
    var bisEl = document.getElementById('slot-bis');
    var metaEl = document.getElementById('slot-meta');
    if (!detail) return;
    if (hint) hint.hidden = true;
    detail.hidden = false;
    if (dateLabel) dateLabel.textContent = formatDateLong(slot.day);
    if (vonEl) vonEl.textContent = minutesToTimeStr(slot.startMin) + ' Uhr';
    if (bisEl) bisEl.textContent = minutesToTimeStr(slot.endMin) + ' Uhr';
    var durationMin = slot.endMin - slot.startMin;
    if (metaEl) metaEl.textContent = 'Dauer: ' + (durationMin >= 60 ? (durationMin / 60) + ' h' : durationMin + ' min');
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

  // ── Admin-Modal ───────────────────────────────────────────────────────────────

  var _modalType = 'blocker'; // 'blocker' | 'booking'
  var _modalSched = 'single'; // 'single' | 'recurring'
  var _recurringPayload = null;
  var _memberSearchTimer = null;

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
    // Prefill the 5-minute time inputs from the dragged (30-minute) selection.
    var stStr = minutesToTimeStr(selectedSlot.startMin);
    var enStr = minutesToTimeStr(selectedSlot.endMin);
    setEl('admin-bm-blocker-start', stStr);
    setEl('admin-bm-blocker-end', enStr);
    setEl('admin-bm-booking-start', stStr);
    setEl('admin-bm-booking-end', enStr);
    if (_dotNet) _dotNet.invokeMethodAsync('SetBookingTimes', stStr, enStr);
    modal.removeAttribute('hidden');
    document.body.style.overflow = 'hidden';
    setTimeout(function () {
      var f = document.getElementById('admin-bm-blocker-title');
      if (f) f.focus();
    }, 60);
  }

  function closeAdminModal() {
    var modal = document.getElementById('admin-booking-modal');
    if (modal) modal.setAttribute('hidden', '');
    document.body.style.overflow = '';
  }

  function resetAdminModal() {
    setEl('admin-bm-blocker-title', '');
    setEl('admin-bm-notes-blocker', '');
    setEl('admin-bm-event', '');
    setEl('admin-bm-notes', '');
    clearSelectedMember();
    hideError('admin-bm-error-blocker');
    hideError('admin-bm-error');
    hideError('admin-bm-error-recurring');
    hideRecurringConflicts();
    _recurringPayload = null;
    var success = document.getElementById('admin-bm-success');
    if (success) success.hidden = true;
    _modalType = 'blocker';
    _modalSched = 'single';
    refreshModalView();
    enableSubmit('admin-bm-submit-blocker', 'Blocker speichern');
    enableSubmit('admin-bm-submit', 'Buchung erfassen');
    enableSubmit('admin-bm-submit-recurring', 'Serientermine erstellen');
  }

  function hideRecurringConflicts() {
    var panel = document.getElementById('admin-bm-recurring-conflicts');
    if (panel) panel.hidden = true;
    var text = document.getElementById('admin-bm-recurring-conflict-text');
    if (text) text.textContent = '';
  }

  function setModalType(typ) {
    _modalType = typ;
    refreshModalView();
  }

  function setModalSched(sched) {
    _modalSched = sched;
    refreshModalView();
    if (sched === 'recurring') prefillRecurringForm();
  }

  function refreshModalView() {
    var btnBlocker = document.getElementById('admin-type-blocker');
    var btnBooking = document.getElementById('admin-type-booking');
    var btnSingle  = document.getElementById('admin-sched-single');
    var btnRecurring   = document.getElementById('admin-sched-recurring');
    var bodyBlockerSingle = document.getElementById('admin-bm-body-blocker-single');
    var bodyBookingSingle = document.getElementById('admin-bm-body-booking-single');
    var bodyRecurring         = document.getElementById('admin-bm-body-recurring');

    if (btnBlocker) btnBlocker.className = (_modalType === 'blocker') ? 'bm-btn-primary' : 'bm-btn-secondary';
    if (btnBooking) btnBooking.className = (_modalType === 'booking') ? 'bm-btn-primary' : 'bm-btn-secondary';
    if (btnSingle)  btnSingle.className  = (_modalSched === 'single') ? 'bm-btn-primary' : 'bm-btn-secondary';
    if (btnRecurring)   btnRecurring.className   = (_modalSched === 'recurring')  ? 'bm-btn-primary' : 'bm-btn-secondary';

    if (bodyBlockerSingle) bodyBlockerSingle.hidden = !(_modalType === 'blocker' && _modalSched === 'single');
    if (bodyBookingSingle) bodyBookingSingle.hidden = !(_modalType === 'booking' && _modalSched === 'single');
    if (bodyRecurring)         bodyRecurring.hidden         = (_modalSched !== 'recurring');
  }

  function prefillRecurringForm() {
    if (!selectedSlot) return;
    setEl('admin-bm-recurring-start', toLocalDateStr(selectedSlot.day));
    setEl('admin-bm-recurring-end', toLocalDateStr(selectedSlot.day));
    setEl('admin-bm-recurring-weekday', String(selectedSlot.day.getDay()));
    var startTime = minutesToTimeStr(selectedSlot.startMin);
    var endTime = minutesToTimeStr(selectedSlot.endMin);
    setEl('admin-bm-recurring-start-time', startTime);
    setEl('admin-bm-recurring-end-time', endTime);
    if (_dotNet) _dotNet.invokeMethodAsync('SetRecurringTimes', startTime, endTime);
  }

  function clearSelectedMember() {
    setEl('admin-bm-member-id', '');
    setEl('admin-bm-member-search', '');
    hideMemberResults();
    var selected = document.getElementById('admin-bm-member-selected');
    if (selected) selected.hidden = true;
  }

  function hideMemberResults() {
    var results = document.getElementById('admin-bm-member-results');
    if (results) results.hidden = true;
  }

  function selectMember(m) {
    var name = m.name
      ? m.name + ' (' + (m.contactFirstName + ' ' + m.contactLastName).trim() + ')'
      : (m.contactFirstName + ' ' + m.contactLastName).trim();
    setEl('admin-bm-member-id', String(m.id));
    setEl('admin-bm-member-search', '');
    hideMemberResults();
    var badgeName = document.getElementById('admin-bm-member-badge-name');
    if (badgeName) badgeName.textContent = name;
    var badgeEmail = document.getElementById('admin-bm-member-badge-email');
    if (badgeEmail) badgeEmail.textContent = m.email;
    var selected = document.getElementById('admin-bm-member-selected');
    if (selected) selected.hidden = false;
  }

  function showMemberResults(members) {
    var results = document.getElementById('admin-bm-member-results');
    if (!results) return;
    results.innerHTML = '';
    if (!members || members.length === 0) {
      var empty = document.createElement('div');
      empty.className = 'bm-search-result bm-search-result--empty';
      empty.textContent = 'Keine Mitglieder gefunden';
      results.appendChild(empty);
      results.hidden = false;
      return;
    }
    members.forEach(function (m) {
      var el = document.createElement('div');
      el.className = 'bm-search-result';
      var name = m.name
        ? m.name + ' (' + escHtml(m.contactFirstName + ' ' + m.contactLastName) + ')'
        : escHtml((m.contactFirstName + ' ' + m.contactLastName).trim());
      el.innerHTML = '<strong>' + name + '</strong><br><small class="text-muted">' + escHtml(m.email) + '</small>';
      el.addEventListener('click', function () { selectMember(m); });
      results.appendChild(el);
    });
    results.hidden = false;
  }

  function escHtml(str) {
    return String(str || '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  function initMemberSearch() {
    var searchInput = document.getElementById('admin-bm-member-search');
    if (!searchInput) return;

    addHandler(searchInput, 'input', function () {
      clearTimeout(_memberSearchTimer);
      var q = searchInput.value.trim();
      if (q.length < 2) { hideMemberResults(); return; }
      _memberSearchTimer = setTimeout(function () {
        if (!_dotNet) return;
        _dotNet.invokeMethodAsync('SearchMembersAsync', q)
          .then(function (json) { showMemberResults(JSON.parse(json)); })
          .catch(function () { hideMemberResults(); });
      }, 300);
    });

    addHandler(searchInput, 'blur', function () {
      setTimeout(hideMemberResults, 200);
    });

    var clearBtn = document.getElementById('admin-bm-member-clear');
    addHandler(clearBtn, 'click', clearSelectedMember);
  }

  function submitAdminBooking() {
    if (!selectedSlot || !_dotNet) return;
    hideError(_modalType === 'blocker' ? 'admin-bm-error-blocker' : 'admin-bm-error');
    var payload;
    if (_modalType === 'blocker') {
      var title = getVal('admin-bm-blocker-title');
      if (!title) { showError('admin-bm-error-blocker', 'Bezeichnung ist erforderlich.'); return; }
      var bt = adminSlotTimesUtc('admin-bm-blocker-start', 'admin-bm-blocker-end');
      if (!bt) { showError('admin-bm-error-blocker', 'Ungültige Zeit: «Bis» muss nach «Von» liegen.'); return; }
      payload = {
        isBlocker: true,
        startUtc: bt.startUtc,
        endUtc: bt.endUtc,
        title: title,
        notes: getVal('admin-bm-notes-blocker') || null
      };
      disableSubmit('admin-bm-submit-blocker', 'Speichern…');
    } else {
      var memberId = getVal('admin-bm-member-id');
      var eventTitle = getVal('admin-bm-event');
      if (!memberId) { showError('admin-bm-error', 'Bitte wähle einen Mieter aus.'); return; }
      if (!eventTitle) { showError('admin-bm-error', 'Anlass ist erforderlich.'); return; }
      var bkt = adminSlotTimesUtc('admin-bm-booking-start', 'admin-bm-booking-end');
      if (!bkt) { showError('admin-bm-error', 'Ungültige Zeit: «Bis» muss nach «Von» liegen.'); return; }
      payload = {
        isBlocker: false,
        startUtc: bkt.startUtc,
        endUtc: bkt.endUtc,
        eventTitle: eventTitle,
        memberId: parseInt(memberId, 10),
        notes: getVal('admin-bm-notes') || null
      };
      disableSubmit('admin-bm-submit', 'Wird gespeichert…');
    }

    _dotNet.invokeMethodAsync('CreateSlotAsync', JSON.stringify(payload))
      .then(function (resultJson) {
        var result = JSON.parse(resultJson);
        if (result.ok) {
          var bodyBlockerSingle = document.getElementById('admin-bm-body-blocker-single');
          var bodyBookingSingle = document.getElementById('admin-bm-body-booking-single');
          var success = document.getElementById('admin-bm-success');
          var successTitle = document.getElementById('admin-bm-success-title');
          var successMsg = document.getElementById('admin-bm-success-msg');
          if (bodyBlockerSingle) bodyBlockerSingle.hidden = true;
          if (bodyBookingSingle) bodyBookingSingle.hidden = true;
          if (successTitle) successTitle.textContent = result.isBlocker ? 'Blocker gespeichert' : 'Buchung erfasst';
          if (successMsg) successMsg.textContent = result.isBlocker
            ? 'Der Blocker erscheint nun im Kalender.'
            : 'Die Buchung wurde erfasst und ist direkt bestätigt.';
          if (success) success.hidden = false;
          var title = document.getElementById('admin-bm-title');
          if (title) title.textContent = result.isBlocker ? 'Blocker gespeichert' : 'Buchung erfasst';
          clearSelectionOverlay();
          selectedSlot = null;
          loadWeek();
        } else {
          if (_modalType === 'blocker') {
            enableSubmit('admin-bm-submit-blocker', 'Blocker speichern');
            showError('admin-bm-error-blocker', result.error || 'Fehler beim Speichern.');
          } else {
            enableSubmit('admin-bm-submit', 'Buchung erfassen');
            showError('admin-bm-error', result.error || 'Fehler beim Speichern.');
          }
        }
      })
      .catch(function () {
        if (_modalType === 'blocker') {
          enableSubmit('admin-bm-submit-blocker', 'Blocker speichern');
          showError('admin-bm-error-blocker', 'Verbindungsfehler. Bitte erneut versuchen.');
        } else {
          enableSubmit('admin-bm-submit', 'Buchung erfassen');
          showError('admin-bm-error', 'Verbindungsfehler. Bitte erneut versuchen.');
        }
      });
  }

  function submitRecurring() {
    if (!selectedSlot || !_dotNet) return;
    hideError('admin-bm-error-recurring');
    hideRecurringConflicts();
    _recurringPayload = null;

    var title = getVal('admin-bm-recurring-title');
    var seriesStart = getVal('admin-bm-recurring-start');
    var seriesEnd = getVal('admin-bm-recurring-end');
    var startTime = getVal('admin-bm-recurring-start-time');
    var endTime = getVal('admin-bm-recurring-end-time');
    if (!title) { showError('admin-bm-error-recurring', 'Bezeichnung ist erforderlich.'); return; }
    if (!seriesStart) { showError('admin-bm-error-recurring', 'Serie-Beginn ist erforderlich.'); return; }
    if (!seriesEnd) { showError('admin-bm-error-recurring', 'Serie-Ende ist erforderlich.'); return; }
    if (seriesEnd < seriesStart) { showError('admin-bm-error-recurring', 'Serie-Ende muss nach Serie-Beginn liegen.'); return; }
    if (!startTime || !endTime) { showError('admin-bm-error-recurring', 'Zeiten sind erforderlich.'); return; }
    if (endTime <= startTime) { showError('admin-bm-error-recurring', 'Endzeit muss nach Startzeit liegen.'); return; }

    var payload = {
      title: title,
      weekday: parseInt(getVal('admin-bm-recurring-weekday'), 10),
      startTime: startTime,
      endTime: endTime,
      seriesStart: seriesStart,
      seriesEnd: seriesEnd,
      notes: getVal('admin-bm-recurring-notes') || null,
      isBlocker: _modalType === 'blocker'
    };

    disableSubmit('admin-bm-submit-recurring', 'Prüfen…');

    _dotNet.invokeMethodAsync('CheckRecurringConflictsAsync', JSON.stringify(payload))
      .then(function (resultJson) {
        var result = JSON.parse(resultJson);
        if (result.error) {
          enableSubmit('admin-bm-submit-recurring', 'Serientermine erstellen');
          showError('admin-bm-error-recurring', result.error);
          return;
        }
        if (!result.conflicts || result.conflicts.length === 0) {
          doCreateRecurring(payload, false);
          return;
        }
        // Konflikte gefunden → anzeigen
        _recurringPayload = payload;
        enableSubmit('admin-bm-submit-recurring', 'Serientermine erstellen');
        var panel = document.getElementById('admin-bm-recurring-conflicts');
        var text = document.getElementById('admin-bm-recurring-conflict-text');
        if (text) {
          var shown = result.conflicts.slice(0, 3).map(function (c) { return c.label; }).join(', ');
          var extra = result.conflicts.length > 3 ? ' … und ' + (result.conflicts.length - 3) + ' weitere' : '';
          text.textContent = result.conflicts.length + ' Termin(e) mit Konflikten: ' + shown + extra + '.';
        }
        if (panel) panel.hidden = false;
      })
      .catch(function () {
        enableSubmit('admin-bm-submit-recurring', 'Serientermine erstellen');
        showError('admin-bm-error-recurring', 'Verbindungsfehler. Bitte erneut versuchen.');
      });
  }

  function doCreateRecurring(payload, skipConflicts) {
    disableSubmit('admin-bm-submit-recurring', 'Wird gespeichert…');
    _dotNet.invokeMethodAsync('CreateRecurringSlotAsync', JSON.stringify(payload), skipConflicts)
      .then(function (resultJson) {
        var result = JSON.parse(resultJson);
        if (result.ok) {
          var bodyRecurring = document.getElementById('admin-bm-body-recurring');
          var success = document.getElementById('admin-bm-success');
          var successTitle = document.getElementById('admin-bm-success-title');
          var successMsg = document.getElementById('admin-bm-success-msg');
          if (bodyRecurring) bodyRecurring.hidden = true;
          var recurringLabel = _modalType === 'blocker' ? 'Blocker-Serie erstellt' : 'Serientermine erstellt';
          if (successTitle) successTitle.textContent = recurringLabel;
          if (successMsg) successMsg.textContent = result.created + ' Termine angelegt' +
            (result.skipped > 0 ? ', ' + result.skipped + ' übersprungen.' : '.');
          if (success) success.hidden = false;
          var title = document.getElementById('admin-bm-title');
          if (title) title.textContent = recurringLabel;
          clearSelectionOverlay();
          selectedSlot = null;
          _recurringPayload = null;
          loadWeek();
        } else {
          enableSubmit('admin-bm-submit-recurring', 'Serientermine erstellen');
          showError('admin-bm-error-recurring', result.error || 'Fehler beim Speichern.');
        }
      })
      .catch(function () {
        enableSubmit('admin-bm-submit-recurring', 'Serientermine erstellen');
        showError('admin-bm-error-recurring', 'Verbindungsfehler. Bitte erneut versuchen.');
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
    var grid = document.getElementById('calendar-grid');
    if (!grid) return;
    clearSelectionOverlay();
    grid.innerHTML = '<div class="calendar-loading" style="grid-column:1/-1">Lade Kalender …</div>';
    var weekStart = toLocalDateStr(currentMonday);
    _dotNet.invokeMethodAsync('GetWeekSlotsAsync', weekStart)
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
          if (cfg.openingHourStart !== undefined) OPENING_HOUR_START = cfg.openingHourStart;
          if (cfg.openingHourEnd !== undefined) OPENING_HOUR_END = cfg.openingHourEnd;
          TOTAL_BLOCKS = (OPENING_HOUR_END - OPENING_HOUR_START) * (60 / BLOCK_MINUTES);
        }
        callback();
      })
      .catch(function () { callback(); });
  }

  // ── Datepicker ────────────────────────────────────────────────────────────────

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

    ['Mo','Di','Mi','Do','Fr','Sa','So'].forEach(function (d) {
      html += '<div class="dp-weekday">' + d + '</div>';
    });

    var firstDay = new Date(year, month, 1);
    var startPad = (firstDay.getDay() + 6) % 7;
    for (var p = 0; p < startPad; p++) html += '<div class="dp-day dp-day--empty"></div>';

    var daysInMonth = new Date(year, month + 1, 0).getDate();
    for (var d = 1; d <= daysInMonth; d++) {
      var date = new Date(year, month, d);
      var past = isPastDay(date);
      var inW  = isInCurrentWeek(date);
      var tod  = isToday(date);

      var cls = 'dp-day';
      if (past) cls += ' dp-day--past';
      else      cls += ' dp-day--bookable';
      if (inW)  cls += ' dp-day--in-week';
      if (tod)  cls += ' dp-day--today';

      var attr = !past ? (' data-date="' + toLocalDateStr(date) + '"') : '';
      html += '<div class="' + cls + '"' + attr + '>' + d + '</div>';
    }
    html += '</div>';

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
    initCalendar: function (dotNetRef) {
      _dotNet = dotNetRef;
      currentMonday = getMonday(new Date());
      lastSlots = [];
      dragState = null;
      selectionEl = null;
      selectedSlot = null;
      isDragging = false;

      addHandler(document.getElementById('prev-week'), 'click', function () { navigateWeek(-1); });
      addHandler(document.getElementById('next-week'), 'click', function () { navigateWeek(+1); });

      addHandler(document, 'mousemove', onDragMove);
      addHandler(document, 'touchmove', onDragMove, { passive: false });
      addHandler(document, 'mouseup', onDragEnd);
      addHandler(document, 'touchend', onDragEnd);

      var grid = document.getElementById('calendar-grid');
      addHandler(grid, 'mousedown', onDragStart);
      addHandler(grid, 'touchstart', onDragStart, { passive: false });

      // Slot-Panel-Button
      addHandler(document.getElementById('admin-btn-create'), 'click', openAdminModal);

      // Modal-Buttons
      addHandler(document.getElementById('admin-bm-close'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-cancel'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-cancel2'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-submit-blocker'), 'click', submitAdminBooking);
      addHandler(document.getElementById('admin-bm-submit'), 'click', submitAdminBooking);
      addHandler(document.getElementById('admin-bm-done'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-cancel-recurring'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-submit-recurring'), 'click', submitRecurring);
      addHandler(document.getElementById('admin-bm-recurring-retry'), 'click', function () {
        hideRecurringConflicts();
        _recurringPayload = null;
      });
      addHandler(document.getElementById('admin-bm-recurring-skip'), 'click', function () {
        if (_recurringPayload) { hideRecurringConflicts(); doCreateRecurring(_recurringPayload, true); }
      });
      addHandler(document.getElementById('admin-type-blocker'), 'click', function () { setModalType('blocker'); });
      addHandler(document.getElementById('admin-type-booking'), 'click', function () { setModalType('booking'); });
      addHandler(document.getElementById('admin-sched-single'), 'click', function () { setModalSched('single'); });
      addHandler(document.getElementById('admin-sched-recurring'), 'click', function () { setModalSched('recurring'); });
      initMemberSearch();

      var modal = document.getElementById('admin-booking-modal');
      addHandler(modal, 'click', function (e) { if (e.target === modal) closeAdminModal(); });
      addHandler(document, 'keydown', function (e) { if (e.key === 'Escape') { closeAdminModal(); closeDatePicker(); } });

      addHandler(document.getElementById('dp-toggle'), 'click', function (e) {
        e.stopPropagation();
        toggleDatePicker();
      });
      addHandler(document, 'click', function (e) {
        if (!dpOpen) return;
        var popup = document.getElementById('date-picker');
        var toggle = document.getElementById('dp-toggle');
        if (popup && !popup.contains(e.target) && toggle && !toggle.contains(e.target)) {
          closeDatePicker();
        }
      });

      addHandler(window, 'resize', function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function () {
          var g = document.getElementById('calendar-grid');
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

    destroyCalendar: function () {
      closeDatePicker();
      removeAllHandlers();
      _dotNet = null;
      lastSlots = [];
      dragState = null;
      selectionEl = null;
      selectedSlot = null;
      isDragging = false;
      _recurringPayload = null;
    },

    // ── Edit-Dialog Tageskalender ────────────────────────────────────────────────
    // JS renders the full single-day grid (cal-time + cal-cell rows, booking overlays,
    // and selection overlay). Blazor container stays empty to avoid DOM ownership conflict.
    initEditCalendarDay: function (el, dotNetRef, slots, calStart, calEnd, selStart, selEnd) {
      // el is the DOM element passed via Blazor ElementReference
      if (_docEditMouseUp) {
        document.removeEventListener('mouseup', _docEditMouseUp);
        _docEditMouseUp = null;
      }
      if (_docEditMouseMove) {
        document.removeEventListener('mousemove', _docEditMouseMove);
        _docEditMouseMove = null;
      }
      editSelEl = null;

      if (!el) return;

      if (el._ecDown) el.removeEventListener('mousedown', el._ecDown);
      if (el._ecTs)   el.removeEventListener('touchstart', el._ecTs);
      if (el._ecTm)   el.removeEventListener('touchmove',  el._ecTm);
      if (el._ecTe)   el.removeEventListener('touchend',   el._ecTe);

      var CELL_STEP_EDIT = 21; // 20px cell + 1px gap
      var totalBlocks = (calEnd - calStart) * 2;

      // ── Render grid ────────────────────────────────────────────────────────
      el.innerHTML = '';
      el.style.cssText = 'display:grid;grid-template-columns:48px 1fr;gap:1px;background:#e0e0e0;border-radius:6px;overflow:hidden;position:relative;user-select:none';

      var bookedBlocks = {};
      for (var b = 0; b < totalBlocks; b++) {
        var isHour = b % 2 === 0;
        var totalMin = calStart * 60 + b * 30;
        var hh = Math.floor(totalMin / 60);

        var timeCell = document.createElement('div');
        timeCell.className = 'cal-time' + (isHour ? ' hour-start' : '');
        timeCell.textContent = isHour ? String(hh).padStart(2, '0') + ':00' : '';
        el.appendChild(timeCell);

        var bodyCell = document.createElement('div');
        bodyCell.className = 'cal-cell' + (isHour ? ' hour-start' : '');
        el.appendChild(bodyCell);
      }

      // ── Booking overlays ──────────────────────────────────────────────────
      slots.forEach(function (slot) {
        var startBlock = Math.round((slot.startMin - calStart * 60) / 30);
        var endBlock   = Math.round((slot.endMin   - calStart * 60) / 30);
        startBlock = Math.max(0, startBlock);
        endBlock   = Math.min(totalBlocks, endBlock);
        var numBlocks = endBlock - startBlock;
        if (numBlocks <= 0) return;

        for (var i = startBlock; i < endBlock; i++) bookedBlocks[i] = true;

        var top    = startBlock * CELL_STEP_EDIT;
        var height = numBlocks * CELL_STEP_EDIT - 1;
        var ov = document.createElement('div');
        ov.className = 'booking-overlay booking-overlay--confirmed';
        ov.style.cssText = 'position:absolute;top:' + top + 'px;left:49px;right:0;height:' + height + 'px;background:' + slot.color + ';border-radius:5px;overflow:hidden;z-index:2;pointer-events:none;box-sizing:border-box;display:flex;align-items:center;padding:0 6px';
        if (height >= 20 && slot.title) {
          var lbl = document.createElement('span');
          lbl.className = 'bol-title';
          lbl.style.cssText = 'font-size:0.68rem;font-weight:600;color:#fff;white-space:nowrap;overflow:hidden;text-overflow:ellipsis';
          lbl.textContent = slot.title;
          ov.appendChild(lbl);
        }
        el.appendChild(ov);
      });

      // ── Selection overlay helpers ─────────────────────────────────────────
      function blockFromClientY(clientY) {
        var rect = el.getBoundingClientRect();
        var relY = clientY - rect.top;
        return Math.max(0, Math.min(totalBlocks - 1, Math.floor(relY / CELL_STEP_EDIT)));
      }

      function renderSel(lo, hi) {
        if (editSelEl) { editSelEl.remove(); editSelEl = null; }
        if (lo < 0 || hi < lo) return;
        var top    = lo * CELL_STEP_EDIT;
        var height = (hi - lo + 1) * CELL_STEP_EDIT - 1;
        var durationMin = (hi - lo + 1) * 30;
        var durationText = durationMin >= 60
          ? (durationMin % 60 === 0
              ? (durationMin / 60) + ' h'
              : Math.floor(durationMin / 60) + ' h ' + (durationMin % 60) + ' min')
          : durationMin + ' min';
        var sel = document.createElement('div');
        sel.className = 'cal-selection';
        sel.style.cssText = 'position:absolute;top:' + top + 'px;left:49px;right:0;height:' + height + 'px';
        sel.innerHTML = '<span class="cal-selection__label">' + durationText + '</span>';
        el.appendChild(sel);
        editSelEl = sel;
      }

      var dragging = false;
      var selStartB = selStart;
      var selEndB   = selEnd;

      // Draw initial selection
      renderSel(selStartB, selEndB);

      // ── Drag listeners ────────────────────────────────────────────────────
      el._ecDown = function (e) {
        if (!e.target.classList.contains('cal-cell')) return;
        var block = blockFromClientY(e.clientY);
        if (bookedBlocks[block]) return;
        dragging = true;
        selStartB = block;
        selEndB   = block;
        renderSel(selStartB, selEndB);
        e.preventDefault();
      };
      el.addEventListener('mousedown', el._ecDown);

      _docEditMouseMove = function (e) {
        if (!dragging) return;
        selEndB = blockFromClientY(e.clientY);
        renderSel(Math.min(selStartB, selEndB), Math.max(selStartB, selEndB));
      };
      document.addEventListener('mousemove', _docEditMouseMove);

      _docEditMouseUp = function () {
        if (!dragging) return;
        dragging = false;
        var lo = Math.min(selStartB, selEndB);
        var hi = Math.max(selStartB, selEndB);
        dotNetRef.invokeMethodAsync('OnCalendarDragEnd', lo, hi);
      };
      document.addEventListener('mouseup', _docEditMouseUp);

      el._ecTs = function (e) {
        var t = e.touches[0];
        var under = document.elementFromPoint(t.clientX, t.clientY);
        if (!under || !under.classList.contains('cal-cell')) return;
        var block = blockFromClientY(t.clientY);
        if (bookedBlocks[block]) return;
        dragging = true;
        selStartB = block;
        selEndB   = block;
        renderSel(selStartB, selEndB);
      };
      el.addEventListener('touchstart', el._ecTs, { passive: true });

      el._ecTm = function (e) {
        if (!dragging) return;
        var t = e.touches[0];
        selEndB = blockFromClientY(t.clientY);
        renderSel(Math.min(selStartB, selEndB), Math.max(selStartB, selEndB));
      };
      el.addEventListener('touchmove', el._ecTm, { passive: true });

      el._ecTe = function () {
        if (!dragging) return;
        dragging = false;
        var lo = Math.min(selStartB, selEndB);
        var hi = Math.max(selStartB, selEndB);
        dotNetRef.invokeMethodAsync('OnCalendarDragEnd', lo, hi);
      };
      el.addEventListener('touchend', el._ecTe);

      // Scroll initial selection into view
      setTimeout(function () {
        if (editSelEl) editSelEl.scrollIntoView({ block: 'center', behavior: 'instant' });
      }, 50);
    },

    destroyEditCalendar: function () {
      if (_docEditMouseUp) {
        document.removeEventListener('mouseup', _docEditMouseUp);
        _docEditMouseUp = null;
      }
      if (_docEditMouseMove) {
        document.removeEventListener('mousemove', _docEditMouseMove);
        _docEditMouseMove = null;
      }
      if (editSelEl) { editSelEl.remove(); editSelEl = null; }
    }
  };
})();

window.bookingGetElementRect = function (element) {
  var r = element.getBoundingClientRect();
  return { left: r.left, top: r.top, right: r.right, bottom: r.bottom, width: r.width, height: r.height, viewportWidth: window.innerWidth };
};
