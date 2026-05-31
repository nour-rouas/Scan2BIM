# Scan2BIM

**Open-source Revit plugin for point cloud measurement and scan-to-BIM workflows.**

Built by **Nour Rouas** — Revit Modeler & Developer.

**Landing page:** enable GitHub Pages from the `/docs` folder, or open [`docs/index.html`](docs/index.html) locally.

## Supported versions

| Revit | Status |
|-------|--------|
| 2024  | Supported |
| 2025  | Supported |
| 2026  | Supported |
| 2027+ | Add the year to `RevitVersions.txt` and create `Metrika.20XX.addin` |

Each Revit year needs its own build (Revit API is version-specific). The same source code compiles for all supported versions.

## Features

- **Hide / show point clouds** in the active view
- **Pick coordinates** on point clouds (X, Y, Z)
- **Measure points** on building element faces (floors, walls, roofs, stairs, toposolids, etc.)
- **Export / import markers** as CSV or JSON

## Download (end users)

1. Go to [GitHub Releases](https://github.com/nourrouas/scan2bim/releases) (update the URL after publishing).
2. Download the zip for your Revit version (`Scan2BIM-Revit2024.zip`, etc.).
3. Extract and run `install.ps1` (close Revit first).
4. Open Revit — look for the **Scan2BIM** ribbon tab.

## Build from source (developers)

**Requirements:** Windows, Visual Studio with .NET desktop development, and the Revit version(s) you want to target installed.

```powershell
# Deploy to local Revit Addins folders (all installed versions from RevitVersions.txt)
.\deploy.ps1

# Create release zip packages in dist/
.\build-release.ps1

# Build a single version
msbuild FloorToPointCloud.sln /p:Configuration=Release /p:RevitVersion=2025
```

### Adding a new Revit year (e.g. 2027)

1. Add `2027` to `RevitVersions.txt`
2. Copy `Metrika.2026.addin` → `Metrika.2027.addin`
3. Run `.\build-release.ps1`

## Project structure

```
FloorToPointCloud/
├── App.cs                 # Ribbon setup
├── Commands/              # Revit external commands
├── Utils/                 # Helpers (icons, markers, geometry)
├── Metrika.20XX.addin     # Per-version add-in manifests
├── RevitVersions.txt      # Supported Revit years
├── deploy.ps1             # Build + deploy locally
├── build-release.ps1      # Build + package zips
├── scripts/               # Shared build/install helpers
└── docs/index.html        # Landing page (GitHub Pages)
```

## Publish the landing page (GitHub Pages)

1. Push this repo to GitHub (e.g. `nourrouas/scan2bim`).
2. **Settings → Pages → Build from branch → `/docs` → Save.**
3. Your site will be at `https://nourrouas.github.io/scan2bim/`.

Update GitHub URLs in `docs/index.html` if your username or repo name differs.

## Publish a release

```powershell
.\build-release.ps1
# Upload dist/*.zip to a new GitHub Release tagged v1.0.0
gh release create v1.0.0 dist/*.zip --title "Scan2BIM v1.0.0"
```

## License

MIT — see [LICENSE](LICENSE).

---

*Not affiliated with Autodesk.*
