# CLAUDE.md — BiosWatcher (Multi-Vendor)

## Project Overview

BiosWatcher is a Windows desktop app that monitors a user-maintained list of motherboards
across the major vendors (MSI, ASUS, Gigabyte, ASRock, Biostar) and notifies the user when a new
BIOS version is released. Any BIOS whose release date is **within the last 14 days** is
flagged as *NEW* (the flag expires automatically 2 weeks after the release date — it is
derived from the date, not stored).

Target user: single user, local machine, no server component.

## Current Status (as of 2026-08-12)

**M1 through M8 are done.** v1 scope (M1–M6) shipped first; M7 (Biostar, a 5th vendor) and
M8 (fixing a real MSI 403 a user hit through the published exe — see MSI vendor section)
followed as post-v1 additions. Solution builds clean (`dotnet build BiosWatcher.slnx`),
83/83 tests pass (`dotnet test BiosWatcher.slnx`
— 65 from M1–M6 plus 18 new `BiostarParserTests`).

- Solution file is `BiosWatcher.slnx` (newer `dotnet new sln` default on this machine),
  not `.sln` — use that filename with `dotnet build`/`dotnet test`.
- `global.json` pins the SDK to `9.0.316`. The machine's default `dotnet` resolves to a
  `10.0.400-preview` SDK, which breaks `net9.0-windows` template generation and would
  silently retarget the project if unpinned. Keep `global.json` even after touching
  `.csproj` files.
- **M1 delivered:** all `Models/`, `ConfigRepository` (atomic write, `.bak` on corrupt
  load — matches spec), `IBiosSource`/`VendorRegistry`/`HttpClientProvider`,
  `MsiBiosSource`, 11 fixture-based `MsiParserTests`. Two files not in the original
  suggested structure, added because they turned out to be needed:
  `Services/DateOnlyJsonConverter.cs` (forces ISO `yyyy-MM-dd` + InvariantCulture in
  config.json regardless of .NET's native DateOnly JSON support) and
  `Services/Sources/MsiApiModels.cs` (internal DTOs for the MSI JSON shape, kept
  separate from `MsiBiosSource.cs`).
- **M2 delivered:** `UpdateChecker` (sequential, 500 ms delay, beta-aware latest-release
  selection, keeps last-known data + sets `LastCheckFailed` on any error without
  blocking other boards), `MainForm` (toolbar, DataGridView with the spec'd 7 columns,
  NEW-row highlighting via color+text, right-click context menu, double-click →
  detail), `AddEditBoardForm` (Add: URL textbox → auto-detect → duplicate check →
  fetch baseline; Edit: rename + fix URL only, no re-fetch), `BoardDetailForm`
  (changelog/size/sha256 are **not** persisted in config.json, so this form always
  re-fetches live from the vendor on open). 9 more tests in `UpdateCheckerTests.cs`
  using a fake `IBiosSource` (no network).
- **Known environment caveat, later resolved (2026-08-12) — kept for history:** the
  sandbox this was built in got a `403` from an Akamai WAF on `msi.com` for every request
  (even the plain homepage), while other sites resolved fine — this blocked live-testing
  `MsiBiosSource` against the real API from that environment at the time. It turned out to
  *not* be sandbox-specific: a real user hit the identical 403 from their own home machine
  running the published exe. Root cause and fix (Edge WebView2 replacing `HttpClient` for
  MSI specifically) are in the MSI vendor section below — do not reintroduce a plain
  `HttpClient`/`HttpClientProvider` call for msi.com, it will get blocked again.
- **M3 delivered:** `MainForm` now owns a `NotifyIcon` (generic `SystemIcons.Application`
  icon — no custom `.ico` asset yet) with a context menu (Open / Check all now / Exit),
  a `System.Windows.Forms.Timer` (`periodicCheckTimer`) driving `RunFullCheckAsync()` on
  `settings.CheckIntervalHours`, and close-to-tray behavior: minimizing or clicking the
  title-bar X hides the form instead of exiting (`FormClosing` checks
  `CloseReason.UserClosing` and `Hide()`s instead of closing; only the tray "Exit" item
  sets `_isExiting` first). `UpdateChecker.CheckBoardAsync`/`CheckAllAsync` now return
  whether a release version was newly added to `Board.NotifiedVersions` (de-dup lives on
  the board itself, not in the checker), and `MainForm` shows a balloon tip listing the
  affected board(s) whenever a check (manual, "Check all", or timer-driven) surfaces one.
  3 more tests in `UpdateCheckerTests.cs` cover the new-version detection/de-dup logic.
  A `_isChecking` guard prevents the periodic timer from overlapping a manual check.
  **Verified live on-screen** (not just unit tests): launched the built exe and drove it
  with real simulated mouse input (not posted window messages — see note below) —
  minimizing hides the window and process stays alive; clicking the actual title-bar X
  produces `CloseReason.UserClosing` and also hides-not-exits; the generic tray icon
  appears in the notification area. Caveat: the Windows 11 tray-overflow flyout proved
  too flaky to drive reliably via synthetic clicks in this pass, so the tray context
  menu's "Open"/"Check all now"/"Exit" items were confirmed by code review + the
  Show()/Hide() mechanics they call (already proven live) rather than by clicking them
  on screen — worth a real click-through if picked up again.
  **Automation gotcha worth remembering:** a `WM_CLOSE` posted via `PostMessage` from an
  *external* process (e.g. a PowerShell test script) arrives in `FormClosing` as
  `CloseReason.TaskManagerClosing`, not `UserClosing` — WinForms deliberately
  distinguishes a real title-bar click from a foreign-process close request. Don't
  mistake that for a bug in the cancel-and-hide logic; test the real click (or
  `SendInput`-style input on the actual button) to get `UserClosing`.
- **M4 delivered:** `AsusBiosSource` and `GigabyteBiosSource`, both wired into
  `VendorRegistry` in `MainForm` and into the Add-dialog's unknown-vendor error message.
  Added the `HtmlAgilityPack` NuGet package (to both the main project and the test
  project, since `GigabyteParserTests` constructs `HtmlDocument` directly) and a small
  shared `DownloadSizeParser` (KB/MB/GB display-string → bytes) used by both new sources,
  since neither vendor gives an exact byte count the way MSI does. 20 more tests
  (`AsusParserTests.cs`, `GigabyteParserTests.cs`).
  **Both M-tasks this doc called out were actually done, not deferred:** live `curl`
  calls against the real ASUS API corrected the primary endpoint (see the ASUS vendor
  section above — `www.asus.com`'s documented endpoint turned out to be dead, not just
  slower to verify), and a Wayback Machine archive of a real Gigabyte support page
  (gigabyte.com itself still 403s from this sandbox, like msi.com) supplied genuine HTML
  to build `GigabyteBiosSource` and its fixture against, rather than guessing markup —
  see the Gigabyte vendor section for the verification caveat that comes with using an
  archived page instead of the live one.
  **Verified beyond fixtures:** a throwaway live test (written, run once, then deleted —
  not part of the permanent suite, consistent with MSI/ASUS not having live-network
  tests checked in) round-tripped `AsusBiosSource.GetBoardInfoAsync` against the real
  `rog.asus.com` API end-to-end (HTTP → JSON deserialize → group filter → date parse →
  sort) and printed a real parsed release.
  **UI click-through done on a later pass** (the machine's screen was locked the first
  time; unlocked on retry): Add board with a live ASUS PRIME B650-PLUS URL correctly
  fetched, correctly skipped a newer beta build (`IsRelease=0`) in favor of the latest
  non-beta per the default `ignoreBetaVersions: true` setting, and the detail view showed
  the full release history (betas correctly labeled), cleaned changelog, size, and
  SHA-256. Adding a Gigabyte URL correctly surfaced the sandbox's 403 as a caught, clearly
  worded error rather than crashing.
  **Bug found and fixed via that click-through, not by the fixture suite:**
  `AddEditBoardForm.SetBusy(false)` unconditionally reset `statusLabel.Text` to `""` in
  the `finally` block, silently erasing whatever `ShowError` had just written in the
  `catch` block above it — so a failed Add (any vendor, not just Gigabyte) went visually
  quiet with zero feedback. `SetBusy` now only touches the status label on the "entering
  busy" transition; whichever terminal state (`ShowError`, or the dialog just closing on
  success) owns the label from then on. This is exactly the kind of bug that's invisible
  to WinForms-free unit tests and only shows up by actually clicking through the UI —
  reinforces that Forms code needs the on-screen check, not just the parser fixtures.
