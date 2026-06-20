import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';

class PmAdminElement extends LitElement {
  static styles = css`
    :host {
      display: block;
      position: absolute;
      inset: 0;
    }
    iframe {
      width: 100%;
      height: 100%;
      border: none;
      display: block;
    }
  `;

  render() {
    return html`<iframe src="/passivmitglieder/admin"></iframe>`;
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
