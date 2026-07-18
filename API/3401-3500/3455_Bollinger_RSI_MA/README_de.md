# Bollinger RSI MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Bollinger RSI MA-Strategie portiert die MetaTrader-Experten *BolRSIMAs* auf die StockSharp-High-Level-API. Das System kombiniert a
Bollinger-Bandausbruch, ein RSI-Filter und ein exponentieller gleitender Durchschnitt mit höherem Zeitrahmen (EMA) zur Identifizierung von Pullback-Trades
die Richtung des vorherrschenden Trends. Die automatische Losgröße bleibt erhalten: Wenn sie aktiviert ist, konvertiert die Strategie das konfigurierte Risiko
Bruchteil des Portfolio-Eigenkapitals in Volumen unter Verwendung des aktuellen Preises, der Stop-Distanz Bollinger und der Kontraktgröße des Instruments.

## Handelslogik
1. Abonnieren Sie die primäre Kerzenserie (Standard: 1 Stunde) und berechnen Sie Bollinger Bänder und RSI im selben Zeitrahmen.
2. Abonnieren Sie Tageskerzen und geben Sie deren Schlusskurse in einen EMA mit 200 Perioden ein, um den verwendeten Filter für höhere Zeitrahmen zu reproduzieren
im Original EA.
3. Generieren Sie ein **Long**-Setup, wenn die letzte Kerze unterhalb des unteren Bandes schließt und der RSI-Wert unter dem überverkauften Schwellenwert liegt
und der Schlusskurs bleibt über dem Tageskurs EMA. Ein **Short**-Setup wird durch einen Schlusskurs über dem oberen Band, RSI über dem, ausgelöst
Überkaufschwelle und Preis unter dem täglichen EMA.
4. Offene Positionen nur, wenn kein Exposure aktiv ist. Jeder neue Handel speichert daraus abgeleitete Stop-Loss- und Take-Profit-Werte
vorherige Bollinger-Werte: Longs verwenden `lowerBand - StopLossOffset` und zielen auf das mittlere Band; Shorts verwenden
`upperBand + StopLossOffset` und zielen Sie auch auf das mittlere Band.
5. Bei jeder fertigen Kerze vergleicht die Strategie die Kerzenextremwerte mit den Schutzniveaus. Wenn das Tief/Hoch das berührt
Stop oder Target, die Position wird sofort geschlossen, wobei die Schutzaufträge der MetaTrader-Version nachgeahmt werden.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 1-Stunden-Kerzen | Primärer Zeitrahmen verarbeitet von Bollinger Bändern und RSI. |
| `DailyCandleType` | 1-Tages-Kerzen | Höherer Zeitrahmen, der den EMA-Trendfilter speist. |
| `BollingerPeriod` | `20` | Anzahl der Kerzen, die zum Aufbau von Bollinger-Bändern verwendet werden. |
| `BollingerDeviation` | `2` | Bandbreitenmultiplikator. |
| `RsiPeriod` | `13` | RSI Glättungslänge. |
| `RsiUpperLevel` | `70` | Für Short-Trades ist ein Überkaufschwellenwert erforderlich. |
| `RsiLowerLevel` | `30` | Für Long-Trades ist ein Überverkaufsschwellenwert erforderlich. |
| `MaPeriod` | `200` | Länge des höheren Zeitrahmens EMA. |
| `StopLossOffset` | `0.0238` | Zusätzlicher Puffer, der außerhalb des Bandes hinzugefügt wird, bevor der Stop-Loss platziert wird. |
| `UseAutoLot` | `true` | Ermöglicht eine risikobasierte Positionsgrößenbestimmung. |
| `RiskPerTrade` | `0.05` | Anteil des Eigenkapitals, der jedem Trade zugewiesen wird, wenn Auto-Lot aktiv ist. |
| `FixedVolume` | `0.1` | Bestellgröße, wenn die automatische Losgrößenbestimmung deaktiviert ist. |

## Geldmanagement
- Wenn `UseAutoLot` gleich `true` ist, entspricht das Volumen `(equity * RiskPerTrade) / (StopLossOffset * price * contractSize)`, gerundet auf
Umtauschlimits. Dies spiegelt die MetaTrader-Autolot-Routine wider, die den Risikobetrag durch die Stoppdistanz in Bargeld und dividiert
die Vertragsgröße.
- Wenn keine Aktieninformationen oder kein Preis verfügbar sind, fällt die Strategie auf `FixedVolume` zurück und berücksichtigt dabei weiterhin die
Beschränkungen der Instrumentenlautstärke.

## Unterschiede zum MetaTrader-Experten
- Stop-Loss- und Take-Profit-Orders werden durch Kerzenhochs und -tiefs anstelle von serverseitigen Orders simuliert, die dem entsprechen
Ergebnis des ursprünglichen EA, ohne auf synchrone Auftragsübermittlung angewiesen zu sein.
- Der EMA-Filter verwendet die Kerzenabonnements von StockSharp; Es besteht keine Abhängigkeit von MetaTrader-spezifischen täglichen Datenabrufen.
- Bei der Risikogröße werden StockSharp Sicherheitsgrenzen (`MinVolume`, `MaxVolume`, `VolumeStep`) berücksichtigt, um abgelehnte Aufträge an Börsen zu vermeiden.

## Anwendungstipps
- Passen Sie `StopLossOffset` an, wenn Sie Symbole mit unterschiedlichen Preisskalen handeln, sodass der Abstand den ursprünglichen EAs entspricht
2,38 % Puffer über dem Bollinger-Band.
- Wenn das Instrument einen anderen täglichen Zeitrahmen verwendet (z. B. Krypto-Börsen), ändern Sie `DailyCandleType` entsprechend, sodass EMA
spiegelt den beabsichtigten Trendfilter wider.
- Kombinieren Sie die Strategie mit externen Trailing Stops, wenn Sie dynamische Ausstiege bevorzugen, sobald das mittlere Bandziel erreicht ist.
