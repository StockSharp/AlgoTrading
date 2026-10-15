# Einfache WPR-Strategie für benutzerdefinierte Cloud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **WPR Custom Cloud Simple Strategy** ist ein StockSharp-Port des MetaTrader-Expertenberaters `WPR Custom Cloud Simple.mq5`. Der EA überwacht den %R-Oszillator von Larry Williams und eröffnet Geschäfte, wenn der Indikator den überverkauften oder überkauften Bereich verlässt. Diese C#-Version behält das ursprüngliche Design des Handels nur bei neuen Kerzen bei, kehrt die Position um, wenn ein entgegengesetztes Signal erscheint, und vermeidet Stop-Loss- oder Take-Profit-Orders genau wie die Referenzimplementierung.

## Handelslogik
1. Abonnieren Sie den konfigurierten Zeitrahmen (`CandleType`) und füttern Sie einen `WilliamsR`-Indikator mit den eingehenden Kerzen.
2. Warten Sie, bis die Kerze fertig ist. Die Strategie wirkt sich niemals auf unvollständige Balken aus.
3. Speichern Sie die letzten beiden abgeschlossenen %R-Werte. Sie spiegeln die Messwerte `wpr[1]` und `wpr[2]` von MetaTrader wider.
4. Signale an Kreuzungen erzeugen:
   - **Long Setup**: Der vorherige Balken schließt über `OversoldLevel`, während der Balken davor unter diesem Niveau lag. Dadurch wird die Bedingung „Ausstieg aus Überverkauft“ (`wpr[2] < level` und `wpr[1] > level`) aus EA neu erstellt.
   - **Kurze Einrichtung**: Der vorherige Balken schließt unter `OverboughtLevel`, während der frühere Balken darüber lag, was mit der ursprünglichen `wpr[2] > level`- und `wpr[1] < level`-Prüfung übereinstimmt.
5. Wenn ein Long-Setup auftritt, glätten Sie das Short-Engagement und kaufen Sie ein Nettovolumen. Wenn ein Short-Setup ausgelöst wird, glätten Sie die Long-Seite und verkaufen Sie ein Nettovolumen. Da StockSharp mit Nettopositionen arbeitet, reproduziert das Senden von `BuyMarket`/`SellMarket` mit `Volume + |Position|` perfekt den Close-and-Reverse-Ablauf vom Sicherungskonto von MetaTrader.
6. Es werden keine zusätzlichen Ausgänge verwendet; Ein neuer entgegengesetzter Crossover ist die einzige Möglichkeit, Geschäfte abzuschließen, genau wie im ursprünglichen Berater.

## Parameter
| Name | Typ | Standard | MetaTrader Gegenstück | Beschreibung |
| --- | --- | --- | --- | --- |
| `WprPeriod` | `int` | `14` | `Inp_WPR_Period` | Lookback-Länge für die Williams %R-Berechnung. |
| `OverboughtLevel` | `decimal` | `-20` | `Inp_WPR_Level1` | Schwellenwert, der den überkauften Bereich definiert. Eine Unterschreitung löst Shorts aus. |
| `OversoldLevel` | `decimal` | `-80` | `Inp_WPR_Level2` | Schwellenwert, der den überverkauften Bereich definiert. Ein Überschreiten dieser Grenze löst Long-Positionen aus. |
| `CandleType` | `DataType` | 1-stündiger Zeitrahmen | `InpWorkingPeriod` | Kerzenserien, die zur Aktualisierung des Indikators und zur Auswertung von Signalen verwendet werden. |
| `Volume` | `decimal` | Basisvolumen der Strategie | `InpLots` | Losgröße für Marktaufträge. Die Strategie gleicht die aktuelle Nettoposition automatisch aus, bevor ein neuer Trade eröffnet wird. |

## Unterschiede zum Original EA
- StockSharp arbeitet mit Nettopositionen. Die Schließung des gegenteiligen Risikos erfolgt durch Erhöhung des Marktauftragsvolumens, sodass das Verhalten dem Absicherungsmodell ohne zusätzliche Buchhaltungsstrukturen wie `STRUCT_POSITION` entspricht.
- Alle Hilfsklassen zur Auftragsverwaltung (`CTrade`, `CPositionInfo`, Margenprüfungen usw.) werden durch die integrierten Risikokontrollen von StockSharp ersetzt. Die Strategie basiert auf `Strategy.Volume` und den Austauschmetadaten anstelle manueller Berechnungen der freien Marge.
- Die Protokollierung wird vereinfacht. Die Version StockSharp vermeidet ausführliche `Print`-Anweisungen, da die High-Level-Version API bereits Bestellstatusaktualisierungen bereitstellt.
- Schutzanordnungen werden absichtlich weggelassen, um das „Schließen bei entgegengesetztem Signal“-Design der Quelle EA widerzuspiegeln.

## Anwendungstipps
- Passen Sie `CandleType` an den gleichen Zeitrahmen an, den Sie in MetaTrader verwendet haben, um die Crossover-Häufigkeit vergleichbar zu halten.
- Williams %R-Schwellenwerte sind negative Werte. Wenn Sie `OverboughtLevel` näher an Null heranrücken, werden Short-Einträge seltener, während eine Verschiebung von `OversoldLevel` in Richtung `-100` Long-Einträge seltener macht.
- Die Strategie geht davon aus, dass `Volume` bereits den Mindestschritt- und Netting-Regeln des Brokers entspricht. Passen Sie das Basisvolumen in der Benutzeroberfläche oder per Code an, bevor Sie mit dem Live-Handel beginnen.
