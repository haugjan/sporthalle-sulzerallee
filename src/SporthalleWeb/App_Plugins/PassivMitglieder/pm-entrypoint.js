class PmAdminElement extends HTMLElement {
  connectedCallback() {
    const style = document.createElement('style');
    style.textContent = `
      pm-admin {
        display: block;
        position: absolute;
        inset: 0;
      }
      pm-admin iframe {
        width: 100%;
        height: 100%;
        border: none;
        display: block;
      }
    `;
    document.head.appendChild(style);

    const iframe = document.createElement('iframe');
    iframe.src = '/passivmitglieder/admin';
    this.appendChild(iframe);
  }
}
customElements.define('pm-admin', PmAdminElement);

export const onInit = (_host, extensionRegistry) => {
  extensionRegistry.registerMany([
    {
      type: 'section',
      alias: 'pm.Section',
      name: 'Passivmitglieder',
      weight: 900,
      meta: {
        label: 'Passivmitglieder',
        pathname: 'passivmitglieder'
      }
    },
    {
      type: 'dashboard',
      alias: 'pm.Dashboard',
      name: 'Passivmitglieder',
      elementName: 'pm-admin',
      weight: 100,
      meta: {
        label: 'Passivmitglieder',
        pathname: 'passivmitglieder'
      }
    }
  ]);
};
