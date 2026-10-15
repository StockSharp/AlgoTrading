# MostasHaR15 Pivot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Strategie repliziert das Verhalten des ursprünglichen **MostasHaR15 Pivot** MQL5-Expertenberaters mithilfe der StockSharp High-Level-API. Sie kombiniert klassische tägliche Floor-Pivot-Berechnungen mit Momentum-Filtern aus ADX, EMA-Differenzialen und dem MACD-Histogramm (OsMA). Die Strategie operiert auf einem Intraday-Kerzenstrom (standardmäßig 1 Stunde) und verbraucht die vorherige abgeschlossene Tageskerze, um die Pivot-Karte bei jeder Bar neu zu erstellen.

## Handelslogik
- **Pivot-Gitter** – das vorherige tägliche Hoch, Tief und Schlusskurs werden verwendet, um den Haupt-Pivot (P), drei Widerstandsniveaus (R1–R3), drei Unterstützungsniveaus (S1–S3) und sechs Mittelpunkte (M0–M5) zu berechnen. Der aktuelle Kerzenschlusskurs wird mit dieser Leiter verglichen, um das umliegende Unterstützungs- und Widerstandssegment zu identifizieren. Ein von der EA geerbter Sonderfall ordnet Preise zwischen M5 und R3 zurück in den S3/M0-Bereich.
- **Distanzfilter** – Trades werden nur erlaubt, wenn der Abstand zum nächsten Take-Profit-Level größer als `MinimumDistancePips` (standardmäßig 14 Pips) ist, was den ursprünglichen `dif1`/`dif2`-Filtern entspricht.
- **Long-Einstiege** erfordern alles Folgende:
  - ADX-Hauptlinie übersteigt `AdxThreshold` (20) und +DI ist sowohl steigend als auch über –DI.
  - Der 5-Perioden-EMA auf Kerzenschlusskursen liegt mindestens `EmaSlopePips` (5 Pips) über dem 8-Perioden-EMA auf Kerzeneröffnungspreisen, und die vorherige Bar zeigte dieselbe bullische EMA-Anordnung.
  - MACD-Histogramm (OsMA) stieg im Vergleich zur vorherigen Bar.
- **Short-Einstiege** spiegeln die Long-Bedingungen mit –DI-Stärke, bärischer EMA-Spreizung und fallendem MACD-Histogramm wider.
- Nur eine Nettoposition ist erlaubt. Orders werden mit Marktausführung über `BuyMarket()`/`SellMarket()` platziert.

## Positionsmanagement
- **Stop-Loss** – optional, `StopLossPips` unter/über dem Einstiegspreis. Das Setzen des Parameters auf `0` deaktiviert den initialen Stop, wie in der EA.
- **Take-Profit** – fest am nächsten Pivot-Bereichsrand, der den aktuellen Preis bei der Positionseröffnung umgibt.
- **Trailing Stop** – repliziert die ursprüngliche Trailing-Logik. Sobald der Preis mehr als `TrailingStopPips + TrailingStepPips` vom Einstieg voranschreitet, wird der Stop bewegt, um einen Trailing-Abstand von `TrailingStopPips` zu halten. Das Trailing kann durch Setzen von `TrailingStopPips` auf `0` deaktiviert werden.
- Wenn Stop-Loss, Trailing Stop oder Take-Profit während einer Kerze erreicht wird, wird die Position beim Schlusskurs dieser Kerze flachgestellt.

## Strategieparameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Intraday-Kerzenreihe für den Handel. | 1-Stunden-Zeitrahmen |
| `DailyCandleType` | Tägliche Kerzenreihe für Pivot-Berechnungen. | 1-Tages-Zeitrahmen |
| `StopLossPips` | Stop-Loss-Abstand in Pips. `0` zum Deaktivieren setzen. | 20 |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. | 5 |
| `TrailingStepPips` | Minimale günstige Bewegung, bevor der Trail aktualisiert wird. Muss >0 sein, wenn Trailing aktiviert ist. | 5 |
| `MinimumDistancePips` | Minimaler Pip-Abstand zum nächsten Pivot-Rand vor dem Einstieg in einen Trade. | 14 |
| `EmaSlopePips` | Erforderliche Trennung zwischen dem Schluss-EMA und dem Eröffnungs-EMA. | 5 |
| `AdxThreshold` | Minimale ADX-Messung für Long- und Short-Trades. | 20 |
| `AdxPeriod` | ADX-Indikatorlänge. | 14 |
| `EmaClosePeriod` | EMA-Periode angewendet auf Kerzenschlusskurse. | 5 |
| `EmaOpenPeriod` | EMA-Periode angewendet auf Kerzeneröffnungspreise. | 8 |
| `MacdFastPeriod` | Schnelle EMA-Periode innerhalb des MACD-Histogramms. | 12 |
| `MacdSlowPeriod` | Langsame EMA-Periode innerhalb des MACD-Histogramms. | 26 |
| `MacdSignalPeriod` | Signal-EMA-Periode innerhalb des MACD-Histogramms. | 9 |

## Konvertierungshinweise
- Die Strategie behält das ungewöhnliche Verhalten der EA bei, wo der Preisbereich zwischen dem Mittelniveau M5 und dem Widerstand R3 zurück zum Unterstützungs-/Widerstandspaar S3/M0 abbildet.
- Alle Indikatorwerte werden nur auf abgeschlossenen Kerzen verarbeitet. Es werden keine historischen Sammlungen gespeichert; der gesamte Zustand wird gemäß den Repository-Richtlinien in skalaren Feldern gehalten.
- Kommentare in der Strategie bleiben per Repository-Anweisungen auf Englisch.

## Verwendungshinweise
- Passen Sie `CandleType` und `DailyCandleType` an, wenn Sie die Strategie auf Märkte mit unterschiedlichen Handelssitzungen anwenden.
- Da Stop-Loss- und Trailing-Logik auf geschlossenen Kerzen ausgewertet werden, kann in schnellen Märkten im Vergleich zur Tick-Level-Ausführung in der ursprünglichen EA zusätzlicher Slippage auftreten.
