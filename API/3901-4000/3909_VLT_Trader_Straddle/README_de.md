# VLT Trader Straddle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die VLT Trader-Strategie ist eine StockSharp-Umsetzung des MetaTrader 4-Expertenberaters „VLT_TRADER“. Die ursprüngliche Idee sucht nach einem Zeitraum mit extrem geringer Volatilität und bereitet dann einen Ausbruchsstraddle um die jüngste Kerze herum vor. Wenn die zuletzt abgeschlossene Kerze im Vergleich zu einer konfigurierbaren Anzahl früherer Kerzen die kleinste Spanne aufweist, positioniert die Strategie Stop-Orders oberhalb und unterhalb dieser Kerze in Erwartung einer Volatilitätsausweitung.

## Handelslogik
- Abonnieren Sie die konfigurierte Kerzenserie und berechnen Sie die Spanne (Hoch minus Tief) für jeden Balken.
- Verfolgen Sie den Mindestbereich zwischen den vorherigen `LookbackCandles`-Balken mithilfe des `Lowest`-Indikators.
- Sobald die zuletzt abgeschlossene Kerze eine kleinere Spanne als dieses historische Minimum aufweist, bereiten Sie die Breakout-Orders für die folgende Sitzung vor.
- Platzieren Sie einen Kaufstopp über dem vorherigen Hoch plus `EntryOffsetPoints` und einen Verkaufsstopp unter dem vorherigen Tief minus dem gleichen Offset.
- Fügen Sie jeder ausstehenden Bestellung Stopps und Ziele mit fester Entfernung hinzu (`StopLossPoints` und `TakeProfitPoints`).
- Lassen Sie beide ausstehenden Bestellungen aktiv. Welche Seite zuerst auslöst, wird zu einer Marktposition, während der gegenüberliegende Stop im Buch bleibt und später aktiviert werden kann, wenn sich der Markt umkehrt.
- Wenn eine ausstehende Order ausgeführt oder storniert wird, wird die entsprechende Referenz gelöscht, sodass neue Straddles erstellt werden können, nachdem alle Positionen und Orders geschlossen wurden.

## Risikomanagement
- Die Handelsgröße wird durch `OrderVolume` gesteuert und auf den Volumenschritt und die Limits des Instruments gerundet.
- Stop-Loss- und Take-Profit-Abstände werden in Preisschritten (Punkten) ausgedrückt und mithilfe des `PriceStep` des Instruments in tatsächliche Preise umgerechnet.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` | Bei der Erstellung der ausstehenden Aufträge verwendete Losgröße. |
| `EntryOffsetPoints` | Beim Platzieren von Stop-Einträgen werden dem vorherigen Hoch/Tief zusätzliche Punkte hinzugefügt. |
| `TakeProfitPoints` | Jeder Bestellung ist eine Gewinnspanne beigefügt. |
| `StopLossPoints` | Jeder Bestellung ist eine Stop-Loss-Distanz beigefügt. |
| `LookbackCandles` | Anzahl der vorherigen Kerzen, die zur Messung der minimalen historischen Spanne verwendet wurden. |
| `CandleType` | Zeitrahmen der Kerzenserie, die die Strategie speist. |

## Notizen
- Die Strategie erfordert ein gültiges `PriceStep` auf dem Instrument; andernfalls werden keine Bestellungen aufgegeben.
- Da Stop- und Take-Profit-Level zusammen mit den ausstehenden Aufträgen übermittelt werden, können die Ausführungspreise in StockSharp je nach den Ausführungsregeln des Brokers geringfügig von MetaTrader abweichen.
- Die Implementierung basiert ausschließlich auf High-Level-APIs (`SubscribeCandles` + `Bind`) und dem Standardindikator `Lowest`, um die Volatilitätsprüfung des Originals EA widerzuspiegeln.
