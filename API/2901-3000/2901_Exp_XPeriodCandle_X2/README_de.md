# Exp XPeriodCandle X2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Exp XPeriodCandle X2 recreiert den originalen MetaTrader-Experten mit der High-Level-API von StockSharp. Die Strategie erstellt synthetische Kerzen auf zwei Zeitrahmen, indem sie jeden Balken glättet und die verzögerte Eröffnung eines konfigurierbaren Rückblickfensters mit dem letzten geglätteten Schluss vergleicht. Die Kerzenfarbe des höheren Zeitrahmens definiert die Trendbias, während der Arbeitszeitrahmen auf Farbübergänge wartet, um Einstiege und Ausstiege auszulösen. Optionale Stop-Loss- und Take-Profit-Schutzmaßnahmen replizieren die Money-Management-Eingaben aus dem Quellcode.

## Funktionsweise
- **Trenderkennung** – das Abonnement des höheren Zeitrahmens glättet Eröffnungs- und Schlusspreise mit dem ausgewählten gleitenden Durchschnitt. Jede abgeschlossene Kerze vergleicht ihren geglätteten Schluss mit der verzögerten geglätteten Eröffnung von `TrendPeriod` Balken zuvor. Ein Schluss über der verzögerten Eröffnung erzeugt eine bullische Farbe (0), ein Schluss darunter eine bärische Farbe (2). Die gespeicherte Farbe bei `TrendSignalBar` bestimmt, ob der globale Trend long (`+1`), short (`-1`) oder neutral ist.
- **Einstiegslogik** – der Arbeitszeitrahmen wendet dieselbe Glättung an. Für jede abgeschlossene Kerze speichert die Strategie die aktuellen und vorherigen Farben, die durch `EntrySignalBar` referenziert werden. Ein Short-Setup erscheint, wenn der Trend des höheren Zeitrahmens bärisch ist, die aktuelle Farbe 0 und die vorherige Farbe 2 ist, was den ursprünglichen XPeriodCandle-Signalwechsel widerspiegelt. Ein Long-Setup erfordert, dass der Trend bullisch ist, die aktuelle Farbe 2 und die vorherige Farbe 0 ist.
- **Positionsverwaltung** – konfigurierbare Schalter schließen Positionen bei Trendwenden (`CloseLongOnTrendFlip`, `CloseShortOnTrendFlip`) und bei Einstiegsebenen-Umkehrungen (`CloseLongOnEntrySignal`, `CloseShortOnEntrySignal`). Neue Trades dimensionieren `Volume + |Position|`, sodass ein entgegengesetztes Signal sowohl aussteigt als auch umkehrt wie der MQL-Experte.
- **Risikokontrollen** – optionale Stop-Loss- und Take-Profit-Abstände werden in Preisschritten ausgedrückt (`StopLossTicks`, `TakeProfitTicks`). Sie werden nur aktiviert, wenn der entsprechende Boolean aktiviert ist.
- **Glättungsmethoden** – StockSharp-gleitende Durchschnitte werden anstelle der ursprünglichen SmoothAlgorithms-Bibliothek verwendet. Verfügbare Modi sind Simple, Exponential, Smoothed (SMMA), Weighted, Hull, Kaufman Adaptive und Jurik. Die Parameter `TrendPhase` und `EntryPhase` beeinflussen nur die Jurik-Glättung und sind auf den ±100-Bereich begrenzt.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `TrendCandleType` | Höherer Zeitrahmen-Kerzentyp für den Trendfilter. |
| `EntryCandleType` | Arbeitszeitrahmen-Kerzentyp für Einstiege. |
| `TrendPeriod` | Anzahl geglätteter Kerzen, die die verzögerte Eröffnung auf dem Trend-Zeitrahmen definieren. |
| `EntryPeriod` | Anzahl geglätteter Kerzen, die die verzögerte Eröffnung auf dem Einstiegs-Zeitrahmen definieren. |
| `TrendLength` | Glättungslänge für synthetische Kerzen des höheren Zeitrahmens. |
| `EntryLength` | Glättungslänge für synthetische Kerzen des Arbeitszeitrahmens. |
| `TrendPhase` | Jurik-Phasenparameter für den Trend-Zeitrahmen (von anderen Glättungstypen ignoriert). |
| `EntryPhase` | Jurik-Phasenparameter für den Einstiegs-Zeitrahmen (von anderen Glättungstypen ignoriert). |
| `TrendSignalBar` | Versatz zum Lesen der Trend-Kerzenfarbe (`1` entspricht dem zuletzt geschlossenen Balken). |
| `EntrySignalBar` | Versatz zum Lesen von Einstiegsfarben (`1` referenziert den letzten geschlossenen Balken, `2` den vorherigen). |
| `TrendSmoothing` | Gleitender Durchschnittstyp für die Glättung des höheren Zeitrahmens. |
| `EntrySmoothing` | Gleitender Durchschnittstyp für die Glättung des Arbeitszeitrahmens. |
| `EnableLongEntries` | Long-Positionen erlauben, wenn bullische Bedingungen erscheinen. |
| `EnableShortEntries` | Short-Positionen erlauben, wenn bärische Bedingungen erscheinen. |
| `CloseLongOnTrendFlip` | Long-Positionen schließen, wenn der Trend des höheren Zeitrahmens bärisch wird. |
| `CloseShortOnTrendFlip` | Short-Positionen schließen, wenn der Trend des höheren Zeitrahmens bullisch wird. |
| `CloseLongOnEntrySignal` | Long-Positionen schließen, wenn der Einstiegs-Zeitrahmen eine bärische Farbe druckt. |
| `CloseShortOnEntrySignal` | Short-Positionen schließen, wenn der Einstiegs-Zeitrahmen eine bullische Farbe druckt. |
| `UseStopLoss` | Stop-Loss-Schutz in Preisschritten aktivieren. |
| `StopLossTicks` | Stop-Loss-Distanz in Preisschritten. |
| `UseTakeProfit` | Take-Profit-Schutz in Preisschritten aktivieren. |
| `TakeProfitTicks` | Take-Profit-Distanz in Preisschritten. |

## Hinweise
- Die verzögerte Eröffnungslogik speichert die älteste geglättete Eröffnung innerhalb des konfigurierten Zeitraums, was dem Ringpuffer des ursprünglichen Indikators entspricht.
- Wenn `TrendCandleType` und `EntryCandleType` gleich sind, wird nur ein Kerzenabonnement erstellt, aber die Doppelfarblogik funktioniert weiterhin.
- Sicherstellen, dass `Volume` entsprechend eingestellt ist; Umkehrtrades schließen automatisch die aktuelle absolute Position ein, um das MetaTrader-Lot-Handling-Verhalten zu replizieren.
