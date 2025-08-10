# **Keyboard + Mouse Odometer for Windows**

## **Project Overview**

The **Keyboard + Mouse Odometer** is a **C# Windows desktop application** that tracks:

* **Number of keypresses** (per key and total)
* **Mouse travel distance** (in pixels → meters)
* **Click counts (left/right/middle)**
* **Scroll activity (optional)**

The application:

* Logs data **per day** in a **SQLite3 database**
* **Summarizes daily, weekly, and monthly stats**
* Runs **in the system tray**
* Provides a **compact toolbar** that:

  * Displays **last key pressed**
  * Displays **daily keypress count and mouse distance**
  * **Dockable** below the Windows main taskbar
* Includes **automated tests** and **CI/CD build automation**

---

## **Requirements**

### **Functional Requirements**

1. **Key & Mouse Tracking**

   * Track every key press (with timestamps)
   * Track mouse movement distance (calculate cumulative pixel distance moved)
   * Track mouse clicks (left, right, middle)
   * Optional: Track scroll wheel activity

2. **Data Storage**

   * Store daily stats in a **SQLite3 database**
   * Aggregate per day (YYYY-MM-DD)
   * Summaries include:

     * `total_keys`
     * `total_mouse_distance`
     * `total_clicks`
   * Raw event logs optional for deeper analytics

3. **UI Requirements**

   * **System tray icon**

     * Right-click → open menu with:

       * "Open Dashboard"
       * "Pause Tracking"
       * "Exit"
   * **Main Dashboard Window**

     * Daily / weekly / monthly summary charts
     * Table of recent activity
     * Reset statistics option
   * **Dockable Toolbar**

     * Displays:

       * Last key pressed
       * Daily key count
       * Daily mouse distance (km/m)
     * Dockable just under **Windows Taskbar** (optional like “auto-hide” bars)

4. **Reports & Summaries**

   * Generate daily summaries automatically at midnight
   * Weekly and monthly views generated from daily aggregates

5. **Settings**

   * Toggle which stats to track
   * Start with Windows (optional)
   * Configure database retention (e.g., keep last 90 days)

---

### **Non-Functional Requirements**

* **OS:** Windows 10/11 (x64)
* **Language:** C# (.NET 8 / WPF or WinForms)
* **Database:** SQLite3
* **Performance:**

  * Low CPU (<2%) and RAM usage (<50 MB idle)
* **Security:**

  * No cloud upload
  * Local database encrypted (optional)
* **Usability:**

  * Always accessible from **System Tray**
  * Toolbar resizable and dockable

---

## **Architecture**

### **Core Components**

1. **Input Monitoring Service**

   * Global keyboard and mouse hooks
   * Computes mouse travel distance
   * Sends events to **Data Logger**

2. **Data Logger**

   * Aggregates per-day counters in memory
   * Writes events to SQLite database periodically

3. **UI Layer**

   * WPF-based interface for:

     * Dashboard (charts, tables)
     * Toolbar (dockable)
     * Tray context menu

4. **Database Layer**

   * SQLite3 with `System.Data.SQLite` or `Microsoft.Data.Sqlite`
   * Simple schema for aggregates and optionally raw logs

---

## **Database Schema**

```sql
-- Daily aggregates
CREATE TABLE daily_stats (
    date TEXT PRIMARY KEY,        -- YYYY-MM-DD
    key_count INTEGER DEFAULT 0,
    mouse_distance REAL DEFAULT 0, -- in meters
    left_clicks INTEGER DEFAULT 0,
    right_clicks INTEGER DEFAULT 0,
    middle_clicks INTEGER DEFAULT 0
);

-- Optional raw logs for debugging/analytics
CREATE TABLE key_mouse_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    event_type TEXT, -- key_down, mouse_move, mouse_click
    key TEXT,
    mouse_dx REAL,
    mouse_dy REAL
);
```

---

## **UI Mockup**

**System Tray Menu:**

```
[Icon] Keyboard+Mouse Odometer
-------------------------------
Open Dashboard
Pause Tracking
Exit
```

**Dockable Toolbar (example):**

```
[Last Key: W] | Keys Today: 1,254 | Mouse: 1.32 km
```

**Dashboard Window:**

* Top panel: Today’s stats summary
* Line chart: Keys per hour
* Line chart: Mouse distance per hour
* Table: Last 50 key events

---

## **Automation and CI/CD**

* **Build System:** GitHub Actions / Azure DevOps
* **Build Artifacts:**

  * Portable `.zip` and Installer `.msi`
* **Tests:**

  * **Unit Tests**: Validate DB writes, key counting logic
  * **Integration Tests**: Simulate key/mouse hooks
  * **UI Smoke Tests**: Launch app, verify tray & toolbar appear
* **Automation:**

  * Trigger build on `main` branch push
  * Run tests, then produce signed binaries

---

## **Testing Strategy**

1. **Unit Tests**

   * SQLite operations
   * Key counting logic
   * Mouse distance calculation

2. **Integration Tests**

   * Hook listener simulation
   * Database aggregation correctness

3. **UI Tests**

   * Verify tray icon is clickable
   * Verify toolbar updates on keypress

4. **Performance Tests**

   * Long run (24h) CPU/memory usage profiling

---

## **Future Enhancements**

* Cloud sync / web dashboard
* Gamification (badges for daily typing goals)
* Export reports to CSV / JSON
* Multi-monitor distance calculation

---

This spec gives a clear roadmap for **development, UI, database design, and automation**.

---

If you want, I can **also generate a project skeleton** with **C# + SQLite3 + WPF** ready to build, including **unit test scaffolding and GitHub Actions CI**.

Do you want me to create that next?
