# Strategiediagramm für CCI-Rückkehr mit Pending Orders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm handelt die Rückkehr des CCI aus Extremzonen mit kurzlebigen Limitorders. Der ausführbare Preis kommt vom Schlusskurs statt von Level-1-Bid oder -Ask, sodass der Lebenszyklus ohne diese Quote-Felder reproduzierbar bleibt.

![schema](schema.svg)

## Strategieübersicht

- Stundenkerzen speisen CCI(30); Previous value trennt die Rückkehr über −100 oder unter +100 vom Verbleib in der Extremzone.
- Positionsrichtung, vier Kerzen Pause nach einer Ausführung und ein globaler Pending-Latch filtern beide Seiten.
- Order registering stellt genau eine Limitorder zum Schlusskurs ohne Preisrundung ein.
- Die registrierte Order startet N values, Kerzen zählen weiter, und nach vier entfernt Order cancellation eine unerfüllte Order.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Vorheriger CCI <= −100, aktueller CCI > −100, Position nicht long, Pause beendet und keine Pending Order: Kauflimit zum Schluss.
- **Short-Einstieg**: Vorheriger CCI >= +100, aktueller CCI < +100, Position nicht short, Pause beendet und keine Pending Order: Verkaufslimit zum Schluss.
- **Ausstieg**: Es gibt keinen festen Stop oder Zielwert. Eine entgegengesetzte CCI-Rückkehr kann Position durch eine Gegenorder Richtung null führen; eine unerfüllte Order wird nach Ablauf storniert.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Time Frame | 01:00:00 | Kerzenzeitrahmen für CCI, Pause, Orderpreis und Lebensdauer. |
| CCI Length | 30 | Anzahl der Werte im CommodityChannelIndex. |
| CCI Level | 100 | Symmetrische Extremgrenze als +Level und −Level. |
| Signal Cooldown, candles | 4 | Fertige Kerzen nach der letzten Ausführung bis zur nächsten Order. |
| Order Volume | 1 | Menge jeder Pending-Limitorder. |
| Pending Lifetime, candles | 4 | Maximale fertige Kerzen für eine unerfüllte aktive Order. |

## Diagrammdetails

- Der Ordner behält seinen Galerienamen, aber Level 1 ist nicht verbunden: close ist der explizite, replayfähige Pending-Preis.
- Eine registrierte Order setzt den Pending-Status; Finished löscht ihn bei Ausführung, Storno oder Fehler.
- Pause und Orderlebensdauer sind getrennt: die erste misst nach einer Ausführung, die zweite begrenzt eine unerfüllte Order.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
