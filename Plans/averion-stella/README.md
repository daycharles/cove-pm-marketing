# Averion Stella website preview

Open `index.html` to review the website rebrand. This package is based on the existing standalone Stella website; it has not been deployed.

## Handoff for Michael

- `averion-stella.css`: standalone website stylesheet, including the original interface-example components and the Averion brand layer.
- `assets/averion-mark.png`: existing company mark, copied without alteration.
- `index.html`: reference markup for the header, product name, navigation, and feature examples.
- `evidence/`: desktop/mobile screenshots, layout checks, and a short website walkthrough.

Use **Averion Stella** for the product name and **Averion Software LLC** for legal company references. The palette is navy `#101820`, raised navy `#17232e`, silver `#aab1b8`, and gold `#c3a36a`. Primary text is `#e8edf2`. Gold is the brand/action accent; keep green `#29c46a` for positive trading states, red `#f0555a` for negative states, and amber `#e8b34f` for warnings.

The stylesheet retains the existing variables (`--surface-1`, `--surface-2`, `--text-primary`, `--series-1`, `--bull-color`, and others). Michael can map these tokens to the app's theme and reuse the relevant component rules. Website selectors such as `.hero`, `.feature`, and `.site-header` are website-specific and should be scoped when integrating into the app. Rename app labels separately; CSS cannot change accessible product names or document titles.

The page uses system fonts and the original illustrative interface screens. Existing product capability copy is carried over for this visual review; the app was not audited. Both demo links open an email draft with the new product name. The feature navigation and secondary hero link scroll to page sections.

The source repository has no sandbox pool. Work and capture are isolated in this preview directory. Verification starts a temporary local server and headless browser and closes both after recording. No app changes or live deployment are included.
