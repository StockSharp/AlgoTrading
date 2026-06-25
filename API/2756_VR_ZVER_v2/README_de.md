# VR-ZVER v2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die VR-ZVER v2 Strategie ist ein StockSharp-Port des klassischen MetaTrader Expert Advisors. Sie behält die Dreifach-Bestätigungsidee des ursprünglichen Skripts bei: Jeder Trade muss von gleitenden Durchschnitten, dem Stochastik-Oszillator und RSI unterstützt werden. Nur wenn alle aktivierten Filter übereinstimmen, platziert die Strategie eine Marktorder.

## Handelslogik

- Signale werden ausgewertet, wenn eine Kerze schließt. Intrabar-Schwankungen werden nur zur Validierung von Stops oder Zielen verwendet.
- Drei exponentielle gleitende Durchschnitte (schnell, langsam, sehr langsam) müssen in der gleichen Reihenfolge gestapelt sein, um den Trend zu validieren, wenn der MA-Filter aktiviert ist.
- Der Stochastik-Filter wartet auf einen %K/%D-Crossover nahe konfigurierbarer oberer und unterer Bänder.
- Der RSI-Filter erfordert, dass der Oszillator eine neutrale Zone verlässt (unter dem unteren Band für Longs, über dem oberen Band für Shorts).
- Ein Signal wird nur akzeptiert, wenn jeder aktivierte Filter in dieselbe Richtung abstimmt. Wenn ein Filter nicht zustimmt, wird nicht gehandelt.
- Die Strategie öffnet jeweils eine Position. Sie hedgt nicht und baut keine Grids; wenn flach, wartet sie auf das nächste ausgerichtete Signal.

## Positions-Management

- Ein Take-Profit und Stop-Loss werden in Pips ausgedrückt. Der anfängliche Stop wird auf zwei Drittel der konfigurierten Distanz gesetzt, was das ursprüngliche EA-Verhalten reproduziert.
- Ein Breakeven-Trigger (ebenfalls in Pips) verschiebt den Stop auf den Einstiegspreis, sobald der Trade die angegebene Distanz gewonnen hat.
- Trailing Stops verwenden eine Distanz und einen zusätzlichen Schritt. Der Schritt verhindert, dass der Stop bei jedem kleinen Aufwärtsbewegung aktualisiert wird, und entspricht der MT5-Trailing-Logik.
- Long- und Short-Trades teilen dieselben Verwaltungsregeln und reagieren symmetrisch auf Hochs/Tiefs der Kerze.

## Positions-Sizing

- `FixedVolume` größer als null öffnet jede Order mit einer festen Größe.
- Wenn `FixedVolume` auf null gesetzt ist, berechnet die Strategie das Volumen aus `RiskPercent`, dem aktuellen Portfoliowert und der Stop-Distanz. Preisschritt und Schrittpreis werden verwendet, um die Pip-Distanz in monetäres Risiko umzurechnen.
- Volumen werden gerundet, um die `VolumeMin`-, `VolumeMax`- und `VolumeStep`-Beschränkungen des Instruments zu respektieren. Orders werden übersprungen, wenn die berechnete Größe zu klein ist.

## Parameter

| Name | Beschreibung |
| ---- | ------------ |
| `CandleType` | Zeitrahmen für die Signalerzeugung (Standard 15-Minuten-Kerzen). |
| `FixedVolume`, `RiskPercent` | Wahl zwischen festem oder risikobasiertem Sizing. |
| `StopLossPips`, `TakeProfitPips` | Basis-Schutz-Distanzen in Pips. |
| `TrailingStopPips`, `TrailingStepPips`, `BreakevenPips` | Trade-Management-Schwellenwerte. |
| `AllowLongs`, `AllowShorts` | Einzelne Richtungen aktivieren oder deaktivieren. |
| `UseMovingAverageFilter`, `FastMaPeriod`, `SlowMaPeriod`, `VerySlowMaPeriod` | Dreifacher EMA-Trendfilter. |
| `UseStochastic`, `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSmooth`, `StochasticUpperLevel`, `StochasticLowerLevel` | Stochastik-Bestätigungseinstellungen. |
| `UseRsi`, `RsiPeriod`, `RsiUpperLevel`, `RsiLowerLevel` | RSI-Bestätigungsband. |

## Hinweise

- Die Pip-Konvertierung emuliert den ursprünglichen EA: Fünf- und dreistellige Symbole multiplizieren den Preisschritt mit zehn vor der Berechnung der Pip-Werte.
- Der StockSharp-Port verwendet nur Marktorders. Die Sperr- und ausstehenden Order-Funktionen der MetaTrader-Version werden absichtlich weggelassen, um die Implementierung konsistent mit der High-Level-API zu halten.
- Hängen Sie die Strategie an ein Chart an, wenn Sie die EMA-, Stochastik- und RSI-Overlays sehen möchten; sie werden automatisch gezeichnet, wenn ein Chart-Bereich verfügbar ist.
