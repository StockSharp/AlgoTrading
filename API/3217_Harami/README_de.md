# Harami-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
HaramiStrategy konvertiert den MetaTrader-Experten "Harami" in die High-Level-API von StockSharp. Die Strategie kombiniert ein bullisches/bärisches Harami-Muster, das auf einem höheren Zeitrahmen erkannt wird, mit einer Momentum-Expansion und einem langfristigen MACD-Filter. Nur abgeschlossene Kerzen werden verarbeitet und das gesamte Handelsmanagement wird über die integrierte Schutz-Engine von StockSharp abgewickelt.

## Daten und Indikatoren
- **Basiszeitrahmen:** konfigurierbar (standardmäßig 15-Minuten-Kerzen) zur Trenderfassung mittels gleitender Durchschnitte.
- **Höherer Zeitrahmen:** konfigurierbar (standardmäßig eine Stunde) zur Mustererkennung und Momentum-Bestätigung.
- **MACD-Zeitrahmen:** konfigurierbar (standardmäßig 30-Tage-Kerzen) zur Emulation des ursprünglichen monatlichen MACD-Filters.
- **Indikatoren:**
  - Linear gewichteter gleitender Durchschnitt (`FastMaLength`) auf dem Basiszeitrahmen.
  - Exponentieller gleitender Durchschnitt (`SlowMaLength`) auf dem Basiszeitrahmen.
  - Momentum (`MomentumPeriod`) auf dem höheren Zeitrahmen. Die Strategie verwendet den absoluten Abstand vom Neutralwert (100) für die letzten drei Balken des höheren Zeitrahmens.
  - Moving Average Convergence Divergence (12/26/9) auf dem MACD-Zeitrahmen.

## Long-Setup
1. Der langsame EMA liegt über dem schnellen LWMA auf dem Basiszeitrahmen und signalisiert einen Aufwärtstrend.
2. Der höhere Zeitrahmen bildet eine bullische Harami-Sequenz: Vor zwei Kerzen war die Kerze bärisch, die vorherige Kerze war bullisch und ihr Körper ist kleiner als der frühere bärische Körper.
3. Jede der letzten drei Momentum-Abweichungen des höheren Zeitrahmens überschreitet `MomentumBuyThreshold`.
4. Die MACD-Hauptlinie liegt auf dem MACD-Zeitrahmen über der Signallinie.
5. Es ist keine Long-Position offen (`Position <= 0`).
6. Die Strategie sendet eine Market-Kauforder, die groß genug ist, um Short-Exposure umzukehren und `Volume` Lots hinzuzufügen.

## Short-Setup
1. Der langsame EMA liegt unter dem schnellen LWMA auf dem Basiszeitrahmen.
2. Der höhere Zeitrahmen bildet ein bärisches Harami: Vor zwei Kerzen war bullisch, die vorherige Kerze war bärisch und der letzte Körper ist kleiner.
3. Jede der letzten drei Momentum-Abweichungen des höheren Zeitrahmens überschreitet `MomentumSellThreshold`.
4. Die MACD-Hauptlinie liegt unter der Signallinie.
5. Es ist keine Short-Exposition offen (`Position >= 0`).
6. Die Strategie sendet eine Market-Verkaufsorder, die groß genug ist, um Long-Positionen zu schließen und eine neue Short-Position der Größe `Volume` zu eröffnen.

## Risikomanagement
`StartProtection` installiert Stop-Loss- und Take-Profit-Levels (ausgedrückt in Punkten). Zusätzliche Trailing-, Break-Even- und Geldmanagement-Funktionen des ursprünglichen EA werden absichtlich weggelassen, um die StockSharp-Version kompakt zu halten. Handelsrichtungsänderungen glätten automatisch die entgegengesetzte Exposition.

## Parameter
| Name | Beschreibung | Standardwert |
| ---- | ----------- | ------- |
| `CandleType` | Primärer Zeitrahmen für gleitende Durchschnitte und Signalausführung. | 15-Minuten-Kerzen |
| `HigherCandleType` | Zeitrahmen für Harami- und Momentum-Bestätigung. | 1-Stunden-Kerzen |
| `MacdCandleType` | Zeitrahmen für den MACD-Trendfilter. | 30-Tage-Kerzen |
| `FastMaLength` | Länge des schnellen linear gewichteten MA. | 6 |
| `SlowMaLength` | Länge des langsamen exponentiellen MA. | 85 |
| `MomentumPeriod` | Momentum-Rückblick auf dem höheren Zeitrahmen. | 14 |
| `MomentumBuyThreshold` | Minimale Momentum-Abweichung für Long-Bestätigung. | 0.3 |
| `MomentumSellThreshold` | Minimale Momentum-Abweichung für Short-Bestätigung. | 0.3 |
| `StopLossPoints` | Stop-Loss-Abstand in Punkten. | 40 |
| `TakeProfitPoints` | Take-Profit-Abstand in Punkten. | 100 |

## Verwendungshinweise
- `CandleType`, `HigherCandleType` und `MacdCandleType` mit verfügbaren historischen Daten abstimmen; sicherstellen, dass der höhere Zeitrahmen länger als der Basiszeitrahmen ist.
- Momentum-Schwellenwerte an die Volatilität des gehandelten Instruments anpassen.
- Den StockSharp-Optimierer über die bereitgestellten Parameterbereiche verwenden, um MA-Längen und Momentum-Schwellenwerte zu optimieren.
- Immer mit realistischen Provisions-/Latenzeinstellungen backtesten, bevor live eingesetzt wird.
