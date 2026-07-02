# MA Reverse-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die MA Reverse Strategy ist eine StockSharp-Umwandlung des einfachen MetaTrader 4 Expert Advisors „MA_Reverse“. Der ursprüngliche Roboter
überwacht, wie lange der Gebotspreis über oder unter einem einfachen gleitenden 14-Perioden-Durchschnitt (SMA) bleibt. Nach einer ausreichend langen Serie in einem
Richtung eröffnet es eine Position, die auf eine kurzfristige Umkehr setzt. Der StockSharp-Port behält die gleiche Idee bei, indem er die Anzahl zählt
von aufeinanderfolgenden fertigen Kerzen, die über oder unter SMA schließen und eine Marktorder ausführen, sobald der konfigurierte Schwellenwert erreicht ist
erreicht.

## Handelslogik
- Abonnieren Sie Kerzen des ausgewählten Zeitrahmens und berechnen Sie einen einfachen gleitenden Durchschnitt mit dem durch `SmaPeriod` definierten Zeitraum.
- Pflegen Sie einen ganzzahligen Zähler (`StreakThreshold` steuert die Ziellänge), der sich erhöht, während der Kerzenschluss darüber bleibt
der gleitende Durchschnitt und sinkt, während der Schlusskurs darunter bleibt. Durch Berühren des gleitenden Durchschnitts wird der Zähler zurückgesetzt.
- Sobald der Zähler `StreakThreshold` erreicht und der Schlusskurs mindestens `MinimumDeviation` über SMA liegt, wird die Strategie mit a verkauft
Marktordnung. Es wird davon ausgegangen, dass eine längere zinsbullische Abweichung vom gleitenden Durchschnitt wahrscheinlich zu einer Rückkehr zum Mittelwert führt.
- Wenn der Zähler `-StreakThreshold` erreicht und der Schlusskurs mindestens `MinimumDeviation` unter SMA liegt, spiegelt die Logik den Wert wider
Verhalten und eröffnet eine Long-Position.
- Nach jedem Trade behält der Zähler seinen laufenden Wert, genau wie die Quelle EA, sodass er sofort mit der Messung beginnen kann
nächste Serie.

## Auftragsverwaltung
- Markteinträge verwenden den Parameter `TradeVolume`. Wenn es eine entgegengesetzte Position auf dem Buch gibt, schließt die Strategie diese zunächst und
Anschließend wird der neue Trade in einer einzelnen Marktorder eröffnet, sodass Umkehrungen dem MetaTrader-Verhalten entsprechen.
- Ein globaler Take-Profit wird über den `StartProtection`-Helfer von StockSharp konfiguriert. Die Entfernung beträgt `TakeProfitPoints`
multipliziert mit der Wertpapierpreisstufe, was das Gewinnziel „30 * Punkte“ aus dem MQL-Code reproduziert. Wenn das Ziel getroffen wird
Die Position wird mit einer Marktorder geschlossen.
- Im ursprünglichen Experten ist kein Stop-Loss implementiert und daher wird im Port auch kein Stop-Loss hinzugefügt. Die Risikokontrolle liegt vollständig bei
vom Take-Profit und von den Geldverwaltungseinstellungen des Benutzers.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Für jeden Markteintritt verwendete Losgröße. Der Wert wird auch zur Dimensionierung von Umkehrungen beim Richtungswechsel verwendet. |
| `SmaPeriod` | Anzahl der Kerzen, die vom einfachen gleitenden Durchschnitt verwendet werden. Der Standard entspricht dem gleitenden 14-Perioden-Durchschnitt von EA. |
| `StreakThreshold` | Anzahl aufeinanderfolgender Schließungen, die auf einer Seite des SMA bleiben müssen, bevor eine Umkehrorder zulässig ist. |
| `MinimumDeviation` | Minimaler absoluter Abstand zwischen dem Schlusskurs und dem SMA, der die Ausbruchsbedingung bestätigt. |
| `TakeProfitPoints` | Take-Profit-Distanz, ausgedrückt in Preisschritten. Er wird mit dem `PriceStep` des Instruments multipliziert, um den absoluten Preisversatz zu erhalten. |
| `CandleType` | Kerzentyp (Zeitrahmen), der zur Berechnung des SMA und zur Auswertung der Streak-Zähler verwendet wird. |

## Notizen
- Die Zählerlogik funktioniert mit fertigen Kerzen, die von `SubscribeCandles` bereitgestellt werden, was die Implementierung robust und robust macht
kompatibel mit historischen Tests. Das Verhalten entspricht der Tick-basierten MetaTrader-Version, solange die Kerzen in Ordnung sind
körnig genug, um kurzfristige Ausflüge festzuhalten.
- Da StockSharp standardmäßig Positionen aggregiert, werden mehrere aufeinanderfolgende Einträge als eine einzelne Position mit einem einzigen verwaltet
Floating Take-Profit-Distanz. Dies ist gleichbedeutend damit, dass MetaTrader bei jeder Bestellung den gleichen Take-Profit erzielt, weil die
Der Abstand zum aktuellen durchschnittlichen Einstiegspreis bleibt konstant.
- Die Strategie fügt `Strategy.Indicators` keinen eigenen Indikator hinzu, da die Bindungsinfrastruktur den Indikator verwaltet
Lebenszeit automatisch.
- Überprüfen Sie immer die Preisschritt- und Volumeneinstellungen für Ihre spezifischen Brokersymbole, damit der Parameter `TakeProfitPoints`
ergibt die gewünschte absolute Zielgröße.