- **M5 delivered — real implementation, not a stub.** First pass shipped
  `AsrockBiosSource` as "unsupported yet" (this sandbox's own `curl`/`HttpClient` calls to
  `www.asrock.com` 403 under Incapsula, and a Wayback Machine archive of a product page's
  JS confirmed the real mechanism — `$('#BIOS').load('BIOS.html')`, a jQuery AJAX GET to a
  sibling `BIOS.html` next to the product's `index.asp` — but not the fragment's actual
  markup, which nothing had ever crawled). The user then opened a real board's page in
  their own browser, pulled the live `BIOS.html` response via DevTools, and pasted it in —
  genuine current markup, not archived and not guessed — which is what `AsrockBiosSource`
  and its fixture are actually built against. **Real, verified findings from that markup**
  (all different from what a reasonable guess would have produced):
  - Plain `<table>`, no CSS classes on cells — columns are positional: Version, Date,
    Size, Update method (skip — Instant Flash/Flashback links, not release data),
    Description, Global download, China download (FTP — skip in favor of the HTTPS
    Global link).
  - Date format is `yyyy/M/d`, **not zero-padded** (`2022/11/1`, `2024/1/19`) — the
    opposite of Gigabyte, which *is* zero-padded. Don't assume one vendor's convention
    for another.
  - Size has **no space** before the unit (`19.13MB`) — `DownloadSizeParser`'s regex
    already tolerated this (`\s*`, zero-or-more) without needing a change.
  - Beta is signaled by a `<br><font>[Beta]</font>` appended to the version cell
    (`3.18.AS02<br><font>[Beta]</font>`) — extracted via a `[Beta]` substring check, not
    a separate column.
  - **SHA256 lives inside the Description cell** as its own trailing line
    (`<font style="color:gray;">SHA256: <hash></font>`), not a separate column — split
    out via regex, everything before it is the changelog.
  - Footnotes (`<div class="Remark">...</div>`) can appear inline within the description,
    same pattern as Gigabyte's out-of-`<ol>` footnotes — handled the same way, by
    stripping tags rather than trying to enumerate structural elements.
  Extracted the shared `<br/>`-to-newline + tag-stripping logic (`HtmlTextUtils.cs`) since
  ASUS needed the identical thing — same DRY threshold already applied to
  `DownloadSizeParser` in M4. `AsrockParserTests.cs` (7→15 tests) now parses the real
  fixture (`asrock_x670e_taichi_bios.html`, trimmed from the user-provided live response)
  instead of asserting a `NotSupportedException`. The Add-dialog's unknown-vendor message
  no longer carries the "not supported yet" caveat for ASRock.
  **Verified live on-screen, both outcomes:** Add board with a real ASRock X670E Taichi
  URL succeeded end-to-end from this sandbox — fetched, parsed, and added with real data
  (version 4.43, 30.06.2026) — meaning Incapsula's block on this sandbox is
  **intermittent, not a hard block** (the very same session's detail-view re-fetch on the
  same board immediately after got a 403). Don't treat one 403 from this environment as
  proof a vendor endpoint is unreachable; retry before concluding that.
  The detail-view failure case also confirmed graceful handling: clear error text, no
  crash, `Download BIOS` correctly disabled with nothing loaded.
  **Automation notes for next time:** `%` is `SendKeys`'s Alt-modifier escape character,
  so typing a URL containing `%20` (ASRock model names are space-separated) silently
  mangles it unless written as `{%}20`. Also, `SetForegroundWindow` calls from a
  background PowerShell process are frequently denied by Windows' focus-stealing
  prevention even when the target window exists and is visible; `AttachThreadInput`
  (attach to the current foreground window's thread, restore/`BringWindowToTop`/
  `SetForegroundWindow` the target, detach) reliably works around it, plain
  `SetForegroundWindow` alone often doesn't.
