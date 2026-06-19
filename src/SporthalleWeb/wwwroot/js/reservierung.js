(function () {
  'use strict';

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

  // --- Datum-Hilfsfunktionen ---

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

  function fmtTime(h, m) {
    return String(h).padStart(2, '0') + ':' + String(m).padStart(2, '0');
  }

  function esc(s) {
    return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  // --- Zeitzonen-Konvertierung UTC -> Europe/Zurich ---

  function getZurichParts(utcDate) {
    var parts = new Intl.DateTimeFormat('en-US', {
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

  // --- Kalender-Grid (reine Struktur, keine Slot-Farben) ---

  function renderGrid(slots) {
    var grid = document.getElementById('calendar-grid');
    if (!grid) return;
    grid.innerHTML = '';

    var days = [];
    for (var i = 0; i < 7; i++) days.push(addDays(currentMonday, i));

    // Kopfzeile
    var timeCorner = document.createElement('div');
    timeCorner.className = 'cal-header-time';
    grid.appendChild(timeCorner);

    days.forEach(function (day) {
      var cell = document.createElement('div');
      cell.className = 'cal-header-day';
      if (isToday(day)) cell.classList.add('is-today');
      else if (isPastDay(day)) cell.classList.add('is-past');
      cell.textContent = formatDayHeader(day);
      grid.appendChild(cell);
    });

    // Zeit-Zeilen (immer leer — Buchungen kommen als Overlays)
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
        grid.appendChild(cell);
      });
    }

    // Overlays nach dem Browser-Layout rendern
    requestAnimationFrame(function () {
      renderBookingOverlays(slots, days, grid);
    });
  }

  // --- Booking-Overlays (absolut über dem Grid) ---

  function renderBookingOverlays(slots, days, grid) {
    // Alte Overlays entfernen
    var old = grid.querySelectorAll('.booking-overlay');
    for (var i = 0; i < old.length; i++) old[i].remove();

    var gridRect = grid.getBoundingClientRect();
    var headerCells = grid.querySelectorAll('.cal-header-day');
    if (!headerCells.length) return;

    // Spalten-Positionen (relativ zum Grid-Container)
    var cols = [];
    headerCells.forEach(function (h) {
      var r = h.getBoundingClientRect();
      cols.push({ left: r.left - gridRect.left, width: r.width });
    });

    // Oberkante der ersten Inhaltszeile (unterhalb des Headers + 1px Gap)
    var headerRect = headerCells[0].getBoundingClientRect();
    var contentTop = headerRect.bottom - gridRect.top + CELL_GAP;

    slots.forEach(function (slot) {
      var slotStartUtc = new Date(slot.startUtc);
      var slotEndUtc = new Date(slot.endUtc);
      var startZ = getZurichParts(slotStartUtc);
      var endZ = getZurichParts(slotEndUtc);

      // Tag-Spalte ermitteln
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
      if (dayIdx < 0 || dayIdx >= cols.length) return;
      if (isPastDay(days[dayIdx])) return;

      // Block-Bereich berechnen
      var openStart = OPENING_HOUR_START * 60;
      var slotStartMin = startZ.hour * 60 + startZ.minute;
      var slotEndMin = endZ.hour * 60 + endZ.minute;
      var startBlock = Math.round((slotStartMin - openStart) / BLOCK_MINUTES);
      var endBlock = Math.round((slotEndMin - openStart) / BLOCK_MINUTES);
      startBlock = Math.max(0, startBlock);
      endBlock = Math.min(TOTAL_BLOCKS, endBlock);
      var numBlocks = endBlock - startBlock;
      if (numBlocks <= 0) return;

      // Pixel-Position
      var col = cols[dayIdx];
      var top = contentTop + startBlock * CELL_STEP;
      var height = numBlocks * CELL_STEP - CELL_GAP;
      var left = col.left + 2;
      var width = col.width - 4;

      var el = document.createElement('div');
      el.className = 'booking-overlay booking-overlay--' + getOverlayMod(slot);
      el.style.top = top + 'px';
      el.style.height = height + 'px';
      el.style.left = left + 'px';
      el.style.width = width + 'px';

      if (slot.isRecurringSlot && slot.color) {
        el.style.background = slot.color;
        el.style.opacity = '0.9';
      }

      if (height >= 16) {
        var label = document.createElement('div');
        label.className = 'booking-overlay__label';
        var titleEl = document.createElement('span');
        titleEl.className = 'bol-title';
        titleEl.textContent = slot.eventType || '';
        label.appendChild(titleEl);
        if (height >= 40) {
          var timeEl = document.createElement('span');
          timeEl.className = 'bol-time';
          timeEl.textContent = fmtTime(startZ.hour, startZ.minute) + '–' + fmtTime(endZ.hour, endZ.minute);
          label.appendChild(timeEl);
        }
        el.appendChild(label);
      }

      grid.appendChild(el);
    });
  }

  function getOverlayMod(slot) {
    if (slot.isRecurringSlot) return 'recurring';
    if (slot.status === 'Provisorisch') return 'provisional';
    return 'confirmed';
  }

  // --- Daten laden ---

  function loadWeek() {
    var grid = document.getElementById('calendar-grid');
    if (!grid) return;

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
      })
      .catch(function (err) {
        grid.innerHTML = '<div class="calendar-loading" style="grid-column:1/-1">Kalender konnte nicht geladen werden.</div>';
        console.error('Reservierung: Fehler beim Laden der Slots', err);
      });
  }

  // --- Wochen-Navigation ---

  function navigateWeek(delta) {
    currentMonday = addDays(currentMonday, delta * 7);
    updateWeekLabel();
    loadWeek();
  }

  function updateWeekLabel() {
    var label = document.getElementById('week-label');
    if (label) label.textContent = formatWeekLabel(currentMonday);
  }

  // --- Init ---

  function init() {
    var prevBtn = document.getElementById('prev-week');
    var nextBtn = document.getElementById('next-week');

    if (prevBtn) prevBtn.addEventListener('click', function () { navigateWeek(-1); });
    if (nextBtn) nextBtn.addEventListener('click', function () { navigateWeek(+1); });

    // Overlays bei Fenster-Resize neu positionieren
    window.addEventListener('resize', function () {
      clearTimeout(resizeTimer);
      resizeTimer = setTimeout(function () {
        var grid = document.getElementById('calendar-grid');
        if (grid && lastSlots.length) {
          var days = [];
          for (var i = 0; i < 7; i++) days.push(addDays(currentMonday, i));
          renderBookingOverlays(lastSlots, days, grid);
        }
      }, 150);
    });

    updateWeekLabel();
    loadWeek();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
