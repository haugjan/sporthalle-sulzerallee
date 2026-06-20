class ReservationenView extends HTMLElement {
  connectedCallback() {
    this.style.cssText = 'display:block;width:100%;height:100%;';

    const iframe = document.createElement('iframe');
    iframe.src = '/reservierung/backoffice-admin';
    iframe.style.cssText = 'width:100%;height:calc(100vh - 60px);border:none;display:block;';

    this.appendChild(iframe);
  }

  disconnectedCallback() {
    this.innerHTML = '';
  }
}

customElements.define('reservationen-view', ReservationenView);
