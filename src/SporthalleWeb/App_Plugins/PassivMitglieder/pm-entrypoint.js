class PmAdminElement extends HTMLElement {
  connectedCallback() {
    this.style.cssText = 'display:block;width:100%;height:100%;';
    const iframe = document.createElement('iframe');
    iframe.src = '/admin/passivmitglieder';
    iframe.style.cssText = 'width:100%;height:100%;border:none;display:block;';
    this.appendChild(iframe);
  }
}

customElements.define('pm-admin', PmAdminElement);
