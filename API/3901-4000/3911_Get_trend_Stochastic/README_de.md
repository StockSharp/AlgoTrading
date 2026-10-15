# Holen Sie sich die trendige Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie ist eine StockSharp High-Level-Portierung des MetaTrader 4 Expert Advisors **Get trend.mq4**. Es wertet das M15-Diagramm aus
Einträge, validiert den breiteren Trend im ersten Halbjahr und stützt sich auf zwei geglättete gleitende Durchschnitte zusammen mit einem Paar stochastischer Werte
Oszillatoren zur Erkennung von Mean-Reversion-Ausbrüchen in der Nähe des längerfristigen Trends. Die Implementierung behält die ursprüngliche Geldverwaltung bei
Regeln, die auf festen Take-Profit-, Stop-Loss- und Trailing-Stop-Abständen basieren, ausgedrückt in Preispunkten.

## Handelslogik

1. **Indikatoren und Daten**
   - M15-Kerzen speisen einen geglätteten gleitenden Durchschnitt (SMMA, Medianpreis) mit der Periode `M15MaPeriod` und zwei stochastischen Oszillatoren.
   - H1-Kerzen versorgen einen weiteren SMMA (Medianpreis) mit der Periode `H1MaPeriod`.
   - Die schnelle Stochastik (`FastStochasticPeriod`, 3, 3) liefert die %K-Linie und ihren vorherigen Wert. Die langsame Stochastik (`SlowStochasticPeriod`, 3, 3) liefert die %D-Signalleitung.
2. **Lange Einrichtung**
   - Der aktuelle M15-Schlusskurs liegt unter seinem SMMA und der H1-Schlusskurs liegt unter seinem eigenen SMMA.
   - Der Abstand zwischen dem M15 SMMA und dem Schlusskurs liegt innerhalb von `ThresholdPoints` Preisschritten.
   - Beide stochastischen Linien liegen unter 20. Die schnelle Linie kreuzt während der letzten Kerze die langsame Linie (`fast` > `slow`, während der vorherige schnelle Wert unter `slow` lag).
   - Wenn eine Short-Position besteht, kauft die Strategie zunächst genug Volumen, um diese zu glätten, und eröffnet dann eine neue Long-Position mit `TradeVolume`.
3. **Kurzer Aufbau** spiegelt die lange Logik wider:
   - Beide Schlusskurse liegen über ihren SMMAs, der Abstand liegt innerhalb von `ThresholdPoints`, die stochastischen Werte liegen über 80 und der schnelle
Linie kreuzt unterhalb der langsamen Linie. Die Strategie verkauft und schließt bei Bedarf eine bestehende Long-Position.
4. **Risikomanagement**
   - Nach jeder Eingabe werden Schutzaufträge zu `StopLossPoints` und `TakeProfitPoints` (umgerechnet in einen absoluten Preis) platziert
Entfernungen anhand der Preisstufe des Instruments).
   - Ein Trailing Stop richtet die Stop-Loss-Order neu aus, sobald der Trade mindestens `TrailingStopPoints` Punkte erreicht. Die neue Haltestelle ist
positioniert am aktuellen Schlusskurs minus/plus der Nachlaufdistanz für Longs/Shorts.
   - Wenn die Position wieder flach ist, werden alle Schutzaufträge storniert.

## Unterschiede zum Original EA

- Der SMMA von MetaTrader verwendet eine Indikatorverschiebung von acht Balken; StockSharp-Indikatoren machen keine direkte Verschiebungseinstellung verfügbar. Der Hafen
wertet stattdessen den aktuellsten Endwert aus. Dadurch wird das Crossover-Timing beibehalten und gleichzeitig zusätzliche benutzerdefinierte Puffer vermieden.
- Der ursprüngliche EA verwendete die Bid/Ask-Kurse von MQL für den Schluss. Der Port verwendet den abgeschlossenen Kerzenschluss, der das Trailing ausgelöst hat
Update, das dem nächsten verfügbaren Analogon im High-Level API entspricht.
- Die Geldverwaltung stützt sich stattdessen auf die Auftragsregistrierungshilfen von StockSharp (`BuyMarket`, `SellMarket`, `SellStop` usw.).
`OrderSend` und `OrderModify`.

## Parameter

| Gruppe | Name | Beschreibung | Standard |
|-------|------|-------------|---------|
| Daten | `M15 Candle Type` | Kerzentyp/Zeitrahmen, der für die Hauptberechnungen verwendet wird. | Zeitrahmen M15 |
| Daten | `H1 Candle Type` | Kerzentyp/Zeitrahmen, der zur Bestätigung verwendet wird. | H1-Zeitrahmen |
| Indikatoren | `M15 SMMA Period` | Länge des geglätteten gleitenden Durchschnitts der M15-Serie. | 200 |
| Indikatoren | `H1 SMMA Period` | Länge des geglätteten gleitenden Durchschnitts der H1-Serie. | 200 |
| Indikatoren | `Slow Stochastic Period` | %K-Länge für den langsamen stochastischen Oszillator, der die %D-Linie bereitstellt. | 14 |
| Indikatoren | `Fast Stochastic Period` | %K-Länge für den schnellen stochastischen Oszillator, der die %K-Hauptleitung bereitstellt. | 14 |
| Signale | `Threshold (points)` | Maximaler Abstand zwischen dem M15 SMMA und dem aktuellen Nahbereich, um Eingaben zu ermöglichen. | 50 |
| Risiko | `Take Profit (points)` | Take-Profit-Distanz, ausgedrückt in Preisschritten. | 570 |
| Risiko | `Stop Loss (points)` | Stop-Loss-Distanz ausgedrückt in Preisschritten. | 30 |
| Risiko | `Trailing Stop (points)` | Trailing-Stop-Distanz, ausgedrückt in Preisschritten. | 200 |
| Handel | `Trade Volume` | Mit jeder Marktorder gesendetes Volumen. | 0,1 |

## Hinweise zur Nutzung

- Stellen Sie sicher, dass das gehandelte Wertpapier `PriceStep` offenlegt; Andernfalls fallen die punktbasierten Abstände auf `1` zurück, was zu großen Entfernungen führen kann
Schutzanordnungen für Instrumente, die in Bruchteilen notiert sind.
- Die Strategie storniert Stop-Orders und erstellt sie neu, sobald ein besseres Trailing-Level erkannt wird. Makler, die häufiges verbieten
Änderungen erfordern möglicherweise eine Drosselung.
- Da der Port nur mit fertigen Kerzen arbeitet, ist das System für Backtests und die Ausführung am Ende des Balkens konzipiert. Läuft weiter
Live-Tick-Daten erfordern den Abgleich der Kerzenaufbaueinstellungen zwischen dem Terminal und StockSharp.
