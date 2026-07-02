# Elite eFibo Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Elite eFibo Trader reproduziert den durchschnittlichen Expertenberater, der eine Fibonacci-Reihenfolge von Aufträgen eröffnet und dabei einen gleitenden Durchschnitt-Crossover und einen optionalen RSI-Filter überwacht. Der StockSharp-Port behält die ursprüngliche Korblogik bei: Ein Markteintritt löst einen Stapel ausstehender Stop-Orders aus, die durch konfigurierbare Pip-Abstände voneinander getrennt sind, und jede weitere Ausführung erhöht das Engagement gemäß der Fibonacci-Sequenz. Die Strategie flacht den Korb automatisch ab, sobald der variable Gewinn ein Cash-Ziel erreicht oder wenn der Trendfilter sich gegen das aktuelle Engagement wendet.

## Marktdaten
- Abonniert einen einzelnen konfigurierbaren Kerzentyp (Standard: 15-Minuten-Kerzen).
- Verwendet den Kerzenschluss für Indikatorwerte und zur Bewertung von Trailing-/Stop-Bedingungen.

## Eingabelogik
1. Die Richtung wird entweder durch den Crossover des gleitenden Durchschnitts (standardmäßig aktiviert) oder durch die manuelle Umschaltung `ManualOpenBuy`/`ManualOpenSell` bestimmt.
2. Wenn die MA-Logik aktiv ist, führt ein bullischer Crossover (`fast` über `slow`) zu Kaufkörben und ein bärischer Crossover zu Verkaufskörben. Es wird ein einzelnes Signal pro Kerze erzwungen.
3. Wenn der Filter RSI aktiviert ist, erfordern lange Körbe `RSI > RsiHigh`, während kurze Körbe `RSI < RsiLow` erfordern.
4. Eine neue Leiter wird nur geöffnet, wenn keine aktiven Aufträge oder Positionen aus der Strategie vorhanden sind und der Handel zulässig ist (`TradeAgainAfterProfit`).
5. Die erste Ebene wird mit einer Marktorder eröffnet, während die restlichen Ebenen als Stop-Orders mit einem Versatz von `LevelDistancePips` übermittelt werden. Die Lautstärken folgen der Fibonacci-Reihenfolge und können Stufe für Stufe angepasst werden.

## Exit-Logik
- Jede gefüllte Ebene erhält einen anfänglichen Stopp, der ab `StopLossPips` berechnet wird, und nimmt an einer nachlaufenden Aktualisierung teil, wenn die MA-Logik einen nachteiligen Crossover erkennt.
- Stopps werden für lange Körbe auf `close - TrailingStopPips` und für kurze Körbe auf `close + TrailingStopPips` verschoben und bewegen sich nie weiter als der aktuelle Stopp.
- Wenn der Preis einen Level-Stop erreicht (basierend auf dem Hoch/Tief der Kerze), schließt die Strategie das verbleibende Volumen dieses Levels mit einer Marktorder.
- Wenn der variable Gewinn des Korbs (berechnet aus den Instrumenten `PriceStep` und `StepPrice`) `MoneyTakeProfit` erreicht, werden alle Positionen geschlossen und ausstehende Aufträge storniert.
- Sobald der Korb flach ist, werden alle ausstehenden Stop-Orders automatisch storniert. Wenn `TradeAgainAfterProfit` den Wert `false` hat, bleibt die Strategie inaktiv, bis sie zurückgesetzt wird.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| `UseMaLogic` | Aktivieren oder deaktivieren Sie die Crossover-Logik des gleitenden Durchschnitts, die die Handelsrichtung festlegt. |
| `MaSlowPeriod`, `MaFastPeriod` | Perioden der langsamen und schnellen SMAs. |
| `TrailingStopPips` | Pip-Distanz, die vom schützenden Trailing-Stop verwendet wird, wenn der Trendfilter nachteilig wird. |
| `UseRsiFilter`, `RsiPeriod`, `RsiHigh`, `RsiLow` | RSI Filterkonfiguration. Der Filter erlaubt Long-Positionen über `RsiHigh` und Shorts unter `RsiLow`. |
| `ManualOpenBuy`, `ManualOpenSell` | Manuelles Umschalten wird verwendet, wenn die MA-Logik deaktiviert ist. |
| `TradeAgainAfterProfit` | Nehmen Sie den Handel wieder auf, nachdem Sie den Cash-Take-Profit erreicht haben. |
| `LevelDistancePips` | Abstand in Pips zwischen aufeinanderfolgenden ausstehenden Aufträgen. |
| `StopLossPips` | Anfänglicher Stopp-Offset für jedes Level. |
| `MoneyTakeProfit` | Cash-Gewinnziel, bewertet anhand der offenen Gewinn- und Verlustrechnung des Korbs. |
| `Level1Volume` … `Level14Volume` | Lautstärke jedes Fibonacci Levels. Auf Null setzen, um eine Ebene zu deaktivieren. |
| `CandleType` | Zeitrahmen/Datentyp, der für Indikatoren verwendet wird. |

## Hinweise zur Implementierung
- Pip-Abstände werden aus Punkten im MetaTrader-Stil umgerechnet, indem das Instrument `PriceStep` mit zehn multipliziert wird, wenn das Wertpapier 3 oder 5 Dezimalstellen hat. Dies spiegelt die ursprüngliche `MyPoint`-Anpassung für 5-stellige FX-Kurse wider.
- Jede Ebene wird unabhängig verfolgt. Die Strategie speichert den Einstiegspreis, das verbleibende Volumen und das Stop-Level, sodass Teilfüllungen und einzelne Stop-Outs auf die gleiche Weise wie der MQL-Experte gehandhabt werden.
- Der variable Gewinn wird aus `PriceStep` und `StepPrice` berechnet. Stellen Sie sicher, dass diese Instrumenteigenschaften konfiguriert sind, andernfalls wird die Geldmitnahme nicht korrekt ausgelöst.
- `StartProtection()` wird einmal während des Startvorgangs aufgerufen, um die integrierten Sicherheitsprüfungen der Strategiebasisklasse StockSharp zu aktivieren.
- Wenn kein offenes Volume mehr vorhanden ist, wird `CancelAllPendingOrders()` automatisch aufgerufen und repliziert die wiederholten `subCloseAllPending()`-Aufrufe aus dem ursprünglichen Skript.

## Anwendungstipps
- Überprüfen Sie die Brokereinstellungen für `PriceStep`, `StepPrice`, `VolumeStep` und die Mindestlosgröße, um sicherzustellen, dass Volumen von Fibonacci in gültige Aufträge umgewandelt werden.
- Die Strategie basiert auf Kerzendaten; Stellen Sie sicher, dass der ausgewählte Zeitrahmen mit dem beabsichtigten Diagrammzeitraum MetaTrader übereinstimmt.
- Erwägen Sie, die Strategie zunächst auf Demo-Feeds anzuwenden: Mittelungssysteme können bei ungünstigen Trends ein hohes Risiko eingehen.
- Deaktivieren Sie `UseMaLogic`, um die in den ursprünglichen EA-Eingaben verwendete manuelle Tendenz zu reproduzieren, oder lassen Sie es für die automatische Trenderkennung aktiviert.
