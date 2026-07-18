# HarVesteR MACD Trendstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die HarVesteR-Strategie ist ein Trendfolgesystem, das vom ursprünglichen MetaTrader-Berater abgeleitet wurde. Es kombiniert die Bestätigung des MACD-Momentums mit zwei einfachen gleitenden Durchschnitten, die die Trendrichtung definieren und nachlaufende Ausstiege verwalten. Ein optionaler ADX-Filter sorgt dafür, dass sich die Handelsaktivität auf starke Richtungsbewegungen konzentriert.

Die Standardkonfiguration spiegelt den veröffentlichten Expert Advisor wider: MACD(12, 24, 9), ein 50-Perioden-Management SMA, ein 100-Perioden-Trendfilter SMA und ein gestaffelter Take-Profit, der die Position halbiert, sobald der Preis das Doppelte des ursprünglichen Risikos überschreitet.

## Handelslogik
1. **Trendbias** – Der 100-Perioden-SMA fungiert als Richtungstor. Wenn der Preis darunter schließt, aktiviert er das Long-Setup, während er darüber schließt, aktiviert er das Short-Setup. Sobald ein Handel abgeschlossen wird, wird die Flagge zurückgesetzt, bis der Preis wieder auf die entgegengesetzte Seite zurückkehrt, wodurch aufeinanderfolgende Einstiege ohne Pullback verhindert werden.
2. **MACD-Bestätigung** – Ein Signal ist nur gültig, wenn die MACD-Linie auf der erwarteten Seite von Null liegt und sich innerhalb der letzten *Confirmation Bars*-Kerzen mindestens einmal auf der gegenüberliegenden Seite befand. Dies repliziert die ursprüngliche Schleife, die in einem Schiebefenster nach einem Vorzeichenwechsel suchte.
3. **Eintrittsbedingungen** – Long-Trades erfordern, dass der Kerzenschluss plus der konfigurierte Offset (in Preispunkten) über beiden SMAs liegt, MACD positiv ist und (falls aktiviert) ADX 50 überschreitet. Short-Trades verwenden die Spiegellogik mit negativem MACD und einem Preis unter beiden SMAs.
4. **Anfänglicher Stop** – Der Stop-Loss ist am niedrigsten (für Long-Positionen) oder höchsten (für Short-Positionen) Preis der letzten abgeschlossenen *Stop Bars*-Kerzen verankert und entspricht den Aufrufen von MQL `iLowest`/`iHighest` mit einer Verschiebung um einen Balken.
5. **Positionsmanagement** – Wenn der Preis eine Distanz zurücklegt, die dem *Risikomultiplikator* mal dem anfänglichen Risiko entspricht, wird die Hälfte der Position geschlossen und der Stop auf die Gewinnschwelle verschoben. Die verbleibende Hälfte wird beendet, wenn der Preis so weit zurückgeht, dass der 50-Perioden-SMA über (Long) oder unter (Short) den Offset-bereinigten Schlusskurs kreuzt.
6. **Schutzausstieg** – Jede Kerze, die den gespeicherten Stop-Preis durchbricht, schließt sofort die gesamte Position.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Fast EMA` | Kurze EMA-Periode, die in der MACD-Berechnung verwendet wird. | 12 |
| `Slow EMA` | In der MACD-Berechnung wird ein langer Zeitraum von EMA verwendet. | 24 |
| `Signal EMA` | Glättungszeitraum für die Signalleitung MACD. | 9 |
| `MACD Confirmation Bars` | Maximal erforderliche Kerzen zwischen entgegengesetzten MACD-Messwerten vor einem neuen Eintrag. | 6 |
| `Trend SMA` | Länge des Managements SMA, das nachfolgende Exits schützt. | 50 |
| `Filter SMA` | Länge des Richtungs-SMA, der zum Scharfschalten von langen/kurzen Setups verwendet wird. | 100 |
| `Offset (points)` | Offset (in Instrumentenpunkten), der beim Preisvergleich mit den SMAs addiert oder subtrahiert wird. | 10 |
| `Stop Bars` | Anzahl der vergangenen Kerzen, die beim Festlegen des anfänglichen Stopps berücksichtigt werden. | 6 |
| `Risk Multiplier` | Auf die anfängliche Risikodistanz angewendeter Multiplikator, um die teilweise Gewinnmitnahme auszulösen. | 2,0 |
| `Use ADX` | Aktiviert den Trendstärkenfilter ADX>50. | Deaktiviert |
| `ADX Period` | ADX Lookback wird verwendet, wenn der Filter aktiv ist. | 14 |
| `Candle Type` | An die Indikatoren gelieferte Kerzenserien (standardmäßig 1-Stunden-Balken). | 1H Zeitrahmen |

## Implementierungshinweise
- Preisversätze werden über `Security.Step` (oder `Security.PriceStep`, sofern verfügbar) in absolute Preise umgerechnet. Wenn das Wertpapier keinen Schritt offenlegt, greift die Strategie auf `0.0001` zurück und entspricht dem Verhalten des ursprünglichen FX-fokussierten Beraters.
- Bei Teilausstiegen werden Marktaufträge in der Größe der Hälfte der aktuellen Position verwendet, was die in der Quellimplementierung MQL durchgeführte Lotreduzierung widerspiegelt.
- `StartProtection()` ist aktiviert, um sicherzustellen, dass der integrierte Positionsschutz aktiv ist, bevor neue Trades platziert werden.
- Der Filter ADX ist optional; Wenn er deaktiviert ist, verhält sich der Algorithmus genau wie das historische Skript, indem er ADX durch einen künstlichen Wert von 60 ersetzt.

## Nutzungstipps
1. Konfigurieren Sie die Eigenschaft `Volume`, bevor Sie mit der Strategie beginnen. Es definiert die Basis-Ordergröße, die bei Ein- und Teilausstiegen verwendet wird.
2. Passen Sie `Candle Type` an Ihren bevorzugten Zeitrahmen an. Die ursprüngliche Strategie wurde auf stündliche Daten abgestimmt, aber kürzere Zeiträume können durch Parameteroptimierung untersucht werden.
3. Die Optimierung von `MACD Confirmation Bars`, `Offset (points)` und `Risk Multiplier` hat normalerweise den größten Einfluss auf die Gewinnrate und die Handelshäufigkeit.
