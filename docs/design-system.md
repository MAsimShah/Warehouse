# Design System: Minimalism

Status: ACTIVE. Every screen in the app follows this style. The code for it lives in `frontend/src/index.css` (the tokens and component classes). Read this before building or changing any UI.

## 1. Principles
1. **Less is more.** Show only what the user needs for the task. Remove anything that is only decoration.
2. **Content first.** Text and data are the design. No gradients, no shadows, no illustrations, no coloured icon tiles.
3. **One accent.** The app is black and white with greys. Colour is used only for meaning (an error is red, a success is green).
4. **Space, not boxes.** Use whitespace to group things. Use a thin line only when space is not enough.
5. **Consistent.** Same spacing, same radius, same sizes everywhere. Do not invent new values; use the tokens.
6. **Works everywhere.** Mobile first, works on a phone and a desktop. Light and dark mode both supported.
7. **Accessible.** Visible focus, good contrast, labels on every field, keyboard works for everything.

## 2. Colour tokens
Always use the CSS variable, never a raw colour.

| Token | Light | Dark | Use |
|---|---|---|---|
| `--bg` | `#ffffff` | `#0b0b0c` | Page background |
| `--fg` | `#111111` | `#f5f5f5` | Main text, primary button |
| `--fg-contrast` | `#ffffff` | `#0b0b0c` | Text on the primary button |
| `--muted` | `#6b6b6b` | `#9a9a9a` | Secondary text, hints |
| `--line` | `#e5e5e5` | `#262626` | Borders and dividers |
| `--line-strong` | `#c9c9c9` | `#3d3d3d` | Input border, hover border |
| `--hover` | `#f6f6f6` | `#161616` | Hover background |
| `--danger` | `#c42b1c` | `#ff8a7a` | Errors only |
| `--success` | `#1a7f37` | `#5fd17f` | Success only (when needed) |
| `--focus` | `#111111` | `#f5f5f5` | Focus ring colour |

Rules:
- No other colours. If a new meaning needs a colour (for example warning), add a token here first.
- Status in tables or lists is shown with text (and a small dot if needed), not big coloured badges.

## 3. Typography
- Font: system font stack (`system-ui, 'Segoe UI', Roboto, sans-serif`). No web fonts.
- Base size 16px, line height 1.5.
- Sizes: `--text-sm` 0.875rem (hints, errors, table meta), `--text-base` 1rem, `--text-lg` 1.25rem (section title), `--text-xl` 1.75rem (page title).
- Weights: 400 for text, 500 for labels and buttons, 600 only for page titles. No bold paragraphs.
- Page title is sentence case ("Sign in", "Dashboard"). Not all caps.
- Language: English with Australian spelling. Dates `dd/MM/yyyy`.

## 4. Spacing, radius, lines
- Spacing scale (use only these): 4, 8, 12, 16, 24, 32, 48, 64 px (`--space-1` ... `--space-8`).
- Radius: `--radius` 6px for inputs, buttons and panels. No fully round buttons.
- Borders: 1px, `--line`. No shadows anywhere (no `box-shadow` except the focus ring).
- Max content width: forms 360px, pages 1080px.

## 5. Components

We do not use a UI component library (PrimeReact 11 needs a paid license). Components are plain HTML elements (`button`, `input`, `label`) styled with our classes. Only the free `primeicons` icon font is used. Do not add a paid or licensed UI library without asking.

### Button (`<button>`)
- **Primary** `wh-btn`: solid `--fg` background, `--fg-contrast` text. One primary button per screen area.
- **Secondary** `wh-btn secondary`: transparent, 1px `--line-strong` border.
- **Text** `wh-btn text`: no border, used for low priority actions (Cancel).
- Height 40px, radius 6px, font weight 500. Disabled = 50% opacity.
- While working, show a small spinner and change the label ("Signing in...").
- Icon only buttons need an `aria-label`.

### Input (`<input>`)
- Class `wh-input`: 1px `--line-strong` border, radius 6px, height 40px, no background tint.
- Label sits above the field, always visible (no placeholder-only labels). Label is 14px, weight 500.
- Error: red text under the field (`field-error`) and `aria-invalid`. The border turns `--danger`.
- Help text is `--muted`, 14px, under the field.

### Checkbox (`<input type="checkbox">`)
- Class `checkbox-row` on the `<label>`: box on the left, 14px text on the right, 8px gap. The box uses `accent-color: var(--fg)` (black/white, no colour).
- The label text is clickable. Default is unchecked.

### Message (`<div role="alert">`)
- Class `form-message`: 1px border, no fill, `--danger` text for errors. Short, plain sentence. Never blame the user.
- Place it at the top of the form, `role="alert"`.

### Layout classes
- `page-center`: centres one column on the screen (login, empty states).
- `auth-form`: the 360px form column.
- `app-bar`: top bar with the app name on the left and the user menu on the right. 56px high, bottom 1px line.
- `user-menu`: round 40px avatar button (`pi pi-user`) in the app bar. Click opens a dropdown (`menu`) below it, aligned right, with the user name, **Profile** and **Sign out**. No email or Sign out button is shown in the bar itself. The dropdown has a 1px border and no shadow. It closes on outside click, Escape or Tab, supports Arrow keys, Home and End, and the trigger has `aria-haspopup="menu"` and `aria-expanded`. Items that are not built yet are shown disabled with a "Soon" tag. Component: `frontend/src/components/UserMenu.tsx`.
- `page`: page content, max 1080px, 32px padding.
- `panel`: a section with a 1px line border and 24px padding. Prefer plain sections with a divider over many panels.

### Icons
- Only `primeicons` (`pi pi-...`), 16px, same colour as the text. Use an icon only when it helps (show/hide password, sign out). No icons just for decoration.

## 6. Page patterns

### Auth pages (login)
- One centred column, 360px wide, no card, no shadow, no logo tile.
- Order: app name (`Warehouse`), page title ("Sign in"), short muted line, fields, one primary button.
- No links to pages the user cannot use. Signup is not linked.

### Dashboard / app pages
- App bar on top (app name left, user menu avatar right).
- Page title (`h1`) at the top of the content, then the content.
- Prefer lists, tables and plain numbers over charts and cards. Add a chart only if it answers a real question.
- Empty state: one sentence saying what will appear here and, if possible, the action to add the first item.

## 7. Motion
- Almost none. Only 150ms colour transitions on hover and focus. No page animations, no bouncing.
- Respect `prefers-reduced-motion`.

## 8. Accessibility checklist (for every screen)
- Every input has a visible `<label>` linked with `htmlFor`.
- Focus is always visible (2px ring). Never remove the outline without a replacement.
- Text contrast is at least 4.5:1 (the tokens above meet this).
- Errors are announced (`role="alert"`) and linked to the field with `aria-describedby`.
- Everything works with the keyboard only.
- Touch targets are at least 40px high on phones.

## 9. Do and do not
| Do | Do not |
|---|---|
| Use tokens (`var(--line)`) | Use raw hex colours |
| One primary button per area | Many coloured buttons |
| Thin 1px lines, whitespace | Shadows, gradients, glass effects |
| Sentence case, short text | Long help text, all caps, exclamation marks |
| Text status ("Delivered") | Rainbow badges |
| Plain lists and tables | A card for everything |

## 10. How to add a new screen
1. Pick the pattern from section 6.
2. Build it only from the classes and tokens above. If something is missing, add it to `frontend/src/index.css` and to this document in the same change.
3. Check light and dark mode and a phone width.
4. Go through the accessibility checklist.
