# Volume Trader V2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Volume Trader V2 ist eine direkte Konvertierung des MetaTrader Expert Advisors `Volume_trader_v2_www_forex-instruments_info.mq4`. Das ursprüngliche System beobachtet, wie sich das Gesamtvolumen der letzten Kerzen entwickelt, und nutzt diesen kurzfristigen Fluss, um zu entscheiden, ob ein einfaches Long- oder Short-Engagement aktiv sein soll. Der StockSharp-Port behält das Verhalten bei jeweils einer Position, den Tageszeitfilter und die Anforderung bei, nur einmal pro abgeschlossener Kerze zu handeln.

Die Strategie abonniert eine konfigurierbare Kerzenserie und speichert das Volumen der letzten beiden fertigen Kerzen zwischen. Wenn ein neuer Balken schließt, werden die Volumina der beiden vorherigen Balken (MetaTraders `Volume[1]` und `Volume[2]`) verglichen und eine aktualisierte Handelsrichtung erstellt:

- `Volume[1] < Volume[2]` erzeugt eine **lange** Tendenz.
- `Volume[1] > Volume[2]` erzeugt eine **kurze** Tendenz.
- Bei gleichen Volumina oder deaktivierten Handelszeiten werden alle offenen Positionen entfernt.

Vor dem Senden einer neuen Bestellung wird die aktuelle Position abgeflacht, wenn sie in die entgegengesetzte Richtung zeigt, sodass die StockSharp-Implementierung mit dem MetaTrader-Bestelllebenszyklus übereinstimmt.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | Zeitrahmen von 5 Minuten | Von `SubscribeCandles` angeforderter Datentyp. Stellen Sie es so ein, dass es dem in MetaTrader verwendeten Diagrammzeitraum entspricht. |
| `StartHour` | 8 | Erste Handelsstunde (einschließlich). Signale außerhalb des Fensters werden ignoriert und jede Position wird geschlossen. |
| `EndHour` | 20 | Letzte Handelsstunde (einschließlich). Wenn die aktuelle Kerze nach dieser Stunde beginnt, bleibt die Strategie flach. |
| `TradeVolume` | 0,1 | Losgröße, repliziert aus EA. Der Wert wird auch `Strategy.Volume` zugewiesen, sodass Hilfsmethoden denselben Betrag verwenden. |

Alle Parameter sind reguläre `StrategyParam<T>`-Instanzen, sodass sie über die Benutzeroberfläche optimiert oder verfügbar gemacht werden können.

## Handelslogik
1. Behandeln Sie nur fertige Kerzen, um die Bar-für-Bar-Parität mit dem EA zu gewährleisten.
2. Zwischenspeichern Sie `Volume[1]`- und `Volume[2]`-Äquivalente in `_previousVolume` und `_twoBarsAgoVolume`, bevor Sie ein Signal auswerten.
3. Stellen Sie sicher, dass die Startzeit der Kerze zwischen `StartHour` und `EndHour` (einschließlich) liegt. Außerhalb dieses Bereichs wird jede aktive Position geschlossen und es werden keine neuen Aufträge erstellt.
4. Berechnen Sie die gewünschte Richtung:
   - Long, wenn das letzte Volumen niedriger ist als der vorherige Balken.
   - Short, wenn das letzte Volumen höher ist als der vorherige Balken.
   - Ansonsten neutral.
5. Wenn die gewünschte Richtung von der aktuellen Position abweicht, schließen Sie zuerst die entgegengesetzte Position (`BuyMarket(-Position)` oder `SellMarket(Position)`).
6. Geben Sie die neue Position nur dann mit dem konfigurierten `TradeVolume` ein, wenn die Strategie flach ist oder in die entgegengesetzte Richtung positioniert ist.
7. Aktualisieren Sie die zwischengespeicherten Volumina, damit im nächsten Zyklus immer noch die beiden letzten abgeschlossenen Kerzen verglichen werden.

Dieser Ablauf garantiert, dass keine Orders platziert werden, während sich eine Kerze noch aufbaut, und dass die StockSharp-Strategie genau einmal pro Balken reagiert, genau wie die MetaTrader-Implementierung, die auf `LastBarChecked` basierte.

## Zusätzliche Hinweise
- `StartProtection()` wird in `OnStarted` aufgerufen, um den Framework-Schutzhelfer wiederzuverwenden, der die aktuelle Position verfolgt.
- Die Eigenschaft `Comment` spiegelt die EA-Diagnosemeldungen (`"Up trend"`, `"Down trend"`, `"No trend..."` oder `"Trading paused"`) wider, um die Überwachung zu vereinfachen.
- Die Strategie verwaltet keine zusätzlichen Sammlungen und nutzt das High-Level-Kerzenabonnement API gemäß den Projektrichtlinien.
- Stellen Sie Kerzentyp, Sicherheit und Volumen so ein, dass sie mit dem ursprünglich in MetaTrader verwendeten Instrument und Zeitrahmen übereinstimmen, um vergleichbare Ergebnisse zu erzielen.
