# AdHoc Observer: The Complete User Guide

This guide provides a comprehensive overview of the AdHoc Observer's features, interactions, and workflows, based directly on its source code implementation.

## Table of Contents

1.  [Launching the Observer](#1-launching-the-observer)
2.  [The User Interface: A Three-View System](#2-the-user-interface-a-three-view-system)
	*   [The Main View: Host-Packs Diagram](#the-main-view-host-packs-diagram)
	*   [The Channel View: State Machine Diagram](#the-channel-view-state-machine-diagram)
	*   [The Pack-Fields View: Structure Diagram](#the-pack-fields-view-structure-diagram)
3.  [Core Interactions](#3-core-interactions)
	*   [Navigation: Pan, Zoom, and Focus](#navigation-pan-zoom-and-focus)
	*   [Getting Information: Tooltips](#getting-information-tooltips)
	*   [Drill-Down Exploration](#drill-down-exploration)
4.  [The Sidebar: Your Command Center](#4-the-sidebar-your-command-center)
	*   [Tree View Navigation](#tree-view-navigation)
	*   [Searching for Elements](#searching-for-elements)
	*   [Controls: Saving, Themes, and Colors](#controls-saving-themes-and-colors)
5.  [Annotation with Stickers: In-Depth](#5-annotation-with-stickers-in-depth)
	*   [Creating and Editing Stickers](#creating-and-editing-stickers)
	*   [Sticker Interactions](#sticker-interactions)
	*   [The "Peek" Feature](#the-peek-feature)
6.  [Keyboard Shortcuts](#6-keyboard-shortcuts)
7.  [Persistence: Saving Your Workspace](#7-persistence-saving-your-workspace)

## 1. Launching the Observer

To start the Observer, run the `AdHocAgent` utility from your command line, pointing to your protocol description file and appending a `?` to the filename.

```bash
# Example
AdHocAgent.exe path/to/MyProtocol.cs?
```

This starts a local web server, opens your default browser, and establishes a live WebSocket connection to the agent to load the protocol data.

## 2. The User Interface: A Three-View System

The Observer is composed of one main view and two specialized pop-up views, each providing a different perspective on your protocol.

### The Main View: Host-Packs Diagram

This is your primary workspace. It displays the high-level system architecture.

*   **Hosts:** Rendered as large parent nodes, containing all the packs they can interact with.
*   **Packs:** Nodes inside a host, representing data structures.
*   **Languages:** Small nodes within a host (e.g., `TS`, `CS`) that can be clicked to filter the visible packs.
*   **Channels:** Edges connecting hosts, representing communication pathways.

### The Channel View: State Machine Diagram

Opened by **right-clicking a channel edge** in the Main View, this pop-up provides a detailed visualization of a channel's data flow.

*   **Stages:** Nodes representing states in the communication lifecycle. The initial stage is highlighted.
*   **Branches:** Groups of packs that can be sent from a stage.
*   **Transitions:** Arrows showing how sending a pack from a branch leads to a new stage. Terminal states (which close the connection) are marked with a `⛔` symbol.

### The Pack-Fields View: Structure Diagram

Opened by **left-clicking a pack node**, this pop-up details the internal structure of all packs in your project.

*   **Packs:** Rendered as parent nodes, color-coded by their nesting level.
*   **Fields & Constants:** Nodes within a pack representing its data members.
*   **Type Links:** Edges connecting a field to the pack that defines its data type, revealing structural dependencies and self-references.

## 3. Core Interactions

### Navigation: Pan, Zoom, and Focus

*   **Pan:** Click and drag the diagram's background.
*   **Zoom:** Use the mouse wheel or trackpad.
*   **Focus on Element:** Click an item in the sidebar's tree view to automatically pan and zoom to it. The animation is "cinematic"—it zooms out to show context if the target is off-screen before zooming in.

### Getting Information: Tooltips

Hovering over any element provides instant, context-rich information.

*   **Mechanism:** Hover your mouse over an element for 500ms (or tap-and-hold on touch devices) to display a tooltip. The tooltip disappears when you move the mouse, pan, or zoom.
*   **Content:**
	*   **Hosts/Packs/Channels:** Shows the element's name and any C# XML documentation (`<summary>`). The name is a clickable link that can navigate you to the source code definition in your IDE.
	*   **Languages:** Explains the default code generation strategy (e.g., "generates with implementation," "without hash/equals methods").
	*   **Pack (in Host Context):** Details the pack's role (`Send`, `Receive`), implementation status (`Abstract`/`Implemented`), and notes any fields with special generation rules.

### Drill-Down Exploration

*   **View Channel Logic:** **Right-click** a channel edge.
*   **View Pack Structure:** **Left-click** a pack node.

## 4. The Sidebar: Your Command Center

Each view has its own sidebar, accessible via the `☰` button in the top-left corner.

### Tree View Navigation

The sidebar contains a hierarchical tree of every element in the current view.
*   **Expand/Collapse:** Click the `▶` icon next to any parent item.
*   **Go to Element:** Click any item's name to have the main view pan and zoom directly to it, highlighting the element. For stickers, it will center the view on the sticker and flash a highlight border.

### Searching for Elements

The search bar at the top of the sidebar instantly filters the tree view as you type.
*   It hides non-matching elements.
*   It automatically expands the parents of any matching element so you can see its location in the hierarchy.
*   Clearing the search bar restores the full tree.

### Controls: Saving, Themes, and Colors

*   **Save Layout:** Manually triggers a save of the current layout and all stickers.
*   **Theme Switcher:** Toggles the entire UI between a light and dark theme. Your preference is saved in `localStorage`.
*   **Hue Slider:** Dynamically changes the color palette of the diagram for better visibility or personalization.

## 5. Annotation with Stickers: In-Depth

Stickers are powerful, persistent, rich-text annotations you can place on any diagram.

### Creating and Editing Stickers

*   **Create:** **Double-click** on an empty area of the background. A new sticker appears, and the editor opens.
*   **Edit:** **Double-click** on an existing sticker. This triggers a smooth animation where the sticker expands and morphs into the editor UI.
*   **The Editor:** A full-featured CKEditor instance allows you to format text (bold, lists, colors), insert links, tables, and even upload images, which are embedded as Base64 data.

### Sticker Interactions

*   **Move:** Click and drag the sticker's header.
*   **Resize:** Drag the bottom-right corner of the sticker. The sticker remembers its size.
*   **Collapse/Expand:** Click the `▼` (Collapse) or `▲` (Expand) icon in the header. A collapsed sticker only shows its header.
*   **Change Color:** Click the palette icon (`🎨`) in the header to open a color picker for the header background. The text color automatically adjusts for contrast.
*   **Delete:** Click the `×` in the header. A confirmation prompt will appear.

### The "Peek" Feature

This is a subtle but useful interaction for quickly viewing a collapsed sticker's content without fully expanding it.
*   **How to Use:** **Click and hold** (for 1 second) on the header of a *collapsed* sticker without dragging.
*   **Result:** The sticker will animate to its fully expanded state, allowing you to read its content. When you release the mouse button, it will animate back to its collapsed state. If you start dragging during the hold, the peek is cancelled.

## 6. Keyboard Shortcuts

The Observer is fully keyboard-navigable.

| Key(s) | Context | Action |
| :--- | :--- | :--- |
| **Escape** | Sticker editor is open | Closes the editor. If there are unsaved changes, it shows a "Save/Discard" prompt. |
| **Arrow Keys** | An element is selected | Nudges the selected element by 1 pixel in the chosen direction. |
| **Any letter/number** | Diagram is focused | Automatically opens the sidebar (if closed), focuses the search bar, and types the key. |

## 7. Persistence: Saving Your Workspace

The Observer is designed to remember your work between sessions.

*   **What is Saved:**
	1.  **Diagram Layout:** The pan, zoom, and exact `x`/`y` coordinates of every node in every view.
	2.  **Stickers:** The position, size, name, content, color, and collapsed state of every sticker in every view.
	3.  **Theme Preference:** Your choice of light or dark mode.

*   **How it Saves:**
	*   **Automatic:** When you close the browser tab or navigate away (`beforeunload` event), the Observer sends all layout and sticker data to the `AdHocAgent` to be saved to a file (`..._DiagramData/layout`).
	*   **Manual:** Clicking the "Save Layout" button in the sidebar triggers the same save process.

When you next launch the Observer for the same protocol file, this data is reloaded, restoring your workspace exactly as you left it.