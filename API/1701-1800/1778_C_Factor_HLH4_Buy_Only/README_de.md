# C-Factor HLH4 Nur-Kauf-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Übersetzung des ursprünglichen MQL-Experten **C_Factor_HLH4_buy_only**. Sie demonstriert, wie MetaTrader-Strategien auf die StockSharp High-Level-API portiert werden.

## Strategielogik

- Verwendet Vierstaunden-Zeitrahmen-Kerzen.
- Eröffnet eine Long-Position, wenn die aktuelle Kerze oberhalb des Hochs der vorherigen Kerze schließt.
- Verlässt die Long-Position, wenn der Schlusskurs:
  - das Tief der vorherigen Kerze um 100 Ticks überschreitet, oder
  - unter das Hoch der vorherigen Kerze um 20 Ticks fällt.
- Das Risikomanagement wird mit konfigurierbaren Stop-Loss- und Take-Profit-Abständen gehandhabt.
- Das Ordervolumen wird aus dem prozentualen Anteil des Kontokapitals berechnet, der pro Trade riskiert wird.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `StopLoss` | Abstand in Ticks für den Schutz-Stop. |
| `TakeProfit` | Abstand in Ticks für das Gewinnziel. |
| `RiskPercent` | Prozent des Kontokapitals, das pro Trade riskiert wird. |
| `CandleType` | Kerzentyp und Zeitrahmen für die Analyse (Standard: 4-Stunden-Kerzen). |

## Hinweise

Die Strategie ist auf Long-Positionen beschränkt und für Bildungszwecke konzipiert. Passen Sie Parameter und Risikoeinstellungen an, bevor Sie sie im Live-Handel einsetzen.
