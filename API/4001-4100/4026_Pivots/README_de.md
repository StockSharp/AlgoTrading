# 4026 – Pivots-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie portiert die MetaTrader 4 Dateien, die sich in `MQL/8550` befinden (den **Pivots**-Indikator und den begleitenden `Pivots_test`-Expertenberater), in die übergeordnete `Strategy` API von StockSharp. Es behält das ursprüngliche Verhalten der Berechnung täglicher Floor-Pivot-Levels bei, stellt ein Paar gegensätzlicher ausstehender Orders am zentralen Pivot bereit und verwaltet jede resultierende Position mit einem festen Stop-Loss, Take-Profit und Trailing-Stop.

## Pivot-Berechnung

1. Die Strategie abonniert einen konfigurierbaren *Pivot-Zeitrahmen* (`PivotCandleType`, standardmäßig täglich).
2. Immer wenn eine Kerze dieses Zeitrahmens endet, werden klassische Floor-Pivot-Levels aus den OHLC-Preisen des Vortages abgeleitet:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 × Pivot − Low`
   - `S1 = 2 × Pivot − High`
   - `R2 = Pivot + (High − Low)` und `S2 = Pivot − (High − Low)`
   - `R3 = 2 × Pivot + High − 2 × Low` und `S3 = 2 × Pivot − (2 × High − Low)`
3. Die Level werden zu Beginn der nächsten Sitzung aktiv. In diesem Fall protokolliert die Strategie die Werte über `AddInfoLog` (zum Beispiel: `Pivot levels for 2024-04-05: P=1.0924, R1=1.0956, …`).

## Workflow für ausstehende Orders

Sobald Pivot-Levels aktiv sind, stellt die Strategie kontinuierlich sicher, dass zwei ausstehende Orders zum Pivot-Preis vorhanden sind:

- **Kauflimit** bei `Pivot` mit Post-Fill-Schutz `SellStop` (Stop-Loss) bei `S2` und `SellLimit` (Take-Profit) bei `R2`.
- **Verkaufsstopp** bei `Pivot` mit Post-Fill-Schutz `BuyStop` bei `R2` und `BuyLimit` bei `S2`.

Alle Bestellungen werden über die High-Level-Hilfsmethoden `BuyLimit`, `SellStop`, `SellLimit` und `BuyStop` übermittelt. Wenn eine Order ausgeführt wird, berechnet der Code den durchschnittlichen Einstiegspreis für diese Richtung neu, storniert bestehende Schutzorders und sendet ein neues Stop/Limit-Paar, das das gesamte offene Volumen abdeckt (was das MetaTrader-Verhalten widerspiegelt, bei dem jede Position denselben S2/R2-Schutz erbt). Wenn der schützende Stop oder Take-Profit ausgeführt wird, werden die entsprechenden Helfer automatisch gelöscht.

Die Strategie verwendet eine einzige Nettoposition, sodass sich entgegengesetzte Füllungen gegenseitig ausgleichen (im Gegensatz zur Ticket-basierten Absicherung von MetaTrader). Dies ist die einzige bewusste Abweichung vom Originalgutachten.

## Trailing-Stop-Logik

- `TrailingStopPoints` definiert die Entfernung in Indikatorpunkten (multipliziert mit dem Instrument `PriceStep`).
- Bei Long-Positionen wird der Trailing Stop aktiviert, sobald sich der Preis um mehr als diese Distanz über den durchschnittlichen Einstiegspunkt bewegt hat. Der schützende `SellStop` wird dann näher an den Markt herangeführt.
- Für Short-Positionen gilt die Spiegellogik, die den `BuyStop` senkt, wenn sich der Preis günstig entwickelt.
- Nachfolgende Aktualisierungen werden durch die über `CandleType` ausgewählte Intraday-Serie gesteuert (standardmäßig 15-Minuten-Kerzen).

## Parameter

| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `OrderVolume` | Volumen jeder ausstehenden Bestellung (Lots/Kontrakte). | `0.1` |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Punkten. `0` deaktiviert die nachgestellte Logik. | `30` |
| `CandleType` | Intraday-Kerzenserien, die zum Nachlaufen und zum Einhalten des Sitzungsplans verwendet werden. | `15m` Zeitrahmen |
| `PivotCandleType` | Zeitrahmen, der zur Berechnung der täglichen Pivot-Levels verwendet wird. | `1D` Zeitrahmen |
| `LogPivotUpdates` | Bei `true` werden die Pivot-Ebenen bei jeder Änderung in das Strategieprotokoll geschrieben. | `true` |

Alle numerischen Parameter werden über `StrategyParam<T>` verfügbar gemacht, sodass sie innerhalb der StockSharp-Infrastruktur optimiert werden können.

## Protokollierung und Diagnose

- Pivot-Aktualisierungen werden über `AddInfoLog` weitergeleitet, was die Ausgabe von MetaTrader `Comment`/`ObjectSetText` ersetzt.
- Schutzauftragsverwaltung, Positionsverwaltung und Trailing-Logik basieren ausschließlich auf den High-Level-Helfern von StockSharp. Es werden keine Low-Level-Auftragsregistrierung oder Indikatorpuffer verwendet.

## Nutzungshinweise

1. Hängen Sie die Strategie an einen Connector an, der sowohl Tages- als auch Intraday-Kerzen für das ausgewählte Wertpapier bereitstellt.
2. Passen Sie bei Bedarf den Schritt des Instruments an (`PriceStep` wird automatisch erkannt; der Fallback ist `0.0001`).
3. Passen Sie optional `OrderVolume`, `TrailingStopPoints` oder die Kerzentypen an, um sie an das ursprüngliche MT4-Setup anzupassen.

Wie gewünscht wird für diesen Port keine Python-Version bereitgestellt.
