# Trading-Panel-Batch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
`TradingPanelBatchStrategy` ist ein StockSharp-Port des MetaTrader 4-Expertenberaters **EA_TradingPanel**. Das ursprüngliche Skript zeigte ein manuelles Panel an, in dem der Händler die Anzahl der gleichzeitigen Trades, die Losgröße und die Schutzabstände konfigurierte, bevor er **KAUFEN** oder **VERKAUFEN** drückte. In der StockSharp-Version ist das gleiche Verhalten automatisiert: Sobald der Operator den Parameter `Direction` setzt, löst die Strategie eine Reihe von Marktaufträgen für die nächste fertige Kerze aus und setzt die Richtung sofort wieder auf `None` zurück.

Die Logik ist bewusst einfach gehalten, sodass das Modul mit externen Signalen oder manueller Überwachung kombiniert werden kann. Alle Aufträge erben optionale Stop-Loss- und Take-Profit-Distanzen, die in Pips gemessen werden, was die Risikokontrollen widerspiegelt, die in der MQL-Implementierung verfügbar sind.

## Arbeitsablauf
1. Wenn die Strategie startet, berechnet sie die Pip-Größe aus `Security.PriceStep`. Für 1/3/5-stellige Forex-Symbole wird der Wert mit zehn multipliziert, was der MetaTrader-Umrechnung zwischen Punkten und Pips entspricht.
2. Wenn Stop-Loss- oder Take-Profit-Offsets ungleich Null sind, ermöglicht die Strategie `StartProtection`, Exits mit Marktaufträgen zu verwalten.
3. Die Strategie abonniert die durch `CandleType` angegebene Kerzenserie. Nach jeder fertigen Kerze prüft es den Parameter `Direction`.
4. Wenn eine Richtung angefordert wird und die Engine den Handel zulässt, sendet die Strategie `NumberOfOrders` Marktaufträge unter Verwendung von `OrderVolume` für jedes Ticket.
5. Nachdem der Stapel versandt wurde, protokolliert die Strategie die Aktion und setzt `Direction` automatisch auf `None` zurück, bereit für den nächsten manuellen Auslöser.

Dieses Design hält das Modul zwischen den Ausführungen zustandslos. Händler können `Direction` wiederholt auf `Buy` oder `Sell` setzen, wenn sie einen neuen Auftragsstapel benötigen; Die Ausführung erfolgt immer bei der nächsten abgeschlossenen Kerze, um zu vermeiden, dass auf unvollständig gebildete Marktdaten eingegriffen wird.

## Parameter
| Name | Typ | Standard | Beschreibung |
| ---- | ---- | ------- | ----------- |
| `NumberOfOrders` | `int` | `1` | Anzahl der im nächsten Stapel gesendeten Marktaufträge. |
| `OrderVolume` | `decimal` | `0.01` | Auf jede Marktorder angewendetes Volumen. |
| `StopLossPips` | `decimal` | `2` | Stop-Loss-Distanz, umgerechnet von Pips in absoluten Preis unter Verwendung der aktuellen Instrument-Metadaten. Zum Deaktivieren auf `0` setzen. |
| `TakeProfitPips` | `decimal` | `10` | Take-Profit-Distanz in Pips. Zum Deaktivieren auf `0` setzen. |
| `Direction` | `TradeDirection` | `None` | Angeforderte Anweisung für die nächste Ausführung. Die Strategie setzt den Wert zurück, nachdem die Bestellungen aufgegeben wurden. |
| `CandleType` | `DataType` | `TimeFrameCandle(1m)` | Kerzenserie, die zum Auslösen der Ausführung verwendet wird. |

## Notizen
- Die Strategie erfordert ein gültiges `Security` mit ordnungsgemäß konfiguriertem `PriceStep` (und optional `Decimals`). Ohne diese Metadaten fallen die Pip-Berechnungen auf `1` zurück.
- `StartProtection` verwendet Marktaufträge für Exits, um nachzuahmen, wie das MQL-Panel Positionen auf Stop-Loss- oder Take-Profit-Niveau schloss.
- Da die Ausführung bei abgeschlossenen Kerzen erfolgt, können Händler Auftragsstapel mit benutzerdefinierten Analysen oder externen Signalen synchronisieren, indem sie `Direction` aktualisieren, bevor die Kerze schließt.