- **M6 delivered — v1 scope now complete.** `SettingsForm` (`Forms/SettingsForm.cs` +
  `.Designer.cs`) exposes `CheckIntervalHours` (1–168h), `IgnoreBetaVersions`, and
  `NewFlagDays` (1–90d) via NumericUpDowns + a checkbox, mutating the shared `AppSettings`
  instance in place on Save (mirrors `AddEditBoardForm`'s edit-in-place pattern) and
  exposing `IntervalChanged` so `MainForm` only restarts `periodicCheckTimer` when the
  interval actually changed. Wired to a new "Settings…" toolbar button (after a separator,
  following "Check all").
  **Error indicators:** the Status column's existing red "⚠ Failed" cell (already in place
  since M2) now also carries a real tooltip. `Board` gained a `[JsonIgnore] LastCheckError`
  string — deliberately *not* added to the persisted config schema (it's transient,
  re-populated on the next check, same reasoning as `IsNew` being derived rather than
  stored) — populated from the caught exception's `Message` in `UpdateChecker.CheckBoardAsync`
  (both the fetch/parse-failure path and the "no source for this vendorId" path, which
  previously failed silently with no detail at all).
  **Empty-state hint:** a centered `emptyStateLabel` sits over `boardsGrid` (`BringToFront()`
  in the constructor, since designer `Controls.Add` order alone wasn't going to be trusted
  to win the z-order), toggled by `_config.Boards.Count == 0` at the end of `RefreshGrid()`.
  **Verified live on-screen, not just by code review** — a UI-Automation-driven PowerShell
  script (`System.Windows.Automation`, since this project's own automation notes above
  already flagged `SendKeys`/`SetForegroundWindow` pitfalls with plain scripting) launched
  the real built exe against the real `%APPDATA%\BiosWatcher\config.json` (which happened
  to already have zero boards — no fixture needed) and captured real screenshots: the
  empty-state label rendered correctly centered over the grid, and the Settings dialog
  opened with the exact persisted values (24 / checked / 14) read via `ValuePattern`/
  `TogglePattern`. **Gotcha hit and worth remembering:** `InvokePattern.Invoke()` on a
  ToolStripButton whose click handler opens a *modal* dialog blocks the UI Automation COM
  call itself until the dialog closes (the client-side wait then times out first, even
  though the click landed and the dialog is genuinely open) — don't treat that timeout as
  "the click failed," check for the window before retrying. Closed the dialog via Cancel
  (`AppActivate` + `SendKeys("{ESC}")`, since `CancelButton` wiring makes Escape trigger it)
  rather than Save, and diffed `config.json` before/after to confirm the live-verification
  pass didn't mutate the user's real settings.

## Tech Stack

- **Language/Framework:** C# / WinForms (Visual Studio 2022)
- **HTTP:** `HttpClient` (single shared instance, browser-like `User-Agent` header set
  once — several vendor sites reject the default .NET UA) for every vendor **except MSI**,
  which needs a real browser engine — see below.
- **Browser engine (MSI only):** `Microsoft.Web.WebView2` (NuGet) — msi.com's Akamai WAF
  blocks plain `HttpClient`/`curl` outright (confirmed via TLS/client fingerprinting, not
  fixable with header spoofing; see the MSI vendor section). `MsiWebViewFetcher` hosts an
  off-screen WebView2 (Edge/Chromium) control and reads the raw response via
  `WebResourceResponseReceived`. Requires the WebView2 Runtime, effectively always present
  on Windows 10/11 via Edge.
- **JSON:** `System.Text.Json`
- **HTML parsing:** `HtmlAgilityPack` (NuGet) — needed for Gigabyte, ASRock, and Biostar
- **Notifications:** `NotifyIcon` balloon tip (tray notification), no extra packages
- **Persistence:** single JSON config file. No database — a handful of records only.
- **Architecture:** MVC-ish separation like the DragRace project:
  - `Models/` — data classes
  - `Services/Sources/` — one `IBiosSource` implementation per vendor
  - `Services/` — `ConfigRepository`, `UpdateChecker`, `VendorRegistry`
  - `Forms/` — UI

## Core Abstraction

```csharp
public interface IBiosSource
{
    string VendorId { get; }                       // "msi", "asus", "gigabyte", "asrock"
    bool CanHandle(Uri supportUrl);                 // detect vendor from pasted URL
    BoardRef ParseBoardRef(Uri supportUrl);         // extract model identifier
    Task<BoardInfo> GetBoardInfoAsync(BoardRef r);  // official title + releases
}

public record BoardRef(string VendorId, string ModelId, string SupportUrl);
public record BiosRelease(string Version, DateOnly ReleaseDate, string Changelog,
                          string? DownloadUrl, long? SizeBytes, string? Sha256, bool IsBeta);
public record BoardInfo(string Title, IReadOnlyList<BiosRelease> Releases);
```

`VendorRegistry` holds all sources; "Add board" loops `CanHandle` over the pasted URL.

---

## Vendor Data Sources (verified 2026-08-11 unless noted)

**General rule for all vendors: never scrape when an API exists; never guess an
endpoint — verify it in the browser DevTools network tab first.**

### 1. MSI — JSON API ✅ schema verified; fetch mechanism corrected 2026-08-12 (see below)

Support URL pattern: `https://www.msi.com/Motherboard/<SLUG>/support#bios`
Slug regex: `Motherboard/([^/]+)/support` (matches on host `*.msi.com`, so a pasted
locale subdomain like `de.msi.com` is accepted — `GetBoardInfoAsync` always calls the
`www.msi.com` API regardless, same "always use the canonical host" reasoning as
Gigabyte/Biostar).

```
GET https://www.msi.com/api/v1/product/support/panel?product=<SLUG>&type=bios
```

