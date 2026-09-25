# DevSentinel 🛡️

A Windows developer-environment health monitor. Scans your machine's dev
toolchain and produces a single **Developer Environment Health Score**
(0–100, graded A–F), with an optional exported report (JSON / HTML / plain text).

Ships as **one portable `.exe`** — self-contained, no .NET runtime install
required on the target machine, nothing to unzip alongside it.

## What it checks

| Category            | What it does                                                              |
|----------------------|----------------------------------------------------------------------------|
| .NET SDKs            | Lists installed SDKs/runtimes via `dotnet --list-sdks` / `--list-runtimes` |
| Node / Python / Git / Docker | Detects each CLI and its version                                   |
| PATH Integrity        | Flags PATH entries pointing at missing, duplicate, or blank directories   |
| Port Usage             | Reports which common dev ports (3000, 5432, 8080, 27017, ...) are busy   |
| Dependency Tooling      | Detects npm / yarn / pnpm / pip / NuGet                                 |
| Disk Space              | Flags fixed drives with critically/warningly low free space             |

Each category contributes a weighted score toward the overall health score.

## Build (produces the portable .exe)

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download) on the build
machine (not on the machine that will *run* the tool — that one needs nothing).

```powershell
./build.ps1
```

This runs `dotnet publish` with self-contained + single-file settings and
drops the result at `publish\DevSentinel.exe`. Copy that one file anywhere —
a USB stick, another machine, a CI runner — and run it directly.

Manual equivalent, if you'd rather not use the script:

```powershell
dotnet publish DevSentinel.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

> The project targets `win-x64` by default. For ARM64 Windows, pass
> `-r win-arm64` instead (edit `build.ps1` or the manual command above).

## Run

```powershell
DevSentinel.exe
```

```
» Running checks...
[ OK ] .NET SDKs              3 SDK(s) installed (latest: 8.0.100)
[ OK ] Node.js                v20.11.0
[WARN] PATH Integrity         PATH has 2 minor issue(s): 2 missing, 0 duplicate, 0 blank
...
=========================================
  DEVELOPER ENVIRONMENT HEALTH SCORE: 87/100 (Grade B)
=========================================
```

### Options

```
--export <fmt>     Export report. fmt = json | html | txt | all
                    May be passed multiple times, e.g. --export json --export html
--output <dir>     Directory to write exported report(s) to (default: current directory)
--quiet            Suppress console progress output (exit code still reflects health)
--help             Show help
```

Examples:

```powershell
DevSentinel.exe --export html
DevSentinel.exe --export all --output C:\Reports
DevSentinel.exe --quiet --export json    # good for a scheduled task
```

### Exit codes

- `0` — no critical issues found
- `1` — at least one critical issue found (useful in CI / scheduled tasks)

## Project structure

```
DevSentinel/
├── DevSentinel.csproj      # single-file/self-contained publish settings
├── Program.cs               # entry point: orchestrates checks, printing, export
├── CliOptions.cs             # CLI argument parsing
├── Checks/
│   ├── IHealthCheck.cs        # contract every check implements
│   ├── ProcessRunner.cs        # safe external-process execution helper
│   ├── DotNetSdkCheck.cs
│   ├── ToolVersionCheck.cs      # generic Node/Python/Git/Docker check
│   ├── PathIntegrityCheck.cs
│   ├── PortUsageCheck.cs
│   ├── DependencyCheck.cs
│   └── DiskSpaceCheck.cs
├── Models/
│   ├── CheckStatus.cs
│   ├── HealthCheckResult.cs
│   └── EnvironmentReport.cs
├── Services/
│   ├── HealthScoreCalculator.cs   # weighted 0-100 score + letter grade
│   ├── ReportExporter.cs           # JSON / HTML / text export
│   └── ConsoleUi.cs                  # colored console output
└── build.ps1                # publish script -> single portable .exe
```

## Extending it

Add a new check by implementing `IHealthCheck` (one method: `RunAsync()`
returning a `HealthCheckResult`) and registering it in `BuildChecks()` in
`Program.cs`. Nothing else needs to change — scoring, console output, and
export all work generically off the `HealthCheckResult` list.
