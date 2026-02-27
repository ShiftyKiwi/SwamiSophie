# Release Process

## 1. Bump the version

Before creating a release, update the plugin version in the project metadata you use for packaging.

Recommended place:

- [`src/EorzeanMegaArcana/EorzeanMegaArcana.csproj`](./src/EorzeanMegaArcana/EorzeanMegaArcana.csproj)

The project currently defines version metadata directly in the project file.

Keep the Git tag aligned with the release version, for example `v1.0.0.1`.

## 2. Build the release package

Run a Release build from the repository root:

```powershell
dotnet build SwamiSophie.sln -c Release -clp:ErrorsOnly
```

The repository pins the SDK in [`global.json`](./global.json), so local builds and CI should use the same .NET SDK version.

## 3. Release output layout

The Release build produces the packaged output under:

- `src/EorzeanMegaArcana/bin/Release/SwamiSophie/latest.zip`
- `src/EorzeanMegaArcana/bin/Release/SwamiSophie/SwamiSophie.json`

Other supporting build output is available under:

- `src/EorzeanMegaArcana/bin/Release/`

The current packaged zip contains the plugin DLL, generated manifest, and the JSON data files under `/data`.

## 4. Tag the release

Create and push a version tag:

```powershell
git tag v1.0.0.1
git push origin v1.0.0.1
```

## 5. Create a GitHub Release

### Manual

1. Open the repository on GitHub.
2. Create a new release from the tag, for example `v1.0.0.1`.
3. Upload:
   - `latest.zip`
   - `SwamiSophie.json`

### Automated

This repository includes a tag-driven GitHub Actions workflow at:

- [`.github/workflows/release.yml`](./.github/workflows/release.yml)

When you push a tag matching `v*`, it will:

1. restore dependencies
2. download the Dalamud dev files needed for CI builds
3. build `Release`
4. verify that `latest.zip` contains the plugin DLL, generated manifest, and JSON data files
5. run the Release test suite
6. create or update the GitHub Release
7. upload the packaged artifacts

## Notes

- CI and release packaging assume the current Release output layout from the Dalamud packager.
- Debug builds intentionally skip the Dalamud packager by default in this repository.
