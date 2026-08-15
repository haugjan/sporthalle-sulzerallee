class EmailAdminElement extends HTMLElement {
  connectedCallback() {
    this.style.cssText = 'display:block;width:100%;height:100%;';
    const iframe = document.createElement('iframe');
    iframe.src = '/admin/emails';
    iframe.style.cssText = 'width:100%;height:100%;border:none;display:block;';
    this.appendChild(iframe);
  }
}

customElements.define('email-admin', EmailAdminElement);
