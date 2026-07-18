# Pedro Mod-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine StockSharp-Portierung des **Pedroxxmod** MetaTrader 4-Expertenberaters. Der ursprüngliche EA wartet darauf, dass sich der Markt a bewegt
wenige Pips von einem Referenzpreis entfernt und eröffnet dann eine konträre Position. Nachfolgende Bestellungen werden in die gleiche Richtung gemittelt
immer dann, wenn der Preis um eine konfigurierbare Distanz zurückgeht. Die StockSharp-Implementierung behält das Verhalten während der Offenlegung bei
stark typisierte Parameter über das übergeordnete `Strategy` API.

## Handelslogik

1. Abonnieren Sie die besten Bid/Ask-Kurse der Stufe 1 und speichern Sie die neuesten Werte im Cache.
2. Wenn keine Geschäfte offen sind, speichern Sie den aktuellen Briefkurs als Referenzeinstiegsniveau. Der Handel ist nur zwischen erlaubt
`StartHour` und `EndHour` und ab `StartYear`.
3. Wenn der beste Brief um `Gap` MetaTrader Pips über die Referenz steigt, erteilen Sie einen Marktverkaufsauftrag. Wenn es um `Gap` Pips sinkt,
eine Market-Buy-Order einreichen. Durch den Aufruf werden automatisch schützende Stop-Loss- und Take-Profit-Levels festgelegt
`SetStopLoss` / `SetTakeProfit` mit den gleichen Pip-Abständen wie der Expert Advisor.
4. Sobald eine Korbrichtung festgelegt ist, führt die Strategie eine FIFO-Liste der synthetischen Positionen, um die Absicherung zu emulieren
Stil von MetaTrader. Solange die aktuelle Warenkorbgröße unter `MaxTrades` liegt, werden durchschnittliche Bestellungen hinzugefügt, wenn die beste Nachfrage besteht
Die Rendite liegt innerhalb von `ReEntryGap` Pips vom letzten Einstiegspreis.
5. Die Geldverwaltung kann entweder den festen Parameter `Lots` verwenden oder das Volumen gemäß der Regel EA dynamisch zuweisen
`floor(Equity / 20000)`, begrenzt durch `MaxLots`. Alle Volumina werden anhand der Volumenschritte/Min./Max. des Wertpapiers normalisiert.
6. Aktualisierungen außerhalb der Geschäftszeiten setzen die internen Einstiegsanker zurück, um falsche Trades zu vermeiden, wenn die nächste Sitzung beginnt.

## Parameter

| Name | Beschreibung |
|------|-------------|
| `Lots` | Das Auftragsvolumen wurde behoben, wenn die Geldverwaltung deaktiviert ist. |
| `StopLoss` | Schutzstoppdistanz in MetaTrader Pips. Auf `0` setzen, um den Stopp zu deaktivieren. |
| `TakeProfit` | Gewinnzielentfernung in MetaTrader Pips. Auf `0` setzen, um das Ziel zu deaktivieren. |
| `Gap` | Abstand in MetaTrader Pips, um den sich der Brief von der Referenz entfernen muss, bevor der erste Handel eröffnet wird. |
| `MaxTrades` | Maximale Anzahl gleichzeitig geöffneter Trades (Korbgröße). |
| `ReEntryGap` | Abstand in MetaTrader Pips, der durchschnittliche Orders in Korbrichtung auslöst. |
| `MoneyManagement` | Aktiviert die dynamische Lautstärkeregel `floor(Equity / 20000)`, wenn sie auf `true` eingestellt ist. |
| `MaxLots` | Obergrenze für das dynamisch berechnete Volumen. |
| `StartHour` / `EndHour` | Handelsfenster in Börsenserverzeit (einschließlich). |
| `StartYear` | Kalenderjahr, ab dem der Handel erlaubt ist. Frühere Daten werden ignoriert. |

## Notizen

- Die Strategie verbraucht nur Level1-Daten und fordert keine Kerzen an. Es ist daher leicht und reagiert sofort
Zitatänderungen, genau wie der MT4-Tick-Handler `start()`.
- Stopps und Ziele stützen sich auf die Hilfsmethoden von `Strategy`, um MetaTrader Pip-Abstände in Broker-spezifische zu übersetzen
Preisniveaus. Stellen Sie sicher, dass der verbundene Veranstaltungsort die korrekten Werte für `PriceStep`, `StepPrice` und `VolumeStep` bereitstellt.
- Der synthetische Korbzähler ermöglicht es der Strategie, Sicherungskonten nachzuahmen, obwohl StockSharp die Position aggregiert.
Teilfüllungen und Stopptreffer werden über den Callback `OnPositionChanged` verarbeitet, der die FIFO-Warteschlangen verwaltet.
- Gemäß den Repository-Richtlinien wird bewusst auf eine Python-Implementierung verzichtet.
