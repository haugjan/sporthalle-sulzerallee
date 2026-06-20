class PmAdminElement extends HTMLElement {
  connectedCallback() {
    this.style.cssText = 'position:absolute;inset:0;overflow:hidden;';
    const iframe = document.createElement('iframe');
    iframe.src = '/passivmitglieder/admin';
    iframe.style.cssText = 'position:absolute;inset:0;border:none;';
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
      conditions: [
        {
          alias: 'Umb.Condition.SectionAlias',
          match: 'pm.Section'
        }
      ],
      meta: {
        label: 'Passivmitglieder',
        pathname: 'passivmitglieder'
      }
    }
  ]);
};
