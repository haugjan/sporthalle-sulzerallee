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

  var _modalTyp = 'blocker'; // 'blocker' | 'buchung'
  var _modalSched = 'einzel'; // 'einzel' | 'serie'
  var _selectedColor = '#F1C40F';
  var _selectedSerieColor = '#C62828';
  var _seriePayload = null;
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

  function selectColor(color) {
    _selectedColor = color;
    var picker = document.getElementById('admin-bm-color-picker');
    if (!picker) return;
    var swatches = picker.querySelectorAll('.bm-color-swatch');
    for (var i = 0; i < swatches.length; i++) {
      if (swatches[i].dataset.color === color) {
        swatches[i].classList.add('bm-color-swatch--active');
      } else {
        swatches[i].classList.remove('bm-color-swatch--active');
      }
    }
  }

  function resetAdminModal() {
    setEl('admin-bm-titel', '');
    setEl('admin-bm-notizen-blocker', '');
    setEl('admin-bm-anlass', '');
    setEl('admin-bm-notizen', '');
    clearSelectedMember();
    selectColor('#F1C40F');
    hideError('admin-bm-error-blocker');
    hideError('admin-bm-error');
    hideError('admin-bm-error-serie');
    hideSerieConflicts();
    _seriePayload = null;
    var success = document.getElementById('admin-bm-success');
    if (success) success.hidden = true;
    _modalTyp = 'blocker';
    _modalSched = 'einzel';
    refreshModalView();
    enableSubmit('admin-bm-submit-blocker', 'Blocker speichern');
    enableSubmit('admin-bm-submit', 'Buchung erfassen');
    enableSubmit('admin-bm-submit-serie', 'Serientermine erstellen');
  }

  function hideSerieConflicts() {
    var panel = document.getElementById('admin-bm-serie-conflicts');
    if (panel) panel.hidden = true;
    var text = document.getElementById('admin-bm-serie-conflict-text');
    if (text) text.textContent = '';
  }

  function setModalTyp(typ) {
    _modalTyp = typ;
    refreshModalView();
  }

  function setModalSched(sched) {
    _modalSched = sched;
    refreshModalView();
    if (sched === 'serie') prefillSerieForm();
  }

  function refreshModalView() {
    var btnBlocker = document.getElementById('admin-typ-blocker');
    var btnBuchung = document.getElementById('admin-typ-buchung');
    var btnEinzel  = document.getElementById('admin-sched-einzel');
    var btnSerie   = document.getElementById('admin-sched-serie');
    var bodyBlockerEinzel = document.getElementById('admin-bm-body-blocker-einzel');
    var bodyBuchungEinzel = document.getElementById('admin-bm-body-buchung-einzel');
    var bodySerie         = document.getElementById('admin-bm-body-serie');
    var serieColorRow     = document.getElementById('admin-bm-serie-color-row');

    if (btnBlocker) btnBlocker.className = (_modalTyp === 'blocker') ? 'bm-btn-primary' : 'bm-btn-secondary';
    if (btnBuchung) btnBuchung.className = (_modalTyp === 'buchung') ? 'bm-btn-primary' : 'bm-btn-secondary';
    if (btnEinzel)  btnEinzel.className  = (_modalSched === 'einzel') ? 'bm-btn-primary' : 'bm-btn-secondary';
    if (btnSerie)   btnSerie.className   = (_modalSched === 'serie')  ? 'bm-btn-primary' : 'bm-btn-secondary';

    if (bodyBlockerEinzel) bodyBlockerEinzel.hidden = !(_modalTyp === 'blocker' && _modalSched === 'einzel');
    if (bodyBuchungEinzel) bodyBuchungEinzel.hidden = !(_modalTyp === 'buchung' && _modalSched === 'einzel');
    if (bodySerie)         bodySerie.hidden         = (_modalSched !== 'serie');
    if (serieColorRow)     serieColorRow.hidden     = (_modalTyp === 'blocker');
  }

  function prefillSerieForm() {
    if (!selectedSlot) return;
    setEl('admin-bm-serie-von-datum', toLocalDateStr(selectedSlot.day));
    setEl('admin-bm-serie-bis-datum', toLocalDateStr(selectedSlot.day));
    setEl('admin-bm-serie-wochentag', String(selectedSlot.day.getDay()));
    setEl('admin-bm-serie-start-time', minutesToTimeStr(selectedSlot.startMin));
    setEl('admin-bm-serie-end-time', minutesToTimeStr(selectedSlot.endMin));
    selectSerieColor('#F1C40F');
  }

  function selectSerieColor(color) {
    _selectedSerieColor = color;
    var picker = document.getElementById('admin-bm-serie-color-picker');
    if (!picker) return;
    var swatches = picker.querySelectorAll('.bm-color-swatch');
    for (var i = 0; i < swatches.length; i++) {
      if (swatches[i].dataset.color === color) {
        swatches[i].classList.add('bm-color-swatch--active');
      } else {
        swatches[i].classList.remove('bm-color-swatch--active');
      }
    }
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
    hideError(_modalTyp === 'blocker' ? 'admin-bm-error-blocker' : 'admin-bm-error');
    var payload;
    if (_modalTyp === 'blocker') {
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
      var memberId = getVal('admin-bm-member-id');
      var anlass = getVal('admin-bm-anlass');
      if (!memberId) { showError('admin-bm-error', 'Bitte wähle einen Mieter aus.'); return; }
      if (!anlass) { showError('admin-bm-error', 'Anlass ist erforderlich.'); return; }
      payload = {
        isBlocker: false,
        startUtc: selectedSlot.startUtcIso,
        endUtc: selectedSlot.endUtcIso,
        anlass: anlass,
        color: _selectedColor,
        memberId: parseInt(memberId, 10),
        notizen: getVal('admin-bm-notizen') || null
      };
      disableSubmit('admin-bm-submit', 'Wird gespeichert…');
    }

    _dotNet.invokeMethodAsync('BuchungAnlegenAsync', JSON.stringify(payload))
      .then(function (resultJson) {
        var result = JSON.parse(resultJson);
        if (result.ok) {
          var bodyBlockerEinzel = document.getElementById('admin-bm-body-blocker-einzel');
          var bodyBuchungEinzel = document.getElementById('admin-bm-body-buchung-einzel');
          var success = document.getElementById('admin-bm-success');
          var successTitle = document.getElementById('admin-bm-success-title');
          var successMsg = document.getElementById('admin-bm-success-msg');
          if (bodyBlockerEinzel) bodyBlockerEinzel.hidden = true;
          if (bodyBuchungEinzel) bodyBuchungEinzel.hidden = true;
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
          if (_modalTyp === 'blocker') {
            enableSubmit('admin-bm-submit-blocker', 'Blocker speichern');
            showError('admin-bm-error-blocker', result.error || 'Fehler beim Speichern.');
          } else {
            enableSubmit('admin-bm-submit', 'Buchung erfassen');
            showError('admin-bm-error', result.error || 'Fehler beim Speichern.');
          }
        }
      })
      .catch(function () {
        if (_modalTyp === 'blocker') {
          enableSubmit('admin-bm-submit-blocker', 'Blocker speichern');
          showError('admin-bm-error-blocker', 'Verbindungsfehler. Bitte erneut versuchen.');
        } else {
          enableSubmit('admin-bm-submit', 'Buchung erfassen');
          showError('admin-bm-error', 'Verbindungsfehler. Bitte erneut versuchen.');
        }
      });
  }

  function submitSerientermin() {
    if (!selectedSlot || !_dotNet) return;
    hideError('admin-bm-error-serie');
    hideSerieConflicts();
    _seriePayload = null;

    var titel = getVal('admin-bm-serie-titel');
    var vonDatum = getVal('admin-bm-serie-von-datum');
    var bisDatum = getVal('admin-bm-serie-bis-datum');
    var startTime = getVal('admin-bm-serie-start-time');
    var endTime = getVal('admin-bm-serie-end-time');
    if (!titel) { showError('admin-bm-error-serie', 'Bezeichnung ist erforderlich.'); return; }
    if (!vonDatum) { showError('admin-bm-error-serie', 'Serie-Beginn ist erforderlich.'); return; }
    if (!bisDatum) { showError('admin-bm-error-serie', 'Serie-Ende ist erforderlich.'); return; }
    if (bisDatum < vonDatum) { showError('admin-bm-error-serie', 'Serie-Ende muss nach Serie-Beginn liegen.'); return; }
    if (!startTime || !endTime) { showError('admin-bm-error-serie', 'Zeiten sind erforderlich.'); return; }
    if (endTime <= startTime) { showError('admin-bm-error-serie', 'Endzeit muss nach Startzeit liegen.'); return; }

    var payload = {
      titel: titel,
      wochentag: parseInt(getVal('admin-bm-serie-wochentag'), 10),
      startTime: startTime,
      endTime: endTime,
      vonDatum: vonDatum,
      bisDatum: bisDatum,
      color: _modalTyp === 'blocker' ? null : (_selectedSerieColor || null),
      notizen: getVal('admin-bm-serie-notizen') || null,
      isBlocker: _modalTyp === 'blocker'
    };

    disableSubmit('admin-bm-submit-serie', 'Prüfen…');

    _dotNet.invokeMethodAsync('SerienTerminPruefenAsync', JSON.stringify(payload))
      .then(function (resultJson) {
        var result = JSON.parse(resultJson);
        if (result.error) {
          enableSubmit('admin-bm-submit-serie', 'Serientermine erstellen');
          showError('admin-bm-error-serie', result.error);
          return;
        }
        if (!result.conflicts || result.conflicts.length === 0) {
          doSerienAnlegen(payload, false);
          return;
        }
        // Konflikte gefunden → anzeigen
        _seriePayload = payload;
        enableSubmit('admin-bm-submit-serie', 'Serientermine erstellen');
        var panel = document.getElementById('admin-bm-serie-conflicts');
        var text = document.getElementById('admin-bm-serie-conflict-text');
        if (text) {
          var shown = result.conflicts.slice(0, 3).map(function (c) { return c.label; }).join(', ');
          var extra = result.conflicts.length > 3 ? ' … und ' + (result.conflicts.length - 3) + ' weitere' : '';
          text.textContent = result.conflicts.length + ' Termin(e) mit Konflikten: ' + shown + extra + '.';
        }
        if (panel) panel.hidden = false;
      })
      .catch(function () {
        enableSubmit('admin-bm-submit-serie', 'Serientermine erstellen');
        showError('admin-bm-error-serie', 'Verbindungsfehler. Bitte erneut versuchen.');
      });
  }

  function doSerienAnlegen(payload, skipConflicts) {
    disableSubmit('admin-bm-submit-serie', 'Wird gespeichert…');
    _dotNet.invokeMethodAsync('SerienTerminAnlegenAsync', JSON.stringify(payload), skipConflicts)
      .then(function (resultJson) {
        var result = JSON.parse(resultJson);
        if (result.ok) {
          var bodySerie = document.getElementById('admin-bm-body-serie');
          var success = document.getElementById('admin-bm-success');
          var successTitle = document.getElementById('admin-bm-success-title');
          var successMsg = document.getElementById('admin-bm-success-msg');
          if (bodySerie) bodySerie.hidden = true;
          var serieLabel = _modalTyp === 'blocker' ? 'Blocker-Serie erstellt' : 'Serientermine erstellt';
          if (successTitle) successTitle.textContent = serieLabel;
          if (successMsg) successMsg.textContent = result.created + ' Termine angelegt' +
            (result.skipped > 0 ? ', ' + result.skipped + ' übersprungen.' : '.');
          if (success) success.hidden = false;
          var title = document.getElementById('admin-bm-title');
          if (title) title.textContent = serieLabel;
          clearSelectionOverlay();
          selectedSlot = null;
          _seriePayload = null;
          loadWeek();
        } else {
          enableSubmit('admin-bm-submit-serie', 'Serientermine erstellen');
          showError('admin-bm-error-serie', result.error || 'Fehler beim Speichern.');
        }
      })
      .catch(function () {
        enableSubmit('admin-bm-submit-serie', 'Serientermine erstellen');
        showError('admin-bm-error-serie', 'Verbindungsfehler. Bitte erneut versuchen.');
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
      addHandler(document.getElementById('admin-btn-erfassen'), 'click', openAdminModal);

      // Modal-Buttons
      addHandler(document.getElementById('admin-bm-close'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-cancel'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-cancel2'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-submit-blocker'), 'click', submitAdminBooking);
      addHandler(document.getElementById('admin-bm-submit'), 'click', submitAdminBooking);
      addHandler(document.getElementById('admin-bm-done'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-cancel-serie'), 'click', closeAdminModal);
      addHandler(document.getElementById('admin-bm-submit-serie'), 'click', submitSerientermin);
      addHandler(document.getElementById('admin-bm-serie-retry'), 'click', function () {
        hideSerieConflicts();
        _seriePayload = null;
      });
      addHandler(document.getElementById('admin-bm-serie-skip'), 'click', function () {
        if (_seriePayload) { hideSerieConflicts(); doSerienAnlegen(_seriePayload, true); }
      });
      addHandler(document.getElementById('admin-typ-blocker'), 'click', function () { setModalTyp('blocker'); });
      addHandler(document.getElementById('admin-typ-buchung'), 'click', function () { setModalTyp('buchung'); });
      addHandler(document.getElementById('admin-sched-einzel'), 'click', function () { setModalSched('einzel'); });
      addHandler(document.getElementById('admin-sched-serie'), 'click', function () { setModalSched('serie'); });
      initMemberSearch();

      var colorPicker = document.getElementById('admin-bm-color-picker');
      if (colorPicker) {
        var swatches = colorPicker.querySelectorAll('.bm-color-swatch');
        for (var ci = 0; ci < swatches.length; ci++) {
          (function (swatch) {
            addHandler(swatch, 'click', function () { selectColor(swatch.dataset.color); });
          })(swatches[ci]);
        }
      }

      var serieColorPicker = document.getElementById('admin-bm-serie-color-picker');
      if (serieColorPicker) {
        var serieSwatches = serieColorPicker.querySelectorAll('.bm-color-swatch');
        for (var si = 0; si < serieSwatches.length; si++) {
          (function (swatch) {
            addHandler(swatch, 'click', function () { selectSerieColor(swatch.dataset.color); });
          })(serieSwatches[si]);
        }
      }

      var modal = document.getElementById('admin-booking-modal');
      addHandler(modal, 'click', function (e) { if (e.target === modal) closeAdminModal(); });
      addHandler(document, 'keydown', function (e) { if (e.key === 'Escape') closeAdminModal(); });

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

    destroyKalender: function () {
      removeAllHandlers();
      _dotNet = null;
      lastSlots = [];
      dragState = null;
      selectionEl = null;
      selectedSlot = null;
      isDragging = false;
      _seriePayload = null;
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
