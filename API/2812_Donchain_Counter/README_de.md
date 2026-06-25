# Donchain-Zähler-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Donchain-Zähler-Strategie ist eine StockSharp-Portierung des MQL5-Expertenberaters „Donchain counter" von Michal Rutka. Das System beobachtet, wie sich der Donchian-Kanal ausdehnt, um Ausbrüche zu erkennen, und verteidigt dann die Position, indem es den Stop entlang der gegenüberliegenden Band nachzieht, sobald der Preis eine feste Distanz zurückgelegt hat. Es kann nur eine Position alle 24 Stunden eröffnet werden, was die ursprüngliche Einschränkung widerspiegelt.

## Handelslogik
### Long-Einstiege
- Signale werden auf abgeschlossenen Kerzen des konfigurierten Zeitrahmens ausgewertet (Standard **H1**).
- Die obere Donchian-Band der letzten zwei geschlossenen Bars wird beobachtet. Wenn die Band bei Bar *t-1* höher ist als bei *t-2* (ein frischer Ausbruch über das Kanalhoch), wird eine Long-Market-Order platziert.
- Der anfängliche Schutz-Stop wird an der aktuellen unteren Donchian-Band verankert.

### Short-Einstiege
- Die untere Donchian-Band der letzten zwei geschlossenen Bars wird überwacht. Wenn die Band bei Bar *t-1* niedriger ist als bei *t-2* (ein Ausbruch unter das Kanaltief), wird eine Short-Market-Order übermittelt.
- Das erste Stop-Niveau wird auf die aktuelle obere Donchian-Band gesetzt.

### Handels-Abkühlzeit
- Nach jedem neuen Einstieg zeichnet der Algorithmus die Ausführungszeit auf und blockiert weitere Einstiege für die Dauer von `TradeCooldown` (Standard **24 Stunden**). Dies reproduziert die Regel „nur ein Trade pro Tag" der MQL-Version.

### Trailing- und Ausstiegsregeln
- Ein Trailing-Mechanismus greift erst, wenn der Preis mindestens `BufferSteps` Preisschritte über die gegenüberliegende Donchian-Band hinaus vorrückt. Dies repliziert die Anforderung des ursprünglichen EA, bei der sich der Markt 50 Punkte bewegen muss, bevor der Stop enger gesetzt wird.
- Long-Positionen: Sobald der Trailing-Trigger ausgelöst wird, wird der Stop auf die aktuelle untere Band aktualisiert. Wenn das Tief der Kerze dieses Niveau berührt, steigt die Strategie mit einer Market-Order aus.
- Short-Positionen: Nach dem Trigger folgt der Stop der aktuellen oberen Band. Wenn das Hoch der Kerze diesen Preis erreicht, wird die Position geschlossen.
- Wenn ein Trailing-Stop einen Ausstieg erzwingt, öffnet die Strategie erst eine neue Position, wenn das nächste Signal und die Abkühlzeit es erlauben.

### Risikoverwaltung
- Die Strategie handelt immer eine einzelne Position, deren Größe durch den Parameter `Volume` definiert ist.
- Es gibt kein Gewinnziel; alle Ausstiege werden durch die Donchian-Trailing-Logik gesteuert.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Volume` | Ordergröße für Einstiege. | `1` |
| `ChannelPeriod` | Rückblickperiode für die Donchian-Kanal-Berechnung. | `20` |
| `BufferSteps` | Anzahl der Preisschritte, um die der Preis über die gegenüberliegende Band hinausgehen muss, bevor das Trailing aktiviert wird (MQL verwendete 50 Punkte). | `50` |
| `TradeCooldown` | Mindestzeit zwischen neuen Einstiegen. | `1 Tag` |
| `CandleType` | Kerzenserie für den Indikator (Standard 1-Stunden-Kerzen). | `1h-Kerzen` |

## Indikatoren
- **Donchian-Kanäle** – die obere und untere Band definieren Ausbruchssignale und dynamische Stops.

## Hinweise
- Verwenden Sie Instrumente mit einem sinnvollen `PriceStep`, damit der Buffer in eine realistische Preisdistanz übersetzt wird. Die Strategie verwendet standardmäßig einen Schritt von 0.0001, wenn keiner vom Instrument angegeben wird.
- Es ist immer nur eine Richtung offen. Vor einem Richtungswechsel muss die bestehende Position vollständig geschlossen sein, genau wie beim ursprünglichen Expertenberater.
- Chart-Objekte werden automatisch vorbereitet, wenn ein Chart-Bereich verfügbar ist: Kerzen, der Donchian-Kanal und die eigenen Trades der Strategie.
