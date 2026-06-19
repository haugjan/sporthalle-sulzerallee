(function () {
  'use strict';

  // ── Constants ────────────────────────────────────────────────────────────────
  const COLS = 20, ROWS = 15, TOTAL = 300;
  const CELL_W = 40, CELL_H = 400 / 15; // 26.666...
  const FIELD_X = 20, FIELD_Y = 20;

  // VIP field sets (must match server-side VipField.cs)
  const GOAL_CREASE_LEFT  = range(1, 3, 5, 9);
  const GOAL_CREASE_RIGHT = range(16, 18, 5, 9);
  const CENTER_CIRCLE     = range(8, 11, 5, 9);
  const FACE_OFF_SPOTS    = new Set([45, 243, 58, 258]);

  function range(cFrom, cTo, rFrom, rTo) {
    const s = new Set();
    for (let r = rFrom; r <= rTo; r++)
      for (let c = cFrom; c <= cTo; c++)
        s.add(r * COLS + c + 1);
    return s;
  }

  function vipLabel(n) {
    if (GOAL_CREASE_LEFT.has(n) || GOAL_CREASE_RIGHT.has(n)) return 'Torraum';
    if (CENTER_CIRCLE.has(n)) return 'Anspielkreis';
    if (FACE_OFF_SPOTS.has(n)) return 'Anspielpunkt';
    return null;
  }

  // ── State ────────────────────────────────────────────────────────────────────
  let occupiedFields = new Set();
  let selectedField  = null;
  let currentStep    = 1;
  const TOTAL_STEPS  = 6;

  const formData = {
    fieldNumber: null,
    levelKey: null,
    firstName: '', lastName: '', email: '',
    addressLine: '', postalCode: '', city: '',
    showNameOnFloor: false, displayName: '',
    consent: false, captchaToken: ''
  };

  // ── DOM refs ─────────────────────────────────────────────────────────────────
  const svgContainer   = document.getElementById('pm-svg-container');
  const counter        = document.getElementById('pm-counter');
  const wizardOverlay  = document.getElementById('pm-wizard-overlay');
  const wizardModal    = document.getElementById('pm-wizard-modal');
  const wizardClose    = document.getElementById('pm-wizard-close');
  const stepBtnsNext   = document.querySelectorAll('.pm-btn-next');
  const stepBtnsBack   = document.querySelectorAll('.pm-btn-back');
  const submitBtn      = document.getElementById('pm-btn-submit');
  const successMsg     = document.getElementById('pm-success');
  const errorMsg       = document.getElementById('pm-error');

  // ── Init ─────────────────────────────────────────────────────────────────────
  loadFieldStatuses().then(buildGrid);

  if (wizardClose) wizardClose.addEventListener('click', closeWizard);
  if (wizardOverlay) wizardOverlay.addEventListener('click', function (e) {
    if (e.target === wizardOverlay) closeWizard();
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeWizard();
  });

  stepBtnsNext.forEach(btn => btn.addEventListener('click', nextStep));
  stepBtnsBack.forEach(btn => btn.addEventListener('click', prevStep));
  if (submitBtn) submitBtn.addEventListener('click', submitForm);

  // ── Field loading ─────────────────────────────────────────────────────────────
  async function loadFieldStatuses() {
    try {
      const res = await fetch('/api/passivmitglieder/felder');
      if (!res.ok) return;
      const data = await res.json();
      occupiedFields = new Set(data.occupiedFields.map(f => f.fieldNumber));
      updateCounter(data.occupiedCount, data.totalFields);
    } catch (_) { /* silent — grid still renders, just shows all free */ }
  }

  function updateCounter(occupied, total) {
    if (!counter) return;
    counter.textContent = `${occupied} von ${total} Feldern belegt`;
  }

  // ── SVG Grid ─────────────────────────────────────────────────────────────────
  function buildGrid() {
    if (!svgContainer) return;

    // Load the field SVG as background image via <image> tag in an overlay SVG
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('viewBox', '0 0 840 440');
    svg.setAttribute('id', 'pm-field-svg');
    svg.setAttribute('role', 'img');
    svg.setAttribute('aria-label', 'Unihockey-Spielfeld – Feld auswählen');

    // Background field image
    const img = document.createElementNS('http://www.w3.org/2000/svg', 'image');
    img.setAttribute('href', '/media/unihockey-boden.svg');
    img.setAttribute('x', '0');
    img.setAttribute('y', '0');
    img.setAttribute('width', '840');
    img.setAttribute('height', '440');
    svg.appendChild(img);

    // Grid cells
    for (let r = 0; r < ROWS; r++) {
      for (let c = 0; c < COLS; c++) {
        const n = r * COLS + c + 1;
        const x = FIELD_X + c * CELL_W;
        const y = FIELD_Y + r * CELL_H;
        const label = vipLabel(n);
        const isOccupied = occupiedFields.has(n);
        const isVip = label !== null;

        const cell = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
        cell.setAttribute('x', x);
        cell.setAttribute('y', y);
        cell.setAttribute('width', CELL_W);
        cell.setAttribute('height', CELL_H);
        cell.setAttribute('data-field', n);
        cell.setAttribute('class', cellClass(isOccupied, isVip, false));
        cell.setAttribute('tabindex', isOccupied ? '-1' : '0');
        cell.setAttribute('role', 'button');
        cell.setAttribute('aria-label', cellAriaLabel(n, label, isOccupied));

        if (!isOccupied) {
          cell.addEventListener('click', () => selectField(n, cell));
          cell.addEventListener('keydown', e => {
            if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); selectField(n, cell); }
          });
        }

        if (label) {
          const title = document.createElementNS('http://www.w3.org/2000/svg', 'title');
          title.textContent = `Feld ${n} – ${label}`;
          cell.appendChild(title);
        }

        svg.appendChild(cell);
      }
    }

    svgContainer.appendChild(svg);
  }

  function cellClass(occupied, vip, selected) {
    if (selected) return 'pm-cell pm-cell--selected';
    if (occupied) return 'pm-cell pm-cell--occupied';
    if (vip)      return 'pm-cell pm-cell--vip';
    return 'pm-cell pm-cell--free';
  }

  function cellAriaLabel(n, label, occupied) {
    const base = label ? `Feld ${n} (${label})` : `Feld ${n}`;
    return occupied ? `${base} – belegt` : `${base} – frei, auswählen`;
  }

  function selectField(n, cellEl) {
    // Deselect previous
    if (selectedField !== null) {
      const prev = document.querySelector(`[data-field="${selectedField}"]`);
      if (prev) {
        const wasVip = vipLabel(selectedField) !== null;
        prev.setAttribute('class', cellClass(false, wasVip, false));
      }
    }
    selectedField = n;
    formData.fieldNumber = n;
    cellEl.setAttribute('class', cellClass(false, false, true));

    openWizard();
  }

  // ── Wizard ───────────────────────────────────────────────────────────────────
  function openWizard() {
    if (!wizardOverlay) return;
    currentStep = 1;
    showStep(1);
    updateFieldPreview();
    wizardOverlay.classList.add('pm-wizard--open');
    document.body.style.overflow = 'hidden';
    wizardModal.querySelector('.pm-wizard__body').scrollTop = 0;
  }

  function closeWizard() {
    if (!wizardOverlay) return;
    wizardOverlay.classList.remove('pm-wizard--open');
    document.body.style.overflow = '';
  }

  function showStep(step) {
    document.querySelectorAll('.pm-step').forEach(el => {
      el.classList.toggle('pm-step--active', parseInt(el.dataset.step) === step);
    });
    document.querySelectorAll('.pm-progress__dot').forEach(el => {
      const s = parseInt(el.dataset.step);
      el.classList.toggle('pm-progress__dot--done', s < step);
      el.classList.toggle('pm-progress__dot--active', s === step);
    });
    const isLast = step === TOTAL_STEPS;
    stepBtnsNext.forEach(b => { b.style.display = isLast ? 'none' : ''; });
    if (submitBtn) submitBtn.style.display = isLast ? '' : 'none';
    const label = document.getElementById('pm-step-label');
    if (label) label.textContent = `Schritt ${step} von ${TOTAL_STEPS}`;
    clearStepError();
  }

  function nextStep() {
    if (!validateStep(currentStep)) return;
    collectStep(currentStep);
    if (currentStep === TOTAL_STEPS - 1) buildSummary();
    currentStep++;
    showStep(currentStep);
    wizardModal.querySelector('.pm-wizard__body').scrollTop = 0;
  }

  function prevStep() {
    if (currentStep <= 1) { closeWizard(); return; }
    currentStep--;
    showStep(currentStep);
  }

  function validateStep(step) {
    clearStepError();
    switch (step) {
      case 1: // field already chosen by click — always valid
        return true;
      case 2:
        if (!document.querySelector('input[name="levelKey"]:checked')) {
          showStepError('Bitte wähle eine Mitgliedsstufe.');
          return false;
        }
        return true;
      case 3: {
        const fn = v('pm-firstName'), ln = v('pm-lastName'), em = v('pm-email');
        if (!fn || !ln || !em) { showStepError('Bitte fülle alle Pflichtfelder aus.'); return false; }
        if (!em.includes('@')) { showStepError('Bitte gib eine gültige E-Mail-Adresse ein.'); return false; }
        return true;
      }
      case 4: {
        const addr = v('pm-addressLine'), plz = v('pm-postalCode'), city = v('pm-city');
        if (!addr || !plz || !city) { showStepError('Bitte fülle alle Pflichtfelder aus.'); return false; }
        return true;
      }
      case 5: {
        if (!document.getElementById('pm-consent')?.checked) {
          showStepError('Bitte stimme den Bedingungen zu.');
          return false;
        }
        const token = turnstileToken();
        if (!token) {
          showStepError('Bitte löse das CAPTCHA.');
          return false;
        }
        formData.captchaToken = token;
        return true;
      }
      default: return true;
    }
  }

  function collectStep(step) {
    switch (step) {
      case 2:
        formData.levelKey = document.querySelector('input[name="levelKey"]:checked')?.value ?? '';
        break;
      case 3:
        formData.firstName = v('pm-firstName');
        formData.lastName  = v('pm-lastName');
        formData.email     = v('pm-email');
        break;
      case 4:
        formData.addressLine = v('pm-addressLine');
        formData.postalCode  = v('pm-postalCode');
        formData.city        = v('pm-city');
        break;
      case 5:
        formData.showNameOnFloor = document.getElementById('pm-showName')?.checked ?? false;
        formData.displayName     = v('pm-displayName');
        formData.consent         = document.getElementById('pm-consent')?.checked ?? false;
        break;
    }
  }

  function updateFieldPreview() {
    const el = document.getElementById('pm-step1-field');
    if (!el) return;
    const label = vipLabel(formData.fieldNumber);
    el.textContent = label
      ? `Feld Nr. ${formData.fieldNumber} (${label})`
      : `Feld Nr. ${formData.fieldNumber}`;
  }

  function buildSummary() {
    const levels = { Bronze: 'Hallenbodenbesitzer – CHF 50.–/Jahr', Silber: 'Chnebler – CHF 100.–/Jahr', Gold: 'Cüpli-Chnebler – CHF 200.–/Jahr' };
    const label = vipLabel(formData.fieldNumber);
    set('pm-sum-field',   label ? `Feld Nr. ${formData.fieldNumber} (${label})` : `Feld Nr. ${formData.fieldNumber}`);
    set('pm-sum-level',   levels[formData.levelKey] ?? formData.levelKey);
    set('pm-sum-name',    `${formData.firstName} ${formData.lastName}`);
    set('pm-sum-email',   formData.email);
    set('pm-sum-address', `${formData.addressLine}, ${formData.postalCode} ${formData.city}`);
    set('pm-sum-display', formData.showNameOnFloor
      ? `Ja – «${formData.displayName || formData.firstName + ' ' + formData.lastName}»`
      : 'Nein');
  }

  // ── Submit ───────────────────────────────────────────────────────────────────
  async function submitForm() {
    if (!submitBtn) return;
    submitBtn.disabled = true;
    submitBtn.textContent = 'Wird gesendet…';
    clearStepError();

    const payload = {
      fieldNumber:     formData.fieldNumber,
      firstName:       formData.firstName,
      lastName:        formData.lastName,
      addressLine:     formData.addressLine,
      postalCode:      formData.postalCode,
      city:            formData.city,
      email:           formData.email,
      levelKey:        formData.levelKey,
      showNameOnFloor: formData.showNameOnFloor,
      displayName:     formData.displayName || null,
      consent:         formData.consent,
      captchaToken:    formData.captchaToken
    };

    try {
      const res = await fetch('/api/passivmitglieder/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (res.ok) {
        closeWizard();
        if (successMsg) {
          successMsg.classList.remove('pm-message--hidden');
          successMsg.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        // Mark field as occupied in the grid
        const cell = document.querySelector(`[data-field="${formData.fieldNumber}"]`);
        if (cell) {
          cell.setAttribute('class', 'pm-cell pm-cell--occupied');
          cell.setAttribute('tabindex', '-1');
          cell.removeEventListener('click', () => {});
        }
        updateCounter(occupiedFields.size + 1, TOTAL);
      } else {
        const body = await res.json().catch(() => ({}));
        let msg = 'Es ist ein Fehler aufgetreten. Bitte versuche es erneut.';
        if (body.error === 'field_taken') msg = 'Dieses Feld ist leider bereits belegt. Bitte wähle ein anderes Feld.';
        else if (body.error === 'captcha_failed') msg = 'CAPTCHA-Verifizierung fehlgeschlagen. Bitte versuche es erneut.';
        showStepError(msg);
        submitBtn.disabled = false;
        submitBtn.textContent = 'Jetzt anmelden';
      }
    } catch (_) {
      showStepError('Netzwerkfehler. Bitte prüfe deine Internetverbindung und versuche es erneut.');
      submitBtn.disabled = false;
      submitBtn.textContent = 'Jetzt anmelden';
    }
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────
  function v(id) { return (document.getElementById(id)?.value ?? '').trim(); }
  function set(id, text) { const el = document.getElementById(id); if (el) el.textContent = text; }

  function showStepError(msg) {
    const el = document.getElementById('pm-step-error');
    if (el) { el.textContent = msg; el.classList.remove('pm-message--hidden'); }
  }

  function clearStepError() {
    const el = document.getElementById('pm-step-error');
    if (el) { el.textContent = ''; el.classList.add('pm-message--hidden'); }
  }

  function turnstileToken() {
    // Cloudflare Turnstile stores its token in a hidden input named cf-turnstile-response
    return document.querySelector('[name="cf-turnstile-response"]')?.value ?? '';
  }

  // Show/hide display name field based on showName checkbox
  document.addEventListener('change', function (e) {
    if (e.target && e.target.id === 'pm-showName') {
      const wrap = document.getElementById('pm-displayName-wrap');
      if (wrap) wrap.classList.toggle('pm-hidden', !e.target.checked);
    }
  });
})();
