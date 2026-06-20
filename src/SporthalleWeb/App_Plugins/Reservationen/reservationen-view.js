import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';

class ReservationenView extends LitElement {
  static styles = css`
    :host {
      display: block;
      width: 100%;
      height: 100%;
    }
    iframe {
      width: 100%;
      height: calc(100vh - 60px);
      border: none;
      display: block;
    }
  `;

  render() {
    return html`<iframe src="/reservierung/backoffice-admin"></iframe>`;
  }
}

customElements.define('reservationen-view', ReservationenView);
