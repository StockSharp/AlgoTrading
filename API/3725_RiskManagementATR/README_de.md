# Risikomanagement ATR Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Risikomanagement-ATR-Strategie ist eine StockSharp-Umsetzung des MetaTrader 5-Experten *Risikomanagement EA basierend auf ATR Volatilität*. Das ursprüngliche EA konzentrierte sich auf die automatische Größenbestimmung von Positionen entsprechend dem Kontostand und der aktuellen Marktvolatilität, gemessen durch den Average True Range (ATR). Der StockSharp-Port behält die gleiche Philosophie bei: Er eröffnet nur Long-Positionen, wenn ein einfacher gleitender 10-Perioden-Durchschnitt einen einfachen gleitenden 20-Perioden-Durchschnitt überschreitet, und jede Einstiegsgröße wird so berechnet, dass der potenzielle Verlust am Schutzstopp dem konfigurierten Risikoprozentsatz entspricht.

Die Konvertierung folgt dem übergeordneten StockSharp API. Indikatorberechnungen basieren auf den Komponenten `AverageTrueRange` und `SimpleMovingAverage`, die an das Kerzenabonnement angehängt sind, und nicht auf direkten Indikatoraufrufen. Das Handelsmanagement verwendet StockSharp-Hilfsmethoden wieder und bricht den Schutzstopp nach jeder Füllung ab und erstellt ihn neu, sodass die Nettoposition und die Stop-Order immer übereinstimmen.

## Handelslogik
1. Abonnieren Sie den durch `CandleType` definierten Zeitrahmen und warten Sie, bis die Kerzen vollständig geschlossen sind, um vorzeitige Entscheidungen zu vermeiden.
2. Geben Sie einen 14-Perioden-ATR und zwei einfache gleitende Durchschnitte (Längen 10 und 20) mit den Abonnementdaten ein.
3. Wenn der schnell gleitende Durchschnitt über dem langsam gleitenden Durchschnitt schließt und es keine offene Position gibt, berechnen Sie die Positionsgröße basierend auf dem ausgewählten Risikomodell und erteilen Sie eine Marktkauforder.
4. Berechnen Sie nach jeder Füllung die Stop-Loss-Distanz: entweder `ATR * AtrMultiplier` oder eine feste Anzahl von Preisschritten, wenn `UseAtrStopLoss` deaktiviert ist.
5. Runden Sie den Stop-Preis auf den nächsten Tick ab und platzieren Sie eine `SellStop`-Order mit der aktuellen Positionsgröße. Jeder vorherige Stopp wird gelöscht, bevor der neue registriert wird.
6. Wenn die Stop-Order ausgeführt wird und die Position auf Null zurückkehrt, löscht die Strategie ihren internen Zustand und ist bereit für den nächsten Crossover.

## Risikomanagement
- `RiskPercentage` bestimmt, wie viel vom Portfoliowert bei einem einzelnen Trade verloren gehen kann. Die Strategie liest `Portfolio.CurrentValue` (oder `BeginValue` als Fallback) und multipliziert ihn mit dem Prozentsatz, um das zulässige monetäre Risiko zu erhalten.
- Das zulässige Risiko wird durch die Stop-Loss-Distanz dividiert, um das Handelsvolumen zu erhalten. Bei der Volumenrundung werden der Volumenschritt des Instruments sowie Mindest- und Höchstbeschränkungen berücksichtigt, sodass die generierten Aufträge an der Börse gültig bleiben.
- Wenn `RiskPercentage` auf `0` gesetzt ist, greift die Strategie auf die Standardeigenschaft `Volume` zurück (standardmäßig 1 Lot), während der automatische Schutzstopp beibehalten wird.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Zeitrahmen von 1 Minute | Von der Strategie verarbeitete primäre Kerzenserie. |
| `AtrPeriod` | `int` | `14` | Anzahl der Kerzen, die zum Glätten des ATR-Indikators verwendet werden. |
| `AtrMultiplier` | `decimal` | `2.0` | Auf den ATR-Wert angewendeter Multiplikator, um die Stop-Loss-Distanz abzuleiten. |
| `RiskPercentage` | `decimal` | `1.0` | Prozentsatz des Portfoliowerts, der bei jedem Trade riskiert wird. Auf Null setzen, um ein festes Volumen zu verwenden. |
| `UseAtrStopLoss` | `bool` | `true` | Wenn aktiviert, wird der Stopp bei `ATR * AtrMultiplier` platziert; andernfalls wird ein fester Abstand verwendet. |
| `FixedStopLossPoints` | `int` | `50` | Anzahl der Preisschritte, die für den Schutzstopp verwendet werden, wenn die ATR-basierte Platzierung deaktiviert wird. |

## Unterschiede zum Original EA
- StockSharp arbeitet mit Nettopositionen, daher übermittelt die Konvertierung nur Marktkaufaufträge. Ausstiege erfolgen durch das schützende `SellStop`, das das EA-Verhalten reproduziert, nach einem Stopp immer flach zu sein.
- MetaTrader stellt die Konstante `_Point` für die Tick-Größe bereit. Der Port fragt `Security.PriceStep` ab und greift auf eine einzelne Währungseinheit zurück, wenn das Instrument keine Tick-Spezifikation bereitstellt.
- Bei der Positionsgröße werden die Volumenfilter von StockSharp (`VolumeStep`, `MinVolume`, `MaxVolume`) berücksichtigt, um sicherzustellen, dass das Orderbuch die generierten Ordergrößen akzeptiert.
- Die Indikatorverarbeitung erfolgt ereignisgesteuert über `Subscription.Bind(...)` statt synchroner `iMA`/`iATR`-Aufrufe.

## Anwendungstipps
- Stellen Sie sicher, dass das verbundene Portfolio einen korrekten `CurrentValue` meldet; andernfalls sinkt die risikobasierte Positionsgröße auf das Volumen Null zurück.
- Die Eigenschaft `Volume` fungiert weiterhin als Sicherheitsnetz. Wenn Sie unabhängig von ATR-Berechnungen eine feste Losgröße wünschen, setzen Sie `RiskPercentage` auf Null und passen Sie `Volume` an, bevor Sie mit der Strategie beginnen.
- Hängen Sie die Strategie an ein Diagramm an, um die Kerzen, sowohl gleitende Durchschnitte als auch ausgeführte Trades, zu visualisieren. Es hilft zu bestätigen, dass neue Einträge nur dann erscheinen, wenn der schnelle Durchschnitt über dem langsamen schließt und dass Stopps genau unter der letzten Preisschwankung liegen.
- Erwägen Sie, `AtrMultiplier` bei volatileren Instrumenten zu erhöhen, um vorzeitige Stop-Outs zu vermeiden, oder deaktivieren Sie die ATR-basierte Platzierung und stellen Sie eine benutzerdefinierte feste Distanz durch `FixedStopLossPoints` bereit.

## Indikatoren
- `AverageTrueRange` (Länge `AtrPeriod`).
- `SimpleMovingAverage` (schnelle Länge `10`).
- `SimpleMovingAverage` (langsame Länge `20`).