**⚠️ msi.com sits behind an Akamai WAF that flatly blocks plain HTTP clients — confirmed
real, not a sandbox artifact.** The original "M1 delivered" note below (kept for history)
assumed this was a dev-sandbox-only 403; a real user hit the *identical* 403 on their own
home machine running the published exe, which ruled that out. Re-verified 2026-08-12: both
the API above and the plain static product page return `403` from `errors.edgesuite.net`
via `HttpClient`/`curl`, **even with a full set of spoofed real-Chrome headers**
(`User-Agent`, `Referer`, `Accept`, `sec-ch-ua`, `Sec-Fetch-*`). Since the exact same block
reproduces on two unrelated networks and survives header spoofing, it's almost certainly
TLS/HTTP-client fingerprinting (Akamai Bot Manager), not IP reputation or a missing header
— no realistic amount of header tuning fixes that from a non-browser HTTP stack.

**Fix: `MsiBiosSource` no longer uses `HttpClientProvider` at all.** It routes through
`MsiWebViewFetcher`, which hosts a real Microsoft Edge WebView2 (Chromium) control
off-screen, navigates it directly to the target URL, and captures the exact response body
via `CoreWebView2.WebResourceResponseReceived` (bypassing Edge's built-in JSON-viewer DOM
entirely — this reads the raw bytes the server sent, not rendered HTML). A real Chromium
engine passes Akamai's check the same way the user's own browser does. **Verified live**
against the exact URL and board the user reported failing
(`https://de.msi.com/Motherboard/MAG-X670E-TOMAHAWK-WIFI/support#bios`): succeeded twice in
a row from the same sandbox that has 403'd every single `curl`/`HttpClient` attempt against
msi.com throughout this project — once via the Add-board dialog's initial fetch, once again
via the detail view's independent re-fetch (proving the fetcher is safely reusable across
calls, not a one-shot fluke). Real data returned: version `7E12v1L2`, release `2026-07-06`,
17.6 MB, full changelog, clean SHA-256, correctly labeled Beta history.

Trade-off worth knowing: this requires the Microsoft Edge WebView2 Runtime (pre-installed
with Edge on effectively all Windows 10/11 machines) and is slower to first-fetch than a
plain HTTP call, since it spins up a real (if invisible) browser process. `MsiBiosSource`
keeps one `MsiWebViewFetcher` instance alive for the app's lifetime rather than
re-launching Edge per request.

Relevant response fields:

```jsonc
{
  "status": { "code": 200 },
  "result": {
    "title": "MAG B650 TOMAHAWK WIFI",
    "downloads": {
      "AMI BIOS": [ {                              // newest first, sort by date anyway
        "download_version": "7D75v1R2",            // may contain "(Beta version)"
        "download_release": "2026-07-06",          // yyyy-MM-dd
        "download_description": "- AGESA ...",
        "download_url": "https://download.msi.com/bos_exe/mb/7D75v1R2.zip",
        "download_size": 18499572,                 // bytes
        "download_sha256": "SHA-256:c902...<br>"   // strip prefix + trailing <br>
      } ]
    }
  }
}
```

Gotchas: beta suffix in version string; SHA-256 field contains formatting junk;
`status.code` must be checked; missing `"AMI BIOS"` key possible on non-MB products.

### 2. ASUS — JSON API ✅ verified live 2026-08-11 (corrects earlier assumptions below)

Support URL pattern (examples):
`https://www.asus.com/motherboards-components/motherboards/<series>/<model-slug>/`
ModelId = last non-empty path segment of the product URL (e.g.
`rog-strix-x670e-e-gaming-wifi`), passed to the API as-is (it is case-insensitive).

```
GET https://rog.asus.com/support/webapi/product/GetPDBIOS?website=global&model=<MODEL>&pdid=0&cpu=
```

**Correction:** `www.asus.com/support/api/product.asmx/GetPDBIOS` (the endpoint this
doc originally listed as primary) returns `{"Status":"FAIL"}` for every model tried,
ROG or not — it appears dead/deprecated as of 2026-08-11. `rog.asus.com`'s webapi is
the one that actually works, and it works for **all** ASUS boards, not just ROG ones
(verified live with both a ROG board and a plain PRIME board). `AsusBiosSource` calls
`rog.asus.com` exclusively.

Relevant response fields (field names/casing below are exact, captured from a real
response):

```jsonc
{
  "Status": "SUCCESS",
  "Result": {
    "Obj": [
      {
        "Name": "BIOS",                    // <-- use this group
        "Files": [ {
          "Version": "3902",
          "ReleaseDate": "2026/07/16",     // yyyy/MM/dd  (different from MSI!)
          "Description": "\"...html with <br/> and <a> tags...\"",  // note: some entries
                                            // literally wrap the text in a leading/trailing
                                            // `"` character as part of the content itself —
                                            // strip that too, not just the HTML tags
          "FileSize": "17.4 MB",           // string, not bytes — convert via a shared
                                            // KB/MB/GB parser (also used by Gigabyte)
          "DownloadUrl": { "Global": "https://dlcdnets.asus.com/..." },
          "sha256": "AC9FA522D1663FB244B14104F40DF6021A82DBBA538ACCB5F8EFC9FC0C304D13",
          "IsRelease": "1"                 // "0" seen on short-lived builds superseded
                                            // within days by an "1" version — undocumented
                                            // by ASUS but the best available beta signal;
                                            // AsusBiosSource treats "0" as IsBeta=true
        } ]
      },
      { "Name": "Firmware", ... }          // group name for non-BIOS extras varies by
                                            // product (seen "Firmware" and "BIOS
                                            // Update(Windows)") — always filter by
                                            // Name == "BIOS", never by excluding names
    ]
  }
}
```

There is no reliable clean "board title" field anywhere in this response (`Title` is
empty on most entries, present-but-inconsistent on others) — `AsusBiosSource` derives a
display name from the ModelId slug instead (`rog-strix-x670e-e-gaming-wifi` →
`ROG STRIX X670E E GAMING WIFI`); it's a reasonable default, not a claim of exactness,
and the user can rename it in the Add/Edit dialog regardless.

Gotchas: multiple groups in `Obj` — select `Name == "BIOS"` (the EZ-Flash zip) by name,
don't try to exclude known non-BIOS group names since they vary; date format
`yyyy/MM/dd`; `Description` is HTML plus a literal quote-wrapper on some entries;
`FileSize` is a display string; `sha256` can be an empty string (not absent) — treat as
null; the model param must match ASUS's slug or the result is `{"Status":"FAIL"}` —
treat that as "board not found", show error in Add dialog.

### 3. Gigabyte — server-rendered HTML ✅ verified via Wayback Machine archive (not live — see caveat)

Support URL pattern: `https://www.gigabyte.com/Motherboard/<MODEL>-rev-<REV>/support`
(e.g. `X670E-AORUS-MASTER-rev-1x`). The **revision suffix is part of the identity** —
store the full path segment as ModelId. Strip a leading `/us` or other locale segment
and always fetch the global page for consistency.

