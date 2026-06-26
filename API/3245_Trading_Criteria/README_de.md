# Trading Criteria-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die Trading Criteria-Strategie ist ein Multi-Zeitrahmen-Trendfolge-Ansatz, der aus dem ursprünglichen MQL4-Expertenberater "Trading Criteria" konvertiert wurde. Der Port basiert auf linear gewichteten gleitenden Durchschnitten, Momentum-Abweichungsfiltern und MACD-Bestätigungen aus Trend- und Monats-Zeitrahmen. Risikomanagement-Funktionen umfassen Trailing Stops, Break-Even-Schutz und konfigurierbare Stop-Loss/Take-Profit-Ziele.

## Einstiegslogik

1. **Primärer Zeitrahmen**: Verwendet einen schnellen und langsamen linear gewichteten gleitenden Durchschnitt (LWMA). Long-Signale erfordern, dass der schnelle MA über dem langsamen bleibt; Short-Signale erfordern das Gegenteil.
2. **Momentum-Filter**: Berechnet die Momentum-Abweichung (|Momentum-100|) auf dem Trend-Zeitrahmen und prüft die drei neuesten Werte gegen bullische oder bärische Schwellenwerte.
3. **Trend-MACD-Filter**: Bewertet die MACD-Hauptlinie relativ zur Signallinie auf demselben Trend-Zeitrahmen. Signale werden nur ausgelöst wenn die aktuelle Beziehung mit der vorherigen Bar übereinstimmt, um schnelle Wechsel zu vermeiden.
4. **Monatlicher MACD-Filter**: Bestätigt die größere Richtungsneigung mit MACD auf einem monatlichen (oder benutzerspezifizierten langsamen) Zeitrahmen.
5. **Positions-Exposure**: Begrenzt die maximale Netto-Positionsgröße auf `MaxPositions * Volume`. Wenn ein neues Signal erscheint während eine entgegengesetzte Position gehalten wird, wird die Strategie zuerst die Exposure durch Kauf oder Verkauf ausreichenden Volumens neutralisieren.

## Ausstieg und Risikomanagement

- **Stop Loss / Take Profit**: Via `StopLossPoints` und `TakeProfitPoints` definiert, in tatsächliche Preis-Offsets unter Verwendung der normalisierten Pip-Größe des Instruments umgerechnet.
- **Trailing Stop**: Mit `EnableTrailing` und `TrailingStopPoints` aktiviert. Für Longs verfolgt der Stop den höchsten Preis minus der Trailing-Distanz sobald die Bewegung den Schwellenwert überschreitet; Shorts spiegeln die Logik mit dem niedrigsten Preis.
- **Break-Even-Bewegung**: Wenn aktiviert (`EnableBreakEven`), migriert der Stop zum Einstiegspreis plus einem optionalen Offset, sobald der Schlusskurs die Distanz `BreakEvenTriggerPoints` zugunsten der offenen Position erreicht.
- **Manuelle Schutzausstiege**: Wenn die Kerze die berechneten Stop- oder Zielniveaus berührt, schließt die Strategie die gesamte Nettoposition auf dieser Bar.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `CandleType` | Basis-Zeitrahmen für Signalgenerierung und gleitende Durchschnitte. |
| `TrendCandleType` | Zeitrahmen für Momentum- und MACD-Filter. |
| `MonthlyCandleType` | Langsamer Zeitrahmen für langfristige MACD-Bestätigung. |
| `FastMaPeriod` / `SlowMaPeriod` | Längen des schnellen und langsamen LWMA auf dem Einstiegs-Zeitrahmen. |
| `MomentumPeriod` | Momentum-Lookback-Periode auf dem Trend-Zeitrahmen. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Mindestabweichung von 100 für Long- oder Short-Einstiege. |
| `MaxPositions` | Maximale Anzahl der Basis-Lots die gleichzeitig offen sein können. |
| `StopLossPoints` / `TakeProfitPoints` | Abstände in Punkten für Schutz-Stops und Gewinnziele. |
| `EnableTrailing` / `TrailingStopPoints` | Aktiviert Trailing Stops und setzt ihre Distanz. |
| `EnableBreakEven` | Aktiviert Break-Even-Verhalten. |
| `BreakEvenTriggerPoints` / `BreakEvenOffsetPoints` | Steuert wie weit der Preis sich bewegen muss bevor der Stop auf Break-Even wechselt und welchen Offset anwenden. |

## Verwendungshinweise

- Die Strategie an ein Instrument mit geeigneter Kerzen-Serien-Unterstützung für die gewählten Zeitrahmen anhängen.
- Sicherstellen, dass das Wertpapier einen genauen `PriceStep` bereitstellt; die Implementierung passt Fraktional-Pip-Instrumente (3 oder 5 Dezimalstellen) an MQL-Konventionen an.
- Trailing- und Break-Even-Schutz operieren auf abgeschlossenen Kerzen. In schnellen Märkten können Schutzniveaus auf der folgenden Bar ausgeführt werden wenn eine Lücke auftritt.
- Der Standard-Parametersatz spiegelt die veröffentlichten MQL-Inputs wider, kann aber über die integrierten Parameter-Metadaten optimiert werden.
