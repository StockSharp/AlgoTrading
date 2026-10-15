# MoStAsHaR15 Pivot-Line-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert den Experten „MoStAsHaR15 FoReX – Pivot Line“ MetaTrader 4 unter Verwendung der High-Level-Strategie API von StockSharp. Es behält die ursprüngliche tägliche Floor-Pivot-Karte in Kombination mit Momentumfiltern von ADX, EMA Spreads und dem MACD Histogramm (OsMA) bei. Die Intraday-Logik arbeitet mit einem stündlichen Kerzenstrom, während ein zweites Abonnement die zuvor abgeschlossene Tageskerze verbraucht, um die Pivot-Leiter vor jeder Entscheidung neu aufzubauen.

## Handelslogik
- **Pivot-Berechnung** – Die gestrigen Höchst-, Tiefst- und Schlusskurse aus der Tagesserie generieren den klassischen Pivot (P), drei Widerstandsniveaus (R1–R3), drei Unterstützungsniveaus (S1–S3) und sechs Mittelpunkte (M0–M5). Der aktuelle Kerzenschluss wird anhand dieser Leiter überprüft, um den umgebenden Bereich zu ermitteln. Die ungewöhnliche Zuordnung des ursprünglichen EA, die die Region zwischen M5 und R3 zurück mit dem S3/M0-Segment verbindet, bleibt erhalten.
- **Distanzfilter** – Trades werden nur ausgelöst, wenn die Distanz zur Take-Profit-Grenze, die den aktuellen Bereich begrenzt, größer als `MinimumDistancePips` ist (standardmäßig 14 Pips), was den ursprünglichen `dif1`/`dif2`-Prüfungen entspricht.
- **Lange Einträge** erfordern alle der folgenden Filter:
  - Die Hauptlinie von ADX liegt über `AdxThreshold` (20) und die +DI-Komponente ist sowohl steigend als auch stärker als −DI.
  - Der Schlusskurs EMA liegt mindestens `EmaSpreadPips` (5 Pips) über dem Eröffnungskurs EMA, und die vorherige Kerze hatte bereits die gleiche bullische Reihenfolge.
  - Das MACD-Histogramm ist im Vergleich zur vorherigen Kerze gestiegen (OsMA steigt).
- **Short-Einträge** spiegeln den Long-Zweig mit −DI-Stärke, rückläufigem EMA-Spread und einem fallenden MACD-Histogramm wider.
- Es ist immer nur eine Nettoposition zulässig. Aufträge werden mit Marktausführung über `BuyMarket()` und `SellMarket()` gesendet.

## Positionsmanagement
- **Stop-Loss** – optional, liegt `StopLossPips` unter/über dem Einstiegspreis. Auf `0` setzen, um wie im Original EA zu deaktivieren.
- **Take-Profit** – festgelegt an der Pivot-Grenze (Unterstützung oder Widerstand), die die aktuelle Preisspanne abgrenzt, wenn der Handel eröffnet wird.
- **Trailing Stop** – sobald der Preis mehr als `TrailingStopPips + TrailingStepPips` über den Einstieg hinaus steigt, wird der Stop nachgezogen, um einen Abstand von `TrailingStopPips` beizubehalten. Der Schrittwert muss positiv bleiben, wenn Trailing aktiviert ist.
- Wenn der Stop-Loss, Trailing Stop oder Take-Profit innerhalb einer Kerze berührt wird, wird die Position bei der Bewertung dieses Balkens geschlossen.

## Strategieparameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `HourlyCandleType` | Intraday-Kerzenserien, die die Ausführungslogik versorgen. | 1 Stunde |
| `DailyCandleType` | Täglicher Kerzenstrom, der zur Berechnung der Pivot-Levels verwendet wird. | 1 Tag |
| `StopLossPips` | Anfängliche Stop-Loss-Distanz in Pips. `0` deaktiviert es. | 20 |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. | 10 |
| `TrailingStepPips` | Mindestbewegung (in Pips), bevor der Trailing Stop aktualisiert wird. Muss > 0 sein, wenn Trailing aktiviert ist. | 5 |
| `MinimumDistancePips` | Mindestabstand des Pip zur nahegelegenen Pivot-Grenze vor dem Eingehen eines Handels. | 14 |
| `EmaSpreadPips` | Erforderlicher Spread zwischen dem Schlusskurs EMA und dem Eröffnungskurs EMA. | 5 |
| `AdxThreshold` | Mindestwert ADX, der das Signal aktiviert. | 20 |
| `AdxPeriod` | ADX Indikatorzeitraum. | 14 |
| `EmaClosePeriod` | EMA Länge, die auf Kerzenschlüsse angewendet wird. | 5 |
| `EmaOpenPeriod` | EMA Länge wird auf Kerzeneröffnungen angewendet. | 8 |
| `MacdFastPeriod` | Schnelle EMA-Periode für MACD (OsMA-Zähler). | 12 |
| `MacdSlowPeriod` | Langsamer Zeitraum von EMA für MACD. | 26 |
| `MacdSignalPeriod` | Signalisieren Sie einen Zeitraum von EMA für MACD. | 9 |

## Konvertierungshinweise
- Indikatorwerte werden nur für fertige Kerzen ausgewertet und es werden keine fortlaufenden Sammlungen gespeichert – der Status wird über Skalarfelder gemäß den Repository-Richtlinien verwaltet.
- Pips werden aus der `PriceStep`- und Dezimalgenauigkeit des Wertpapiers abgeleitet. Symbole, die mit 3 oder 5 Dezimalstellen zitiert werden, verwenden die „Mini-Pip“-Konvention, genau wie MetaTrader.
- Die Take-Profit-Zuordnung für die M5→R3-Region greift bewusst auf das S3/M0-Paar zurück, um dem Quellcode treu zu bleiben.
- Alle Kommentare innerhalb der Strategie bleiben gemäß den Projektanweisungen auf Englisch.

## Nutzungstipps
- Passen Sie die Kerzentypen an die Handelssitzung Ihres Instruments an, insbesondere für Märkte mit nicht standardmäßigen täglichen Rollovern.
- Da die Logik Stops und Ziele bei geschlossenen Kerzen auswertet, kann es in schnellen Märkten zu einem zusätzlichen Slippage im Vergleich zur Ausführung auf Tick-Level MetaTrader kommen.
- Erwägen Sie die Optimierung von `MinimumDistancePips` und `EmaSpreadPips`, wenn Sie die Strategie auf Vermögenswerte mit unterschiedlichen Volatilitätsregimen anwenden.
