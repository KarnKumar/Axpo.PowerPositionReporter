<div align="center">

# ⚡ Axpo.PowerPositionReporter

**A .NET 10 Worker Service that turns day-ahead power trades into hourly position reports — automatically, reliably, on schedule.**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](#)
[![Tests](https://img.shields.io/badge/tests-unit%20%2B%20BDD-2EA44F)](#-testing)
[![Resilience](https://img.shields.io/badge/resilience-Polly-blueviolet)](#-design-notes)
[![Logging](https://img.shields.io/badge/logging-Serilog-1370A1)](#-design-notes)

</div>

---

## 📑 Table of contents

- [What it does](#-what-it-does)
- [Project structure](#-project-structure)
- [Configuration](#-configuration)
- [Running](#-running)
- [Testing](#-testing)
- [Design notes](#-design-notes)
- [Samples](#-samples)
  - [Sample CSV output](#sample-csv-output)
  - [Sample console/log output](#sample-consolelog-output)
  - [Sample command-line overrides](#sample-command-line-overrides)

---

## 🚀 What it does

On a configurable interval — **immediate first run**, then every `IntervalMinutes` — the service:

1. **Fetch** — requests all trades for the next calendar day (UTC) from the external `PowerService`
   trading system, wrapped in a Polly retry pipeline with exponential backoff.
2. **Aggregate** — sums and rounds trade volumes into a single value per hourly period.
3. **Convert & name** — maps local trading periods to UTC datetimes; builds the file name
   `PowerPosition_{yyyyMMdd}_{yyyyMMddHHmm}.csv`.
4. **Write** — saves to a temp file, then atomically moves it into place, so readers never see a
   partial file. Every step is logged via Serilog (console + rolling file).

---

## 🗂 Project structure

| Project | Responsibility |
|---|---|
| 🧩 `Axpo.PowerPositionReporter.Domain` | Core models (`PowerTrade`) and interfaces (`IPowerTradeService`, `IReportWriter`, `IReportLogger`, `IPowerPositionReportService`) |
| ⚙️ `Axpo.PowerPositionReporter.Application` | Implementations: trade aggregation, CSV writing, Serilog logging, Polly resilience, configuration |
| 🏃 `Axpo.PowerPositionReporter.Worker` | Host entry point (`Program.cs`) and `BackgroundService` running the reporter loop |
| ✅ `Axpo.PowerPositionReporter.UnitTests` | Unit tests for aggregation, CSV writing, exception handling/wrapping, and the report loop |
| 🧪 `Axpo.PowerPositionReporter.IntegrationTests` | Reqnroll (BDD) feature tests exercising the full pipeline |
| 📦 `lib/PowerService.dll` | External Axpo trading-system assembly (`Axpo.IPowerService`) the app integrates with |

> **Layering:** `Domain` → `Application` → `Worker`, each depending only on the layer beneath it — keeping business logic decoupled from hosting and infrastructure concerns.

---

## 🔧 Configuration

Settings live under `PowerPositionReporter` in `appsettings.json` (Worker project):

```json
{
  "PowerPositionReporter": {
    "CsvReportPath": "C:\\Axpo\\report",
    "IntervalMinutes": 2,
    "MaxRetryAttempts": 3,
    "Logging": {
      "LogDirectory": "C:\\Axpo\\logs",
      "RetainedFileDays": 30,
      "OutputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    }
  }
}
```

| Setting | Description | Default | Valid range |
|---|---|---|---|
| `CsvReportPath` | Output directory for generated CSV reports | `./csvReport` | — |
| `IntervalMinutes` | Minutes between report generations | `60` | 1–1440 |
| `MaxRetryAttempts` | Retry attempts for transient trading-system failures | `3` | 1–10 |
| `Logging:LogDirectory` | Directory for rolling log files | `./logs` | — |
| `Logging:RetainedFileDays` | Number of days of logs to retain | `30` | — |

All settings can also be overridden via **environment variables** or **command-line arguments**
(see [Sample command-line overrides](#sample-command-line-overrides)).

---

## ▶️ Running

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet run --project Axpo.PowerPositionReporter.Worker
```

The worker runs continuously until cancelled (`Ctrl+C`) — generating a report immediately, then
again on every subsequent interval tick.

---

## 🧪 Testing

```bash
# Unit tests
dotnet test Axpo.PowerPositionReporter.UnitTests

# Integration / BDD tests (Reqnroll)
dotnet test Axpo.PowerPositionReporter.IntegrationTests
```

**Unit tests** (`Axpo.PowerPositionReporter.UnitTests`) cover the service layer in isolation, with mocked dependencies:

- ✔️ `PowerTradeServiceTests` — volume aggregation across multiple trades, fractional-volume rounding, and that an unexpected failure from the upstream `IPowerService` is wrapped in a `PowerServiceUnavailableException` (with the original exception preserved as `InnerException`) once the Polly retry pipeline is exhausted, rather than the raw exception leaking out.
- ✔️ `CsvReportWriterTests` — refuses to write when there are zero positions, writes rows ordered by period with correct UTC conversion, and that writing twice for the same minute overwrites cleanly with no leftover temp file.
- ✔️ `PowerPositionReporterTests` — the first iteration runs immediately without waiting for the interval, the day-ahead date is requested correctly, and that a failed extraction is logged and does **not** write a report, without crashing the worker host.

**Exception-handling design** (reflected in the tests above and worth knowing when extending them):

| Failure | Where it's caught | What's thrown/logged |
|---|---|---|
| Upstream `PowerService` failure (after retries exhausted) | `PowerTradeService` | Logged at `Error`, wrapped and rethrown as `PowerServiceUnavailableException` |
| CSV write failure (I/O, permissions, other) | `CsvReportWriter` | Logged at `Error`, wrapped and rethrown as `ReportWriteException` |
| Cancellation (`OperationCanceledException`) | All layers | Logged at `Information`, rethrown as-is (never wrapped) |
| A single scheduled report run failing (`PowerServiceUnavailableException` / `ReportWriteException` / anything else) | `PowerPositionReportService.GenerateAndWriteReportAsync` | Logged (`Warning` for expected/retryable failures, `Fatal` for unclassified ones); the loop continues to the next scheduled tick rather than crashing the worker. After 3 consecutive failed runs, an additional `Fatal` log is raised to flag a likely systemic problem rather than a transient blip |

The integration suite covers:

- ✔️ A day-ahead report containing 24 hourly positions
- ✔️ CSV file naming conventions
- ✔️ Resilience under repeated/transient upstream failures
- ✔️ A full single-iteration run of the reporter loop producing exactly one report file

---

## 🏗 Design notes

| Principle | How it's applied |
|---|---|
| **Clean separation (SOLID)** | Domain defines contracts only; Application implements them; Worker wires up DI and hosts the loop |
| **Resilience** | Trade retrieval runs through a Polly retry pipeline (exponential backoff, configurable attempts) via `AddPowerTradeResilience` |
| **Atomic writes** | CSV reports are written to a temp file, then moved into place — no partial/corrupt reads mid-write |
| **Structured logging** | Serilog logs to console + rolling daily file; retries, failures, and report events are logged at appropriate levels |

---

## 📊 Samples

### Sample CSV output

`PowerPosition_20261001_202609301800.csv` — day-ahead report for **2026-10-01**, generated **2026-09-30 18:00 UTC**:

```csv
Datetime;Volume
2026-09-30T22:00:00Z;-22
2026-09-30T23:00:00Z;-15
2026-10-01T00:00:00Z;-15
2026-10-01T01:00:00Z;10
2026-10-01T02:00:00Z;10
2026-10-01T03:00:00Z;10
...
2026-10-01T20:00:00Z;5
2026-10-01T21:00:00Z;5
```

Each row is one trading period (UTC); `Volume` is the summed, rounded volume across all trades for
that period. **Negative** = net sell position, **positive** = net buy.

### Sample console/log output

```text
[2026-06-30 09:00:00.012 INF] [STARTUP] Power Position Reporter Application started │ pid=20144 │ utc=2026-06-30 09:00 UTC
[2026-06-30 09:00:00.045 INF] [Power Position Reporter] Started │ interval=00:02:00 minutes.
[2026-06-30 09:00:00.051 INF] [Power Position Reporter] Trade(s) Extraction started.
[2026-06-30 09:00:00.312 INF] [Power Trade Positions] Trade(s) Extraction completed
[2026-06-30 09:00:00.330 INF] [CSV-WRITER] report generated │ file=PowerPosition_20260701_202606300900.csv │ rows=24 │ size=0.6KB │ dayAhead=2026-07-01
[2026-06-30 09:00:00.331 INF] [Power Position Reporter] Next run scheduled at 2026-06-30 09:02 UTC
[2026-06-30 09:02:00.005 INF] [Power Position Reporter] Scheduled tick
[2026-06-30 09:02:00.018 WRN] [POWER-SVC] Retry attempt=2 │ delay=00:00:15 │ reason=Simulated transient failure from PowerService
[2026-06-30 09:02:00.340 INF] [Power Trade Positions] Trade(s) Extraction completed
[2026-06-30 09:02:00.355 INF] [CSV-WRITER] report generated │ file=PowerPosition_20260701_202606300902.csv │ rows=24 │ size=0.6KB │ dayAhead=2026-07-01
```

### Sample command-line overrides

```bash
# Custom output folder and a 5-minute interval
dotnet run --project Axpo.PowerPositionReporter.Worker -- --csv-report-path "D:\Reports\Axpo" --interval 5

# Short-form flags
dotnet run --project Axpo.PowerPositionReporter.Worker -- -csv ./out -i 10
```

---
👤 Author

Karn — Senior .NET Full-Stack / Cloud Engineer


Designed and built the full solution: Clean Architecture layering (Domain → Application →
Worker), Polly-based resilience for the trading-system integration, atomic CSV report writing,
Serilog structured logging, and the unit + Reqnroll BDD test suites.
💼 LinkedIn · 🐙 GitHub

<div align="center">

Made with ⚡ by Karn for day-ahead power position reporting.

</div>
