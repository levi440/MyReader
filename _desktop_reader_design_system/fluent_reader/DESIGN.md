---
name: Fluent Reader
colors:
  surface: '#f9f9f9'
  surface-dim: '#dadada'
  surface-bright: '#f9f9f9'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f3f3f3'
  surface-container: '#eeeeee'
  surface-container-high: '#e8e8e8'
  surface-container-highest: '#e2e2e2'
  on-surface: '#1a1c1c'
  on-surface-variant: '#404752'
  inverse-surface: '#2f3131'
  inverse-on-surface: '#f1f1f1'
  outline: '#717783'
  outline-variant: '#c0c7d4'
  surface-tint: '#0060ab'
  primary: '#005faa'
  on-primary: '#ffffff'
  primary-container: '#0078d4'
  on-primary-container: '#ffffff'
  inverse-primary: '#a3c9ff'
  secondary: '#0061a6'
  on-secondary: '#ffffff'
  secondary-container: '#6db2ff'
  on-secondary-container: '#004376'
  tertiary: '#974700'
  on-tertiary: '#ffffff'
  tertiary-container: '#bc5b00'
  on-tertiary-container: '#ffffff'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#d3e3ff'
  primary-fixed-dim: '#a3c9ff'
  on-primary-fixed: '#001c39'
  on-primary-fixed-variant: '#004883'
  secondary-fixed: '#d2e4ff'
  secondary-fixed-dim: '#a0c9ff'
  on-secondary-fixed: '#001c37'
  on-secondary-fixed-variant: '#00497f'
  tertiary-fixed: '#ffdbc8'
  tertiary-fixed-dim: '#ffb689'
  on-tertiary-fixed: '#311300'
  on-tertiary-fixed-variant: '#743500'
  background: '#f9f9f9'
  on-background: '#1a1c1c'
  surface-variant: '#e2e2e2'
  mica-light: '#F3F3F3CC'
  mica-dark: '#202020CC'
  book-teal: '#008272'
  reading-paper: '#FBF1D3'
  reading-ink: '#1A1A1A'
  status-success: '#107C10'
  status-error: '#C42B1C'
typography:
  display:
    fontFamily: Inter
    fontSize: 40px
    fontWeight: '600'
    lineHeight: 52px
    letterSpacing: -0.02em
  title-lg:
    fontFamily: Inter
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
  title-md:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  subtitle:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '500'
    lineHeight: 24px
  body-reading:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 30px
  body-ui:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  caption:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-bold:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.05em
  code:
    fontFamily: jetbrainsMono
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 20px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 4px
  gap-xs: 4px
  gap-sm: 8px
  gap-md: 16px
  gap-lg: 24px
  margin-page: 32px
  nav-rail-width: 48px
  nav-pane-expanded: 240px
---

## Brand & Style

The design system is a faithful extension of the **Windows 11 Fluent Design** language, optimized for high-performance reading and information management. It targets a professional audience that values productivity, technical depth, and system-native aesthetics.

The visual style is **Corporate / Modern**, characterized by:
- **Layered Depth:** Utilizing the Mica material to create a sense of hierarchy by letting the desktop wallpaper shine through the application shell.
- **Precision & Efficiency:** A focus on information density and utility, ensuring that the interface feels like a high-end tool rather than a consumer toy.
- **Calm Focus:** A neutral, distraction-free environment that prioritizes content legibility over decorative elements.
- **Native Integration:** Every component, from corner radius to motion, is calibrated to feel like a first-party Windows utility.

## Colors

The palette is anchored by the **System Accent (Windows Blue)**, ensuring a seamless fit with the OS environment. For a more bookish, literary feel, a **Book-inspired Teal** is provided as a secondary brand alternative.

### Color Strategy
- **Application Shell:** Uses the Mica material (semi-transparent, textured background) to differentiate the navigation and title bar from the content area.
- **Surface Tiers:** 
    - **Layer 0 (Background):** Mica surface.
    - **Layer 1 (Cards/Containers):** Solid white (Light) or soft gray (Dark) with a 1px stroke.
- **Reading Themes:** Beyond the standard UI modes, a specialized "Paper" theme (Sepia) is included for long-form reading to reduce eye strain.
- **States:** Hover and active states follow standard WinUI 3 alpha-over-color logic, where buttons slightly darken or lighten upon interaction.