**Verification caveat:** `gigabyte.com` 403s from the dev sandbox for every request
(same Akamai-style WAF issue as `msi.com` — see the M1 environment caveat above), so
the markup below was captured from a real Wayback Machine snapshot of an actual Gigabyte
support page (X670E AORUS MASTER, archived 2024-01-18), not the live current site. It's
real markup, not a guess, but the live DOM could have changed since that archive date —
worth a spot-check against the current site if picked up somewhere network access to
gigabyte.com actually works.

The BIOS table is present in the static HTML (no JS needed). Real structure (verified,
not assumed): a `<ul id="BIOSHide">` contains a `<div class="div-table">` of row divs.
**The header row and data rows share the same cell classes** (`download-version`,
`download-date`, etc.) — only the row's own class distinguishes them
(`div-table-row div-table-header` vs `div-table-row div-table-body-BIOS`). Selecting by
cell class alone (e.g. all `.download-version` divs) picks up the header's literal
"Version" label as a fake entry — select rows by `div-table-body-BIOS` first, then read
cells from within each row.

Per BIOS row:
- `.download-version` — version text (e.g. `F21a` — lowercase letter suffixes are
  typically beta/preview builds; Gigabyte does not label betas explicitly, so do NOT
  auto-classify; `GigabyteBiosSource` always sets `IsBeta = false`)
- `.download-date` — **`MMM dd, yyyy` en-US, zero-padded day** (e.g. `Jun 07, 2023`, not
  `Jun 7, 2023` — this doc originally assumed non-padded `MMM d, yyyy`; the archive shows
  real zero-padded dates, so `GigabyteBiosSource` tries both patterns via
  `DateOnly.TryParseExact` with a format array). Never parse with the system culture
  (German locale would break it) — always `CultureInfo.InvariantCulture`.
- `.download-size` — display string like `10.34 MB`, same shared KB/MB/GB→bytes parser
  as ASUS.
- `.download-site` → `.//a/@href` — direct `download.gigabyte.com/FileList/BIOS/...` URL.
- `.download-desc` — an `<ol>` whose first `<li>` is the checksum line, exact real text
  `Checksum : XXXX` (**space before the colon** — this doc originally paraphrased it as
  `Checksum: XXXX`); remaining `<li>`s are the changelog. **Footnotes referenced from a
  bullet (a trailing `*`) commonly live *outside* the `<ol>`** — as a bare trailing text
  node, a `<p>`, or a `<div>`, sibling to the `<ol>` inside the same `.download-desc` div
  — and were dropped entirely in an earlier draft of this parser that only read `<li>`
  elements. `GigabyteBiosSource.ParseDescription` now also walks the non-`<ol>` children
  of `.download-desc` and appends any non-empty text, so footnotes survive into the
  changelog.

Gotchas: parse defensively — Gigabyte's markup can change without notice (more true than
usual here, since it was verified against a ~2-year-old archive, not the live site);
`BiosWatcher.Tests/Fixtures/gigabyte_x670e_aorus_master.html` is trimmed real markup
(wayback URL-rewrite prefixes stripped back out) — extend it rather than hand-writing
new fixture markup from scratch if the real DOM needs re-verifying later. Entries are
newest-first but sort by parsed date anyway.

### 4. ASRock — AJAX HTML fragment ✅ verified against a real live response

Product URL pattern: `https://www.asrock.com/mb/<PLATFORM>/<MODEL>/index.asp`
(e.g. `/mb/AMD/X670E%20Taichi/index.asp` — model names contain URL-encoded spaces).

**Verification history (2026-08-11):** the static HTML of the product page does NOT
contain the BIOS table — it shows a "Loading for BIOS" placeholder and loads it via AJAX
after page load. A Wayback Machine archive of a product page's JS first confirmed the
mechanism (not the markup):

```js
}else if(hash=='BIOS'){
  if($('#BIOS').html().search('loading')!=-1){ $('#BIOS').load('BIOS.html') };
  $('#BIOS').fadeIn();
}
```

