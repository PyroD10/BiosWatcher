# BiosWatcher

A Windows desktop app that monitors a user-maintained list of motherboards across
MSI, ASUS, Gigabyte, ASRock, and Biostar, and notifies you when a new BIOS version
is released. Single user, local machine, no server component.

## Features

- Add boards by pasting a support/product URL; vendor is auto-detected.
- Tray notification on new BIOS releases, with periodic background checks.
- Detail view with changelog, file size, and SHA-256/checksum where available.
- Never downloads or flashes BIOS files — notification and links only.

## Tech Stack

- C# / WinForms (.NET, `net9.0-windows`)
- `HttpClient` for most vendors; `Microsoft.Web.WebView2` for MSI (its Akamai WAF
  blocks plain HTTP clients)
- `HtmlAgilityPack` for HTML-based vendor sources
- Config stored as JSON in `%APPDATA%\BiosWatcher\config.json`

## Building

```
dotnet build BiosWatcher.slnx
dotnet test BiosWatcher.slnx
```

`global.json` pins the SDK to `9.0.316`.

See `CLAUDE.md` for full project details, vendor data source notes, and history.