## Typography

This design system uses **Inter** (or Segoe UI Variable where native APIs permit) for its exceptional legibility and modern geometric construction. 

### Typographic Hierarchy
- **Functional UI:** Small, clean, and efficient. Labels and interface text use `body-ui` (14px) for high information density.
- **The Reading Experience:** `body-reading` (18px) uses an increased line height (1.6) and comfortable font sizing to facilitate long-form immersion without fatigue.
- **Technical Content:** `code` is reserved for RegEx editing, XPath rules, and JSON book source configurations, ensuring monospaced characters are distinct.
- **Case Usage:** Labels for status or categories use `label-bold` with slight tracking for professional clarity.

## Layout & Spacing

The system follows a **Fixed Grid** philosophy for the navigation shell combined with a **Fluid Content** area.

### Layout Model
- **App Shell:** A vertical `NavigationView` on the left. It defaults to a **Compact Rail** (48px) to maximize horizontal space, expanding to a full pane (240px) on hover or via a hamburger menu.
- **Content Area:** A standard 12-column grid with a 16px gutter.
- **Reading View:** Centered single-column layout for EPUB/TXT with a maximum readable width of 800px.
- **Breakpoints:**
    - **Compact (<640px):** Bottom navigation replaces the side rail; margins reduce to 16px.
    - **Medium (640px - 1007px):** Side rail enabled; library cards display in 3-4 columns.
    - **Large (>1008px):** Side rail may be expanded by default; library cards display in 6+ columns.

## Elevation & Depth

Hierarchy is established through **Tonal Layers** and **Mica Effects**, moving away from heavy drop shadows in favor of structural clarity.

- **Background:** The root window uses the Mica material, providing a dynamic, blurred texture that links the app to the user's desktop.
- **Surfaces:** Secondary containers (like library items or settings panels) use "Resting Elevation"—a subtle 1px border (`#00000010`) and a very soft, diffused shadow (4px blur, 2% opacity) to lift them off the Mica background.
- **Overlays:** Dialogs and context menus use a higher elevation tier with a more pronounced shadow (16px blur, 8% opacity) and a solid background to ensure legibility over complex content.
- **Reading Layer:** When in "Reader Mode," the UI chrome fades out, and the document occupies the primary Z-axis, occasionally using a "Reading Backdrop" (`#525659`) for high-contrast PDF viewing.

## Shapes

The design system adopts the **Rounded (0.5rem / 8px)** corner language native to Windows 11.

- **Primary Elements:** Buttons, text fields, and book cards use a standard 8px radius.
- **Large Containers:** Flyouts and dialogs use `rounded-xl` (24px) for a softer, more modern modal feel.
- **Contextual Shapes:** Small badges and status indicators (e.g., source health tags) use a pill-shape for distinct visual categorization.
- **Book Covers:** While covers retain their natural aspect ratio, they are wrapped in a container with a 4px inner radius to soften their appearance within the library grid.

## Components

### Buttons
- **Primary:** Solid Accent color with white text. Subtle gradient on hover.
- **Secondary:** Transparent background with a 1px border.
- **Icon Buttons:** No border, circular hover background (Ghost style).

### Navigation Rail/View
- Uses `NavigationViewItem` with Segoe Fluent Icons.
- Active state is indicated by a vertical "pill" (accent color) on the left edge of the menu item.

### Library Cards
- Aspect ratio of 3:4 for book covers.
- Includes a title (semibold), author (caption), and a "Source" tag.
- On hover, the card subtly scales (1.02x) and the border color shifts to the System Accent.

### Reading View (EPUB/TXT)
- **Top Bar:** Auto-hiding header with title, progress percentage, and "Exit" button.
- **Bottom Bar:** Page slider and chapter navigation.
- **Settings Overlay:** Floating palette for font size, line height, and theme switching (Light, Dark, Sepia).

### Audio Player Bar
- A unified, docked bar at the bottom of the content frame.
- Features: Album/Book art (small square), Play/Pause (large center icon), Skip 15s buttons, and a progress slider that spans the full width of the bar.

### Input Fields
- Standard WinUI style: Bottom-only active border (2px) in accent color when focused.
- Rounded 8px corners with a subtle light-gray fill in the resting state.