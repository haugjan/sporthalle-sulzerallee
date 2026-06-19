(function () {
  'use strict';

  // --- Konfiguration (wird in Phase 6 aus Umbraco geladen) ---
  var OPENING_HOUR_START = 7;
  var OPENING_HOUR_END = 23;
  var BLOCK_MINUTES = 30;
  var TOTAL_BLOCKS = (OPENING_HOUR_END - OPENING_HOUR_START) * (60 / BLOCK_MINUTES);

  var currentMonday = getMonday(new Date());
  var lastSlots = [];

  // --- Datum-Hilfsfunktionen ---

  function getMonday(d) {
    var date = new Date(d);
    date.setHours(0, 0, 0, 0);
    var day = date.getDay(); // 0=So, 1=Mo, …, 6=Sa
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
    return dayNames[date.getDay()] + ' ' + d + '.' + m + '.';
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

  // --- Zeitzonen-Konvertierung UTC → Europe/Zurich ---

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
    // Stunde 24 → 0 (Intl-Eigenart bei Mitternacht)
    if (p.hour === 24) p.hour = 0;
    return p;
  }

  // Gibt true zurück wenn der Slot die gegebene Zelle (Tag + Block-Index) belegt.
  function slotOccupiesCell(slot, dayDate, blockIdx) {
    var slotStartUtc = new Date(slot.startUtc);
    var slotEndUtc = new Date(slot.endUtc);

    var startZ = getZurichParts(slotStartUtc);
    var endZ = getZurichParts(slotEndUtc);

    // Prüfen ob Slot-Start am gleichen Tag liegt
    if (startZ.year !== dayDate.getFullYear() ||
      startZ.month !== dayDate.getMonth() + 1 ||
      startZ.day !== dayDate.getDate()) {
      return false;
    }

    var cellStartMin = OPENING_HOUR_START * 60 + blockIdx * BLOCK_MINUTES;
    var cellEndMin = cellStartMin + BLOCK_MINUTES;
    var slotStartMin = startZ.hour * 60 + startZ.minute;
    var slotEndMin = endZ.hour * 60 + endZ.minute;

    return slotStartMin < cellEndMin && slotEndMin > cellStartMin;
  }

  // Gibt das passende Slot-Objekt für eine Zelle zurück (oder null).
  function findSlotForCell(slots, dayDate, blockIdx) {
    for (var i = 0; i < slots.length; i++) {
      if (slotOccupiesCell(slots[i], dayDate, blockIdx)) return slots[i];
    }
    return null;
  }

  // CSS-Klasse für eine belegte Zelle
  function slotCssClass(slot) {
    if (slot.isRecurringSlot) return 'status-recurring';
    if (slot.status === 'Bestätigt') return 'status-confirmed';
    if (slot.status === 'Provisorisch') return 'status-provisional';
    return 'status-confirmed';
  }

  // --- Kalender-Rendering ---

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
      cell.innerHTML = formatDayHeader(day);
      grid.appendChild(cell);
    });

    // Zeit-Zeilen
    for (var b = 0; b < TOTAL_BLOCKS; b++) {
      var isHourStart = b % 2 === 0;
      var timeLabel = document.createElement('div');
      timeLabel.className = 'cal-time' + (isHourStart ? ' hour-start' : '');
      timeLabel.textContent = isHourStart ? blockToTimeLabel(b) : '';
      grid.appendChild(timeLabel);

      days.forEach(function (day) {
        var cell = document.createElement('div');
        cell.className = 'cal-cell' + (isHourStart ? ' hour-start' : '');

        var past = isPastDay(day);
        if (past) {
          cell.classList.add('is-past');
        } else {
          var slot = findSlotForCell(slots, day, b);
          if (slot) {
            cell.classList.add(slotCssClass(slot));
            // Admin-definierte Farbe für Dauerbelegungen
            if (slot.isRecurringSlot && slot.color && slot.color !== '#666666') {
              cell.style.backgroundColor = slot.color;
              cell.style.opacity = '0.85';
            }
          }
        }
        grid.appendChild(cell);
      });
    }
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

    updateWeekLabel();
    loadWeek();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
