# Simple EA MA plus MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie portiert den MetaTrader 5 Expertenberater **Simple EA MA plus MACD** auf die StockSharp High-Level-API. Sie sucht nach einem Ausbruch aus einem "Signal-Balken", der zwei Bedingungen erfüllt: Ein verschobener gleitender Durchschnitt liegt unter/über den Hochs des Balkens, und das MACD-Histogramm hat gerade die Nulllinie gekreuzt. Wenn die nächste Kerze über das Extremum des Signal-Balkens hinaus schließt, tritt die Strategie in die Ausbruchsrichtung ein.

Die Implementierung behält das ursprüngliche EA-Verhalten bei:

1. **Signalerkennung** – bei jeder abgeschlossenen Kerze prüft die Strategie den vorherigen Balken. Ein konfigurierbarer gleitender Durchschnitt (Standard LWMA), berechnet auf dem gewählten angewendeten Preis, muss für Longs kleiner als sowohl das vorherige als auch das aktuelle Kerzenhoch sein (größer für Shorts). Gleichzeitig muss die MACD-Hauptlinie zwischen den beiden vorherigen Balken null gekreuzt haben.
2. **Signalbestätigung** – sobald ein Signal-Balken gespeichert ist, wartet die Strategie auf die nächste abgeschlossene Kerze. Ein Schluss über dem gespeicherten Hoch löst einen Long-Ausbruch aus; ein Schluss unter dem gespeicherten Tief löst einen Short-Ausbruch aus. Wenn der Preis das Signal durch Schließen innerhalb des Signal-Balkens ungültig macht, wird das Setup abgebrochen.
3. **Positionsmanagement** – neu eröffnete Trades erben Stop-Loss-, Take-Profit- und Trailing-Stop-Abstände in Pips. Schutzlevels werden unter Verwendung des `PriceStep` des Instruments in absolute Preise konvertiert. Instrumente mit drei oder fünf Dezimalstellen erhalten die klassische Forex-Anpassung (step × 10), um MetaTrader-Pip-Definitionen nachzuahmen.

## Risikomanagement
- **Stop-Loss / Take-Profit** – optionale Pip-Abstände werden bei jedem Kerzenschluss ausgewertet. Wenn der Markt über den entsprechenden Level hinausdruckt, steigt die Strategie mit einer Market-Order aus.
- **Trailing-Stop** – wenn der Gewinn `TrailingStopPips + TrailingStepPips` übersteigt, wird eine Trailing-Referenz hinter dem besten erreichten Preis bewegt. Wenn der Preis auf das Trailing-Level zurückfällt, wird die Position geschlossen. Ein Trailing-Schritt von null reaktiviert den Stop bei jedem neuen Extremum.
- **Flat bei Umkehr** – wenn ein entgegengesetzter Ausbruch erscheint, während eine entgegengesetzte Position offen ist, sendet die Strategie eine einzige Market-Order, die groß genug ist, um die bestehende Exposition zu schließen und den neuen Trade in einem Schritt zu eröffnen.

## Implementierungshinweise
- Der gleitende Durchschnitt unterstützt dieselben Glättungsmethoden und angewendeten Preisoptionen wie MetaTrader (Simple, Exponential, Smoothed, LinearWeighted und Close/Open/High/Low/Median/Typical/Weighted-Preise).
- `MaShift` reproduziert den horizontalen Versatz des MetaTrader-Indikators durch Lesen von Werten aus früheren Balken vor der Auswertung der Ausbruchsregeln.
- MACD verwendet den eingebauten `MovingAverageConvergenceDivergence`-Indikator. Nur das Histogramm (Differenz zwischen schnellen und langsamen EMAs) wird benötigt; die Signallinienperiode wird für Parität mit den EA-Einstellungen beibehalten.
- Kerzen-Abonnements und Indikatorverarbeitung basieren ausschließlich auf der StockSharp High-Level-API. Kein manuelles Tick-Handling oder Indikatorpuffer werden verwendet.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `Volume` | `1` | Ordergröße für jeden Ausbruchseinstieg. |
| `TakeProfitPips` | `50` | Gewinnzielabstand in Pips (umgerechnet in absoluten Preis mit dem Instrument-Preisschritt). Auf 0 setzen zum Deaktivieren. |
| `StopLossPips` | `50` | Schutz-Stop-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `TrailingStopPips` | `5` | Trailing-Stop-Abstand in Pips, der fixiert wird, sobald der Preis ausreichend vorrückt. |
| `TrailingStepPips` | `5` | Minimaler zusätzlicher Fortschritt (in Pips) bevor der Trailing-Stop wieder vorrückt. |
| `MaPeriod` | `100` | Länge des gleitenden Durchschnitts zur Validierung des Signal-Balkens. |
| `MaShift` | `0` | Horizontaler Versatz des gleitenden Durchschnitts, emuliert den MetaTrader-Parameter `ma_shift`. |
| `MaMethod` | `LinearWeighted` | Glättungsmethode des gleitenden Durchschnitts (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaAppliedPrice` | `Weighted` | Preisquelle für den gleitenden Durchschnitt (Close, Open, High, Low, Median, Typical, Weighted). |
| `MacdFastPeriod` | `12` | Schnelle EMA-Periode für die MACD-Berechnung. |
| `MacdSlowPeriod` | `26` | Langsame EMA-Periode für die MACD-Berechnung. |
| `MacdSignalPeriod` | `9` | Signallinien-Glättungsperiode für Parität mit dem ursprünglichen EA. |
| `MacdAppliedPrice` | `Weighted` | Angewendeter Preis beim Einspeisung von Werten in MACD. |
| `CandleType` | `1 hour` time frame | Primäre Kerzenserie für Signale und Trade-Management. |

## Verwendungstipps
- Die Pip-basierten Schutzmaßnahmen an die Tick-Größe des ausgewählten Instruments anpassen; falsche `PriceStep`-Werte auf der Connector-Seite verzerren die Pip-Konvertierungen.
- Für hochvolatile Märkte `TrailingStepPips` erhöhen, um vorzeitige Ausstiege zu reduzieren, oder verringern, um das Trailing-Verhalten zu verschärfen.
- Da Trades auf geschlossenen Kerzen ausgeführt werden, muss der Ausbruch bis zum Balkenabschluss andauern; kleinere Zeitrahmen erhöhen die Handelsfrequenz, können aber mehr Rauschen einführen.