`$(...).load(url)` is jQuery for "GET this URL, inject the response HTML here" — so the
real request is `GET https://www.asrock.com/mb/<PLATFORM>/<MODEL>/BIOS.html` (a sibling
resource next to the product's own `index.asp`), returning an **HTML fragment, not
JSON**. `www.asrock.com` itself 403s under Incapsula from this dev sandbox for most
requests (confirmed via both `curl` and .NET's `HttpClient` — though not reliably: a live
Add-board attempt from this exact sandbox succeeded once and a detail-view re-fetch
seconds later on the same board 403'd, so **treat Incapsula's block here as intermittent,
not a hard wall** — retry before concluding an endpoint is unreachable), and archived
snapshots don't cover an AJAX-only URL nothing links to — so the fragment's *markup*
stayed unverified through the first implementation pass, which shipped as a
recognized-but-unsupported stub rather than guess at it.

The real markup was then obtained directly: a live browser session's DevTools → Network
→ the `BIOS.html` response, pasted in verbatim. Real, verified structure (all four points
below differ from what a reasonable guess would have produced — this is exactly why the
project's "never guess, verify first" rule exists):

```html
<table>
  <thead>
    <tr><th>Version</th><th>Date</th><th>Size</th><th>Update method</th><th>Description</th><th colspan=2>Download</th></tr>
  </thead>
  <tbody>
    <tr align=center>
      <td>4.43</td>                                  <!-- or e.g. "3.18.AS02<br><font>[Beta]</font>" -->
      <td>2026/6/30</td>                              <!-- yyyy/M/d, NOT zero-padded -->
      <td>19.13MB</td>                                <!-- no space before the unit -->
      <td><a href="...BIOSIG.asp...">Instant Flash</a><a href="...FlashbackSOP.pdf">Flashback</a></td>  <!-- not release data, skip -->
      <td align=left>
        1. Update AGESA to ComboAM5 1.3.0.1b Patch A<br>
        2. Support for TSME ... <br>
        <font style="color:gray;">SHA256: 4e22d9dc...</font>    <!-- SHA256 lives INSIDE the description cell -->
      </td>
      <td width=68><a href="https://download.asrock.com/BIOS/AM5/X670E Taichi(4.43)ROM.zip">Global</a></td>
      <td width=68><a href="ftp://asrockchina.com.cn/...">China</a></td>                                 <!-- FTP mirror, skip -->
    </tr>
  </tbody>
</table>
```

No CSS classes on any cell — `AsrockBiosSource.ParseRow` reads columns positionally
(`td[0]`..`td[6]`), which is more brittle to markup reordering than the class-based
selectors used for Gigabyte, but there's nothing else to hook into. Gotchas actually
encountered, not hypothetical:
- **Date is `yyyy/M/d`, not zero-padded** (`2022/11/1`, `2024/1/19`) — the opposite of
  Gigabyte, which *is* zero-padded (`Jun 07, 2023`). Vendors don't share a convention;
  don't assume one.
- **Size has no space before the unit** (`19.13MB` vs ASUS/Gigabyte's `17.4 MB` /
  `10.34 MB`) — `DownloadSizeParser`'s regex already used `\s*` (zero-or-more), so this
  needed no change, but would have silently failed to parse with a naive `\s+`.
- **Beta is a version-cell suffix, not a column**: `<br><font>[Beta]</font>` appended to
  the version text (e.g. `3.18.AS02<br><font>[Beta]</font>`) — detected via a `[Beta]`
  substring check, then stripped back out of the version string.
- **SHA256 is embedded inside the Description cell**, not its own column — the last line
  is `<font style="color:gray;">SHA256: <hash></font>`; split it out via regex and keep
  everything before it as the changelog. A `<div class="Remark">` footnote can also
  appear inline within the description (parallels Gigabyte's out-of-`<ol>` footnotes) —
  handled the same way: strip tags rather than try to enumerate every element type.
- Two download links per row (Global HTTPS, China FTP) — use Global; FTP links may not
  open cleanly via `Process.Start`/shell association on every system.

`HtmlTextUtils.cs` (shared `<br/>`-to-newline + tag-stripping) was extracted since ASUS
needed the identical logic — same reuse threshold already applied to `DownloadSizeParser`
in M4.

### 5. Biostar — AJAX HTML fragment ✅ verified live 2026-08-12 (no WAF block hit, unlike MSI/Gigabyte/ASRock)

Product URL pattern: `https://www.biostar.com.tw/app/{locale}/mb/introduction.php?S_ID=<ID>`
(e.g. `/app/en/mb/introduction.php?S_ID=1079`). ModelId = the numeric `S_ID` query param —
Biostar's product identity is a bare integer, not a slug, unlike every other vendor here.
`BiostarBiosSource` always re-fetches the `/app/en/` (English) pages regardless of the
pasted URL's locale segment, same reasoning as Gigabyte always using its global page.

**Verification history:** unlike msi.com/gigabyte.com/asrock.com, this sandbox's own
`curl`/`HttpClient` calls to `biostar.com.tw` succeed directly with the shared browser
User-Agent — no WAF/Incapsula block encountered, on any request tried. The DOWNLOAD tab on
a product page AJAX-loads a sibling `mb_download.php?S_ID=<ID>` fragment (discovered from
the page's own tab-switch JS comment, `<!-- 此區塊為 ajax -->` — "this block is ajax" — same
discovery method as ASRock's `BIOS.html`), which is genuine server-rendered HTML, not JSON.
Checked against **two different real boards** (X670E VALKYRIE / AM5, Z890 VALKYRIE /
LGA1851) to make sure nothing below was an artifact of a single sample.

The fragment holds *every* download category (Manual, BIOS, chipset/LAN/audio drivers,
utilities) as sibling `<div class="tab-box">` sections that all reuse identical
`tab-title`/`table`/`thead`/`tbody`/`tr`/`td` markup with no distinguishing class of their
own — the BIOS table must be selected by its `tab-title`'s text ("BIOS" exactly; other
sections like "BIOSCreen Utility" and "BIOS Update Utility" also start with "BIOS" and
would false-match a `contains()` check), not assumed to be "the" table on the page.

Per BIOS row, cells are matched by `rwd-title` attribute (`Version`, `Description`,
`File Size`, `Date`, `Download`), not by CSS class:

- `Version` — plain string, e.g. `X67AE416.BST`.
- `Date` — `yyyy-MM-dd`, zero-padded (real ISO format — the one vendor of the five that
  doesn't need a custom `DateOnly` format string).
- `Description` — changelog text, sometimes containing `<br>` line breaks and even raw
  unescaped `<a href=...>` links with **unquoted attribute values** (seen on the Z890
  board) — handled by the same `HtmlTextUtils.StripHtml` used for ASUS/ASRock, which just
  regexes out any tag regardless of attribute quoting, so the malformed markup doesn't
  matter.
- `File Size` — display string parsed by the existing shared `DownloadSizeParser`.
  **Real, verified data-quality caveat**: on both sampled boards, the overwhelming
  majority of BIOS rows (19 of 19 on the X670E VALKYRIE) show the *identical* value
  `32768 KB` (exactly 32 MB) — while a live `HEAD` request against the actual downloadable
  zip for that row returned a real `Content-Length` of ~17.9 MB. This is Biostar's own
  data being wrong/a stale placeholder, not a parser bug — the field is parsed and
  displayed as-is (same as every other vendor's display-string size), just don't trust it
  as exact for this vendor.
- **No SHA256/checksum field exists anywhere in the fragment** — confirmed absent on both
  sampled boards. `BiosRelease.Sha256` is always `null` for Biostar.
- **No beta signal of any kind** — no version suffix, no separate flag, confirmed absent on
  both boards (one AMD, one Intel, spanning firmware from 2022–2026). `IsBeta` is always
  `false`, same reasoning as Gigabyte's letter-suffix versions not being auto-classified.
- **The Download cell has no real `href` at all** — it's
  `onclick="openLightboxWithParameters(id, 'X67AE416.BST', 'X67AE416BST.zip', 'Y')"`, a JS
  lightbox call. Reading the actual (obfuscated-by-minification-only) function body showed
  it fills in `../../../upload/Bios/<zipName>` at click time. The real absolute URL,
  **confirmed with a live HTTP request** (200 OK, `Content-Type: application/zip`), is
  `https://www.biostar.com.tw/upload/Bios/<zipName>` — extracted via regex against the
  `onclick` text, not an `href` attribute that doesn't exist. (A `download.biostar.com.tw`
  subdomain also exists but 404s for BIOS files — it's used for a different, unrelated
  driver-update tool link elsewhere on the page. Don't assume it without checking.)
- **Two trailing rows in the BIOS section aren't releases at all** — a "Smart BIOS update"
  SOP guide and a BIOS-update-manual PDF, both static reference documents Biostar appends
  to the *same* tab-box's `tbody`. They use `rwd-title="Title"`/`"System"` cells instead of
  `"Version"`/`"Description"` (and one even has a literal unescaped `<` inside its text
  content — `<p><BIOS Update Manual</p>` — real markup, not a transcription error).
  `ParseReleases` filters rows by the presence of a `Version` cell rather than assuming
  every `<tr>` under the BIOS tab-box's `tbody` is a real release — the naive approach
  would have picked up two fake "releases" with a non-zero-padded date (`2012-05-1`) that
  turned out to just be another symptom of this same row-type mixing.

There's no dedicated official-title API field either — `ParseTitle` reads the product
page's own breadcrumb/heading block (`div.info-text > div.main > p`), confirmed identical
in structure across both sampled boards.

`BiosWatcher.Tests/Fixtures/biostar_x670e_valkyrie_download.html` and
`biostar_x670e_valkyrie_intro.html` are trimmed real markup from the X670E VALKYRIE pages
(non-BIOS driver/utility tab-box sections and 2 of 3 Manual rows trimmed for size; all 19
real BIOS rows plus both trailing guide-PDF rows kept intact, since those are exactly what
the row-filtering logic needs to be tested against).

---

## Config File

Location: `%APPDATA%\BiosWatcher\config.json`. Atomic write (write `.tmp`, then
`File.Replace`) so a crash mid-write cannot corrupt the config.

```jsonc
{
  "settings": {
    "checkIntervalHours": 24,
    "ignoreBetaVersions": true,      // applies where the vendor marks betas (MSI, ASUS)
    "newFlagDays": 14
  },
  "boards": [
    {
      "id": "c6f1...",                          // Guid
      "vendorId": "msi",                        // msi | asus | gigabyte | asrock | biostar
      "modelId": "MAG-X670E-TOMAHAWK-WIFI",     // vendor-specific identifier
      "displayName": "MAG X670E TOMAHAWK WIFI", // from vendor data, user-editable
      "supportUrl": "https://www.msi.com/Motherboard/MAG-X670E-TOMAHAWK-WIFI/support#bios",
      "lastKnownVersion": "7E12v1L2",
      "lastKnownReleaseDate": "2026-07-06",
      "lastChecked": "2026-08-11T14:32:00Z",
      "lastCheckFailed": false,
      "notifiedVersions": ["7E12v1L2"]          // already-announced versions → no repeat toasts
    }
  ]
}
```

## Features (v1 scope)

1. **Board list (main window):** DataGridView with columns
   `Vendor | Name | Latest Version | Release Date | NEW | Last Checked | Status`.
   Rows with an active NEW flag highlighted (color + "NEW" text, not color alone).
2. **Add board:** one textbox accepting a support/product URL from any of the four
   vendors. `VendorRegistry` auto-detects the vendor via `CanHandle`; on add, fetch once
   to validate and get the official title + current latest version (baseline). Reject
   duplicates by (vendorId, modelId). Unknown vendor → clear error message listing
   supported URL formats.
3. **Edit board:** rename `displayName`, fix the URL.
4. **Delete board:** with confirmation dialog.
5. **Check now:** all boards; per-board context-menu "Check this board". Sequential
   with ~500 ms delay between requests (be polite; also avoids per-vendor rate limits).
6. **Detail view:** double-click → changelog, size, SHA-256/checksum where available,
   "Download BIOS" / "Open support page" links via `Process.Start`
   (`UseShellExecute = true`). The app opens links — it never downloads or flashes.
7. **New-version detection:** latest applicable release (respecting the beta filter)
   differs from `lastKnownVersion` AND not in `notifiedVersions` → tray balloon,
   append to `notifiedVersions`, update `lastKnownVersion`/`lastKnownReleaseDate`.
8. **NEW flag rule:** `IsNew = (DateOnly.Today - lastKnownReleaseDate).Days <= newFlagDays`.
   Computed at render time; nothing persisted, nothing to clean up.
9. **Background checking:** minimize to tray (`NotifyIcon`); `System.Windows.Forms.Timer`
   triggers a full check every `checkIntervalHours`.
10. **Failure handling:** on any fetch/parse error keep last known data, set
    `lastCheckFailed`, show a warning icon in the row. Never crash, never wipe state
    because one vendor's site changed or was down. Vendor failures are independent —
    one broken source must not block the others.

## Non-Goals (v1)

- No automatic BIOS downloading or flashing (safety; notification + link only).
- No further vendors beyond MSI/ASUS/Gigabyte/ASRock/Biostar for v1 (ECS, Colorful, Supermicro, ...) —
  but each is just one more `IBiosSource`. NZXT deliberately excluded even as a future candidate: its
  N7 boards are rebranded ASRock hardware, not an independent design, so there's no distinct vendor BIOS
  source to build against.
- No installer.

## Actual Project Structure (current — see Current Status for what's implemented)

```
AppBios/                           // repo root; not yet a git repo
  global.json                      // pins SDK to 9.0.316, see Current Status
  BiosWatcher.slnx                 // not .sln — newer dotnet template default
  BiosWatcher/
    Program.cs
    Models/
      AppConfig.cs                 // AppSettings + AppConfig
      Board.cs                     // persisted board entry + derived IsNew()
      BiosRelease.cs
      BoardRef.cs
      BoardInfo.cs
    Services/
      ConfigRepository.cs
      DateOnlyJsonConverter.cs     // not in original plan; forces ISO+InvariantCulture
      HttpClientProvider.cs        // not in original plan; single shared HttpClient
      UpdateChecker.cs
      VendorRegistry.cs
      Sources/
        IBiosSource.cs
        MsiBiosSource.cs
        MsiApiModels.cs            // not in original plan; internal MSI JSON DTOs
        MsiWebViewFetcher.cs       // not in original plan; WebView2-based fetch, MSI's Akamai WAF blocks HttpClient
        AsusBiosSource.cs
        AsusApiModels.cs           // not in original plan; internal ASUS JSON DTOs
        GigabyteBiosSource.cs
        DownloadSizeParser.cs      // not in original plan; shared "17.4 MB"/"19.13MB" → bytes parser (ASUS + Gigabyte + ASRock + Biostar)
        HtmlTextUtils.cs           // not in original plan; shared <br/>-to-newline + tag-strip (ASUS + ASRock + Biostar)
        AsrockBiosSource.cs
        BiostarBiosSource.cs
    Forms/
      MainForm.cs / MainForm.Designer.cs
      AddEditBoardForm.cs / AddEditBoardForm.Designer.cs
      BoardDetailForm.cs / BoardDetailForm.Designer.cs
      SettingsForm.cs / SettingsForm.Designer.cs
  BiosWatcher.Tests/
    Fixtures/
      msi_mag_b650_tomahawk_wifi.json
      asus_rog_strix_x670e_e_gaming_wifi.json
      gigabyte_x670e_aorus_master.html
      asrock_x670e_taichi_bios.html
      biostar_x670e_valkyrie_download.html
      biostar_x670e_valkyrie_intro.html
    MsiParserTests.cs               // 11 tests
    UpdateCheckerTests.cs           // 12 tests
    AsusParserTests.cs              // 13 tests
    GigabyteParserTests.cs          // 14 tests
    AsrockParserTests.cs            // 15 tests
    BiostarParserTests.cs           // 18 tests
```

## Milestones

1. **M1 – Core plumbing + MSI: ✅ done.** models, `ConfigRepository` (atomic write),
   `IBiosSource`/`VendorRegistry`, `MsiBiosSource` against the live API. Fixture-based
   unit tests for the parser. (Live-API happy path still needs a real-machine check —
   see Current Status above.)
2. **M2 – Main UI: ✅ done.** board list, add/edit/delete with vendor auto-detect, manual
   "Check now", NEW highlighting, detail view.
3. **M3 – Notifications & tray: ✅ done.** NotifyIcon, balloon on new version, timer-based
   periodic check, `notifiedVersions` de-duplication.
4. **M4 – ASUS + Gigabyte sources: ✅ done.** `AsusBiosSource` (endpoint corrected to
   `rog.asus.com` after live verification — see vendor section), `GigabyteBiosSource`
   (HtmlAgilityPack + a fixture built from real archived markup, since gigabyte.com
   itself is WAF-blocked from this sandbox). Per-vendor date-format handling with
   InvariantCulture.
5. **M5 – ASRock: ✅ done — real implementation, built against a genuine live response**
   (the user pulled the actual `BIOS.html` fragment via their own browser's DevTools and
   pasted it in, after this sandbox's own access proved intermittently Incapsula-blocked
   — see vendor section for the real markup quirks that turned up). All four vendors now
   have working sources.
6. **M6 – Polish: ✅ done.** `SettingsForm` (interval/beta-toggle/newFlagDays), tooltip-based
   error indicators on failed check rows, empty-state hint on the board grid — all verified
   live via a UI-Automation-driven screenshot pass, not just code review.
7. **M7 – Biostar (5th vendor): ✅ done.** `BiostarBiosSource` against a genuine AJAX HTML
   fragment (`mb_download.php`), verified live against two real boards (AMD + Intel) with no
   WAF block encountered — see vendor section for the row-filtering, size-placeholder, and
   onclick-lightbox-download-URL quirks that turned up, none of which a guess would have
   gotten right. **Verified beyond fixtures**: a live UI-Automation-driven click-through
   actually added a real board (`https://www.biostar.com.tw/app/en/mb/introduction.php?S_ID=1079`)
   through the running app's own Add-board dialog — real network fetch, real parse, real
   persisted `config.json` entry, real detail-view re-fetch showing the full release
   history/changelog/size (SHA-256 correctly shown as "not provided") — then the test board
   was removed and the user's original `config.json` restored afterward.
8. **M8 – Fix MSI's Akamai block: ✅ done.** A real user hit a `403` adding their own board
   through the published exe — not a sandbox artifact after all (see the M1 caveat and MSI
   vendor section above/below for the full diagnosis). `MsiBiosSource` now fetches through
   `MsiWebViewFetcher` (an off-screen WebView2/Chromium control) instead of `HttpClient`.
   **Verified against the user's own reported-failing URL**
   (`https://de.msi.com/Motherboard/MAG-X670E-TOMAHAWK-WIFI/support#bios`), twice in a row,
   from the same sandbox that had 403'd every prior `curl`/`HttpClient` attempt at msi.com
   throughout this entire project.

## Conventions for Claude Code

- All parsing (`DateOnly.ParseExact`, number parsing) with `CultureInfo.InvariantCulture`
  — the dev machine runs a German locale; culture bugs must be impossible by design.
- UI updates on the UI thread; network/parsing off it.
- NuGet: HtmlAgilityPack, and Microsoft.Web.WebView2 (MSI only — see Tech Stack). Anything
  else needs explicit agreement.
- Each vendor source must be testable against saved fixture files without network.
- Never overwrite `config.json` with an empty model after a failed load — back up the
  unreadable file as `config.json.bak` and start fresh.
- German date display in the UI is fine (`dd.MM.yyyy`); storage is ISO.
- HTTP: one shared HttpClient; set `User-Agent` to a current browser string; timeout
  ~15 s; treat non-2xx and schema surprises as per-board soft failures.
