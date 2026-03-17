# Flawless Pictury

Flawless Pictury is a WinForms-based, preset-driven batch image processing application built on .NET Framework 4.8.
The solution separates application contracts, infrastructure, plugins, and the WinForms shell so pipeline logic stays reusable and UI-agnostic.

## Runtime notes

Some presets rely on external tools such as ImageMagick or ExifTool. Those executables are not bundled in this repository and must be provided separately when required by the selected preset.

Extracted dependencies to these folders:

.\Tools\ImageMagick
(magick.exe must be present)

.\Tools\ExifTool
(exiftool.exe must be present)

## Solution structure

- `FlawlessPictury.Presentation.WinForms` — WinForms views, presenters, and UI-specific services
- `FlawlessPictury.AppCore` — application contracts, pipeline execution, plugin abstractions, presets, and stats contracts
- `FlawlessPictury.Infrastructure` — file system, plugin loading, preset persistence, safe-output support, and stats sinks
- `FlawlessPictury.Plugins.*` — tool-specific capability providers and step implementations
- `FlawlessPictury.Domain` — shared domain-level assembly reserved for reusable domain concerns

## Build requirements

- Visual Studio 2022 or later with .NET desktop development tools
- .NET Framework 4.8 targeting pack
