# Weitere Bestellungen nach BreakEven (StockSharp Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser Ordner enthält einen StockSharp C#-Port des MetaTrader 4 Expert Advisors **"More Orders After BreakEven"** (MQL Quell-ID `35609`). Das ursprüngliche EA fügt wiederholt neue Long-Positionen hinzu, sobald frühere Trades zum Break-Even geschützt wurden. Der Port reproduziert diese Ticket-basierte Geldverwaltung und integriert gleichzeitig die High-Level-Lösung API von StockSharp.

## Strategieübersicht

* **Marktseite** – nur lang. Bei jedem Trade handelt es sich um eine marktübliche Kauforder für das Primärwertpapier der Strategie.
* **Kernidee** – obwohl es weniger offene Trades ohne Break-Even-Schutz gibt als `MaximumOrders`, kauft die Strategie erneut. Wenn ein bestehender Trade die Break-Even-Distanz erreicht, wird sein Stop-Loss auf den Einstiegspreis erhöht, sodass weitere Einträge nicht mehr blockiert werden.
* **Exit-Management** – jede Order speichert ihre eigenen Stop-Loss- und Take-Profit-Level. Stopps werden auf die Gewinnschwelle verschoben, wenn der Preis um `BreakEvenPips` steigt. Marktverkaufsaufträge schließen Positionen, wenn der Geldkurs eines der Schutzniveaus berührt.
* **Tick-Verarbeitung** – das Original EA funktionierte bei jedem Tick über `OnTick`. Der Port verwendet Marktdaten der Ebene 1, um die besten Geld-/Briefpreise zu überwachen, und emuliert dasselbe Verhalten: Bei jeder Aktualisierung werden Einträge, Break-Even-Regeln und potenzielle Ausstiege bewertet.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `MaximumOrders` | Maximale Anzahl an Long-Trades, deren Stop-Loss die Gewinnschwelle noch nicht erreicht hat. Sobald die Zahl unter diesen Schwellenwert fällt, können neue Positionen eröffnet werden. | `1` |
| `TakeProfitPips` | Abstand vom Einstiegspreis zum Take-Profit-Ziel, ausgedrückt in MetaTrader Pips. Ein Wert von `0` deaktiviert den Take-Profit. | `100` |
| `StopLossPips` | Anfangsabstand zum Schutzstopp in MetaTrader Pips. Auf `0` setzen, um die Position ohne anfänglichen Stopp zu verlassen (die Break-Even-Regel kann sie auch später noch schützen). | `200` |
| `BreakEvenPips` | Gewinndistanz (in MetaTrader Pips), nach der der Stop-Loss auf den Einstiegspreis angehoben wird. `0` bedeutet, dass der Stop die Gewinnschwelle erreicht, sobald der Preis den Einstiegspreis überschreitet. | `10` |
| `TradeVolume` | Mit jeder Marktkauforder übermitteltes Volumen. | `0.01` |
| `DebugMode` | Wenn die Strategie aktiviert ist, protokolliert sie Informationsmeldungen, die die `Comment()`-Ausgabe des ursprünglichen EA nachahmen. | `true` |

Alle Pip-basierten Abstände passen sich automatisch an 4/2- und 5/3-stellige Forex-Symbole an, indem die Tick-Größe und die Dezimalgenauigkeit des Instruments analysiert werden und der Skalierungsfaktor `points` aus dem Originalcode reproduziert wird.

## Handelslogik

1. **Abonnement der Stufe 1** – Die Strategie abonniert die besten Geld-/Briefkurse. Immer wenn beide Preise bekannt sind, emuliert `ProcessPrices` die Schleife MQL `OnTick`.
2. **Auftragszählung** – bevor eine neue Bestellung aufgegeben wird, zählt die Strategie offene Einträge, die noch nicht die Gewinnschwelle erreicht haben. Dies reproduziert den ursprünglichen `OrdersCounter()`-Helfer.
3. **Einträge** – wenn die Anzahl unter `MaximumOrders` liegt, wird eine neue Kauf-Market-Order mit `TradeVolume` übermittelt. Der Füllpreis wird aufgezeichnet und die Stop-/Take-Profit-Level pro Ticket werden initialisiert.
4. **Break-even-Update** – für jeden aktiven Eintrag wird der Gebotspreis mit dem Break-even-Trigger verglichen. Sobald der Stop-Loss überschritten wird, wird er auf den Einstiegspreis verschoben, wodurch das Ticket als geschützt markiert wird, sodass es nicht mehr zur Orderzählung beiträgt.
5. **Exit-Checks** – der Gebotspreis steuert auch die Exit-Erkennung. Wenn es den gespeicherten Take-Profit erreicht oder auf den Stop-Loss (einschließlich des Break-Even-Stopps) fällt, erteilt die Strategie einen Marktverkaufsauftrag für das verbleibende Volumen dieses Tickets.
6. **Positionsverfolgung** – über `OnOwnTradeReceived` empfangene Füllungen führen eine FIFO-Liste mit Einträgen. Dies reproduziert das Ticketverhalten von MetaTrader, bei dem jede Bestellung einzeln bearbeitet werden kann, obwohl StockSharp die Nettoposition aggregiert.

## Unterschiede zum Original EA

* Es werden nur Long-Trades implementiert, da die MQL-Version nie Verkaufseinträge ausgegeben hat.
* Stop- und Take-Profit-Aufträge auf Brokerseite werden durch strategieseitige Überwachung und Marktausstiege ersetzt. Dies ist notwendig, da StockSharp Stopps pro Order im übergeordneten API nicht automatisch ändert.
* Die Diagnoseausgabe verwendet das Protokollierungssystem von StockSharp anstelle von `Comment()`-Text im Diagramm MetaTrader.

## Nutzungshinweise

1. Hängen Sie die Strategie an einen Connector an, der Level-1-Daten für das ausgewählte Wertpapier bereitstellt.
2. Konfigurieren Sie die Pip-basierten Parameter so, dass sie der Volatilität des Instruments und den Brokeranforderungen entsprechen.
3. Aktivieren Sie `DebugMode` während des Tests, um die Auftragszählung und das Break-Even-Verhalten zu überprüfen, und deaktivieren Sie es dann in der Produktion für leisere Protokolle.
4. Da Ausstiege über Marktaufträge abgewickelt werden, stellen Sie sicher, dass das Portfolio über genügend Kaufkraft verfügt, um alle zusätzlichen Einstiege abzudecken, die ausgelöst werden können, sobald der Break-Even-Schutz greift.

## Quellenangabe

* Ursprüngliche MQL4-Datei: `MQL/35609/More Orders After BreakEven.mq4`.
* Konvertierte C#-Strategie: `CS/MoreOrdersAfterBreakEvenStrategy.cs`.
