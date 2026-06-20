class PmAdminElement extends HTMLElement {
  connectedCallback() {
    this.style.cssText = 'display:block;width:100%;height:100%;overflow:hidden;';
    const iframe = document.createElement('iframe');
    iframe.src = '/passivmitglieder/admin';
    iframe.style.cssText = 'width:100%;height:100%;border:none;display:block;';
    iframe.setAttribute('frameborder', '0');
    iframe.setAttribute('allowfullscreen', '');
    this.appendChild(iframe);
  }
}

customElements.define('pm-admin', PmAdminElement);
