# Resonanzjäger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Resonance Hunter-Strategie ist die StockSharp-Portierung des MetaTrader-Expertenberaters `Exp_ResonanceHunter`. Es überwacht drei korrelierte Währungspaare pro Slot und sucht nach synchronem Impuls in ihren Stochastic-Oszillatoren. Wenn die Oszillatoren in die gleiche Richtung schwingen, öffnet die Strategie eine Position auf dem Primärsymbol, während die Sekundär- und Bestätigungssymbole als Filter fungieren. Der Handel wird geschlossen, sobald das führende Instrument an Schwung verliert oder der konfigurierte Stop-Loss erreicht wird.

Drei Steckplätze sind vorkonfiguriert:

1. EURUSD wurde mit EURJPY und USDJPY als Bestätigungen gehandelt.
2. GBPUSD wird mit GBPJPY und USDJPY gehandelt.
3. AUDUSD wird mit AUDJPY und USDJPY gehandelt.

Jeder Slot kann unabhängig aktiviert oder deaktiviert werden und kann seinen eigenen Zeitrahmen und seine eigenen Indikatorparameter verwenden.

## Parameter
Alle Parameter sind nach Slot gruppiert (Slot 1–3). Jede Gruppe teilt die folgenden Einstellungen:

| Parameter | Beschreibung |
| --- | --- |
| `{Slot} Enabled` | Ermöglicht den Handel für den Slot. |
| `{Slot} Primary` | Instrument, das von der Strategie gehandelt und für Ausstiegssignale verwendet wird. |
| `{Slot} Secondary` | Zweites Instrument, das an der Resonanzprüfung teilnimmt. |
| `{Slot} Confirmation` | Drittes Instrument zur Resonanzprüfung. |
| `{Slot} Candle Type` | Auf alle drei Instrumente angewendeter Zeitrahmen (Standard = 1 Stunde). |
| `{Slot} K Period` | Stochastic %K Lookback. |
| `{Slot} D Period` | Glättungszeitraum für %D. |
| `{Slot} Slowing` | Zusätzliche Glättung für %K. |
| `{Slot} Volume` | Bestellvolumen in Losen. Bestehende Gegenpositionen werden saldiert. |
| `{Slot} Stop Loss` | Stop-Loss-Distanz im MetaTrader-Stil in Punkten. Auf 0 setzen, um den Schutzstopp zu deaktivieren. |

## Handelslogik
1. Für jedes konfigurierte Instrument wird auf abgeschlossene Kerzen ein `StochasticOscillator` mit den ausgewählten Parametern berechnet.
2. Sobald die letzten Kerzen der drei Instrumente die gleiche Öffnungszeit haben, werden die Unterschiede `%K - %D` ausgewertet:
   * Eine positive Differenz markiert einen Aufwärtsimpuls (`Up`), eine negative Differenz markiert einen Abwärtsimpuls (`Down`).
   * Zusätzliche Konsistenzregeln des ursprünglichen Indikators passen die Impulse an, indem sie die Größe jedes Paares vergleichen.
3. Ein **Long-Einstieg** entsteht, wenn alle drei Impulse nach oben zeigen. Ein **kurzer Einstieg** entsteht, wenn alle drei Impulse nach unten zeigen.
4. Vor der Übermittlung neuer Aufträge schließt die Strategie bestehende Positionen, wenn das Primärsymbol einen entgegengesetzten Impuls anzeigt (spiegelt die `UpStop`/`DnStop`-Puffer des Indikators wider).
5. Nach Eingabe einer Position wird ein schützender Stop-Preis anhand des letzten Schlusskurses und der Distanz `{Slot} Stop Loss` berechnet. Bei jeder neuen Primärkerze wird der Stop überprüft und bei Überschreitung wird die Position sofort geschlossen.

Bestellungen werden über `BuyMarket`/`SellMarket` weitergeleitet. Das bestehende Risiko des Primärsymbols wird verrechnet, sodass die Strategie bei Bedarf direkt umgekehrt werden kann.

## Notizen
* Die Strategie erfordert synchronisierte Kerzendaten für die drei Instrumente in jedem Slot. Wenn ein Symbol hinterherhinkt, wird das Signal verschoben, bis die Zeitstempel der Balken übereinstimmen.
* Stop-Levels werden innerhalb der Strategie emuliert (es werden keine tatsächlichen Stop-Orders gesendet), um dem MetaTrader-Verhalten zu entsprechen.
* Standardparameterwerte reproduzieren den ursprünglichen Expert Advisor, können jedoch über die `Param`-Schnittstelle optimiert werden.
