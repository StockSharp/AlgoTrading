# Strategie für mehrere Währungsvorlagen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Multi-Currency-Template-Strategie** ist eine Konvertierung des MetaTrader 4 Expertenberaters *Multi-Currency-Template v4*. Es reproduziert die ursprüngliche EMA-Crossover-Eintrittslogik zusammen mit der Mittelung im Martingal-Stil, Pip-basierten Schutzstufen und Trailing-Management unter Verwendung der StockSharp-Hochebene API. Der Standardzeitrahmen beträgt fünf Minuten bei Kerzen, er kann jedoch über einen Parameter geändert werden.

## Handelslogik
- Zwei exponentielle gleitende Durchschnitte (EMA 20 und EMA 50) werden für jede abgeschlossene Kerze des ausgewählten Zeitrahmens berechnet.
- Ein Long-Signal erscheint, wenn der schnelle EMA (20) über dem langsamen EMA (50) schließt. Ein kurzes Signal erscheint, wenn der schnelle EMA unter dem langsamen EMA schließt.
- Der Parameter `Order Method` entscheidet, ob die Strategie auf beide Signale einwirkt oder den Handel auf Long-Only- oder Short-Only-Operationen beschränkt.
- Es wird nur eine Nettoposition pro Richtung beibehalten. Wenn ein neues Signal eintrifft, schließt die Strategie alle gegenüberliegenden Positionen, bevor sie die angeforderte Seite öffnet.

## Positionsmanagement
- **Stop Loss / Take Profit** – Abstände werden in MetaTrader Pips eingegeben. Sie werden mithilfe der Wertpapierpreisstufe in Preiseinheiten umgerechnet und reproduzieren so die ursprüngliche Handhabung von 4- und 5-stelligen Forex-Symbolen.
- **Trailing Stop** – wird aktiviert, sobald sich der Preis um `Trailing Stop (pts)` zugunsten der Position bewegt, und wird nach jeder weiteren Verbesserung um `Trailing Step (pts)` verschärft.
- **Martingale Mittelwertbildung** – wenn aktiviert, werden alle `Step (pts)` zusätzliche Marktaufträge für die aktuelle Position gesendet. Jedes neue Ordervolumen wird um `Lot Multiplier` skaliert und der Vorgang wiederholt sich, bis die Position geschlossen ist.
- **Durchschnittlicher Take-Profit** – wenn zwei oder mehr durchschnittliche Orders offen sind, kann das Take-Profit-Ziel optional den gewichteten Positionspreis plus `Average TP Offset (pts)` verwenden, um das „TP-Durchschnitt“-Verhalten von MetaTrader zu emulieren.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| Bestellmethode | Handelsrichtung (Kaufen und Verkaufen, Nur Kaufen, Nur Verkaufen). | Kaufen und verkaufen |
| Volumen (Lose) | Basis-Market-Order-Größe. | 0,01 |
| Stop-Loss (Pips) | Schutzstoppdistanz in MetaTrader Pips. | 50 |
| Take-Profit (Pips) | Gewinnzielentfernung in MetaTrader Pips. | 100 |
| Trailing Stop (Punkte) | Aktivierungsschwelle für den Trailing Stop in MetaTrader Punkten. | 15 |
| Trailing Step (Punkte) | Minimale Verbesserung erforderlich, bevor der Trailing Stop verschoben wird. | 5 |
| Aktivieren Sie Martingale | Ermöglicht die Mittelwertbildung nach unten/oben bei zunehmender Lautstärke. | wahr |
| Lot-Multiplikator | Der Volumenmultiplikator wird auf jeden neuen Durchschnittsauftrag angewendet. | 1.2 |
| Schritt (Punkte) | MetaTrader Punktabstand, bevor der nächste Mittelungsauftrag erteilt wird. | 150 |
| Durchschnittlicher Take-Profit | Wechseln Sie zwischen festem und durchschnittlichem Take-Profit, wenn mehrere Aufträge vorliegen. | wahr |
| Durchschnittlicher TP-Offset (Punkte) | MetaTrader Punktversatz wird auf den durchschnittlichen Take-Profit angewendet. | 20 |
| Kerzentyp | Kerzentyp (Zeitrahmen), der für die Indikatorberechnungen verwendet wird. | 5-Minuten-Kerzen |

## Unterschiede zum ursprünglichen Expert Advisor
- StockSharp führt Nettopositionen aus, anstatt einzelne MetaTrader-Tickets zu verwalten. Das Martingale-Modul erhöht die Nettopositionsgröße, anstatt separate, ticketspezifische Ziele hinzuzufügen.
- Der Handel mit mehreren Symbolen muss durch den Start mehrerer Strategieinstanzen erreicht werden, eine pro Wertpapier. Der ursprüngliche Expert Advisor unterstützte eine integrierte Liste mit mehreren Währungen in einer EA-Instanz.
- Geldverwaltungsprüfungen (`CheckMoneyForTrade`, `CheckVolumeValue`) und Broker-spezifische Einschränkungen werden durch die Auftragsvalidierung von StockSharp ersetzt.

## Nutzungshinweise
1. Stellen Sie sicher, dass die Sicherheitsmetadaten (Preisschritt und Dezimalstellen) mit dem Instrument übereinstimmen, damit die Pip-Umrechnung korrekt bleibt.
2. Trailing Stop und Martingal-Logik wirken sich standardmäßig auf die Schlusskurse der Kerzen aus. Für ein reaktiveres Verhalten binden Sie zusätzliche Datenquellen (Quotes oder Trades) ein und rufen von dort aus die Management-Helfer an.
3. Da Marktaufträge verwendet werden, wird die Slippage-Kontrolle an den angeschlossenen Broker oder Simulator delegiert.
