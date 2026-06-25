# Auto ADX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Auto ADX-Strategie** ist eine direkte Portierung des MetaTrader-Expertenberaters `Auto ADX.mq5` in die High-Level-API von StockSharp. Die Strategie bewertet die ADX-Stärke (Average Directional Index) und die Beziehung zwischen den +DI- und -DI-Komponenten, um die Handelsrichtung zu bestimmen. Sie reproduziert die ursprünglichen Risikokontrollen, einschließlich Stop-Loss, Take-Profit, umkehrbarer Signale und pip-basierter Trailing Stops, während sie StockSharp-Konzepte wie Kerzenabonnements und Indikatorbindungen übernimmt.

## Handelslogik
- **Kerzenquelle** – Die Strategie abonniert einen konfigurierbaren Kerzentyp (Standard: 1-Stunden-Zeitrahmen) und verarbeitet nur abgeschlossene Kerzen, um Intrabar-Rauschen zu vermeiden.
- **ADX-Berechnung** – Ein einzelner `AverageDirectionalIndex`-Indikator wird über `BindEx` gebunden, was Zugang zum geglätteten ADX-Wert sowie zu den +DI- und -DI-Linien gibt.
- **Long-Einstieg** – Ausgelöst wenn:
  - +DI größer als -DI ist (positives Richtungsmomentum),
  - ADX über dem konfigurierbaren ADX-Niveau liegt, und
  - ADX im Vergleich zur vorherigen Kerze steigt.
- **Short-Einstieg** – Ausgelöst wenn:
  - -DI größer als +DI ist (negatives Richtungsmomentum),
  - ADX unter dem konfigurierten Niveau liegt, und
  - ADX gegenüber der vorherigen Kerze fällt.
- **Umkehrmodus** – Wenn `ReverseSignals` aktiviert ist (Standardverhalten), werden offene Positionen geschlossen, wenn:
  - Eine Long-Position sieht, dass +DI unter -DI fällt **oder** ADX sinkt,
  - Eine Short-Position sieht, dass +DI über -DI steigt **oder** ADX steigt.
- **Positionsgröße** – Aufträge werden mit dem `Volume` der Strategie ausgegeben. Die Umkehrbehandlung basiert auf `ClosePosition()`, um das gesamte Engagement zu verlassen, bevor ein neues Signal berücksichtigt wird.

## Risikomanagement
- **Stop-Loss / Take-Profit** – Aus Pip-Eingaben in absolute Preisabstände umgerechnet unter Verwendung des `PriceStep` des Instruments. StockSharp's `StartProtection`-Helfer platziert die Schutzaufträge mit optionaler Marktausführung.
- **Trailing Stop** – Die ursprüngliche pip-basierte Trailing-Logik wird repliziert:
  - Trailing aktiviert erst, nachdem der unrealisierte Gewinn die Trailing-Distanz überschreitet.
  - Das Stop-Niveau bewegt sich in pip-großen Schritten (`TrailingStepPips`).
  - Eine Long-Position verlässt, wenn der Preis unter den Trailing Stop druckt; eine Short verlässt, wenn der Preis über den Trailing Stop steigt.
- **Pip-Konvertierung** – Um die MQL-Implementierung nachzuahmen, entspricht die Pip-Größe dem `PriceStep`, multipliziert mit 10, wenn das Wertpapier 3- oder 5-Dezimalpreise verwendet. Dies hält das Verhalten über Forex-Symbole konsistent.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `StopLossPips` | 50 | Distanz des Schutzstopp in Pips. Auf null setzen, um den Stop-Loss zu deaktivieren. |
| `TakeProfitPips` | 50 | Distanz des Gewinnziels in Pips. Auf null setzen, um den Take-Profit zu deaktivieren. |
| `TrailingStopPips` | 5 | Größe des Trailing Stops in Pips. Auf null setzen, um Trailing zu deaktivieren. |
| `TrailingStepPips` | 5 | Minimaler inkrementeller Gewinn (in Pips) bevor der Trailing Stop verschoben wird. Muss positiv sein, wenn Trailing aktiviert ist. |
| `AdxPeriod` | 14 | Durchschnittsperiode für den ADX-Indikator. |
| `AdxLevel` | 30 | ADX-Stärkeschwellenwert, der Einstiege filtert. |
| `ReverseSignals` | true | Aktiviert das Schließen bestehender Positionen, wenn die DI-Beziehung oder ADX-Steigung wechselt. |
| `CandleType` | 1 Stunde | Kerzentyp für Analyse und Handel. |

## Implementierungshinweise
- `BindEx` wird verwendet, um auf den vollständigen `AverageDirectionalIndexValue` zuzugreifen, wodurch sichergestellt wird, dass wir uns nie auf manuelles Indikatorwert-Abrufen verlassen.
- Die Trailing-Logik verfolgt das letzte Stop-Niveau und bewegt es nur, wenn der Preis um mindestens `TrailingStepPips` zugunsten der Position voranschreitet, was das MQL-Trailing-Schrittverhalten repliziert.
- Alle Inline-Kommentare im C#-Quellcode sind auf Englisch, um die Repository-Richtlinien zu erfüllen.
- Die Strategie ist in sich geschlossen innerhalb von `API/2908_Auto_ADX/CS/AutoAdxStrategy.cs`; es gibt keine Python-Entsprechung gemäß den Anforderungen.

## Verwendungstipps
1. Die Strategie an ein Wertpapier mit korrekten `PriceStep`-Metadaten anhängen, damit die Pip-Konvertierung genau bleibt.
2. `AdxLevel` anpassen, um zum Volatilitätsprofil des gehandelten Instruments zu passen — höhere Schwellenwerte reduzieren die Signalfrequenz.
3. Wenn Trailing deaktiviert ist (`TrailingStopPips = 0`), wird `TrailingStepPips` ignoriert, was das ursprüngliche Expertenberater-Verhalten reproduziert.
4. Über mehrere Märkte hinweg Backtests durchführen, um pip-basierte Schutzabstände zu validieren und zu bestätigen, dass die ADX-Steigungsfilterung den Erwartungen entspricht.
