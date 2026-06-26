# XD Bereichswechsel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die XD Bereichswechsel-Strategie recreiert den MetaTrader 5 Expert Advisor **Exp_XD-RangeSwitch** mithilfe der StockSharp High-Level-API. Sie basiert auf dem benutzerdefinierten XD-RangeSwitch-Kanalindikator, der abwechselnd obere und untere Bänder zusammen mit Pfeilen zeichnet, sobald das dominante Band wechselt. Die Strategie kann entweder diese Pfeile ausblenden (Gegentrend-Verhalten) oder in Richtung des Ausbruchs handeln, abhängig vom `TradeDirection`-Parameter. Die Positionsgröße folgt der Basis-`Strategy.Volume`-Einstellung, während die ursprünglichen Money-Management-Formeln durch StockSharp-Positionsverwaltungshelfer ersetzt werden.

## Nachbildung des XD-RangeSwitch-Indikators
* Der Indikator verfolgt die letzten `Peaks` abgeschlossenen Kerzen, um die höchsten Hoch- und tiefsten Tief-Bereiche zu bestimmen.
* Ein bullishes Kanal (unteres Band) wird gedruckt, wenn das aktuelle Schlusskurs über dem höchsten Hoch der vorherigen `Peaks` Kerzen liegt. Sein Wert entspricht dem minimalen Tief im selben Fenster plus der aktuellen Kerze.
* Ein bearishes Kanal (oberes Band) wird gedruckt, wenn das aktuelle Schlusskurs unter dem tiefsten Tief der vorherigen `Peaks` Kerzen liegt. Sein Wert entspricht dem maximalen Hoch im selben Fenster plus der aktuellen Kerze.
* Wenn kein Ausbruch auftritt, werden die vorherigen Kanalwerte vorwärtspropagiert.
* Immer wenn ein Kanal nach einer Leerstelle wieder erscheint, zeichnet die Strategie ein Pfeilsignal am Kanalpreis auf. Dies spiegelt das Verhalten der MT5-Puffer 2 und 3 wider, die vom ursprünglichen Expert verwendet werden.
* Es werden nur vollständig abgeschlossene Kerzen verarbeitet, was konsistente Werte bei Live- und historischen Läufen sicherstellt.

## Handelslogik
1. Die Strategie verarbeitet Kerzen vom durch `CandleType` gewählten Zeitrahmen und speichert die rekonstruierten Indikatorbuffer.
2. Für jede neue Kerze untersucht sie den Indikatorwert, der `SignalBar` Kerzen alt ist (der MT5-Code verwendet denselben Versatz beim Aufruf von `CopyBuffer`).
3. Die Signalzuordnung hängt von der `TradeDirection`-Option ab:
   * **AgainstSignal** repliziert das Standard-MT5-Verhalten — bullishe Pfeile lösen Longs aus und fordern auch den Abschluss von Short-Trades, bearishe Pfeile lösen Shorts aus und fordern den Abschluss von Longs.
   * **WithSignal** dreht die Interpretation um, sodass bullishe Pfeile als Ausstiegspunkte für Longs und Einstiegspunkte für Shorts behandelt werden, was effektiv in derselben Richtung wie der Kanalausbruch handelt.
4. Trendbuffer ohne Pfeile werden weiterhin als Ausstiegssignale respektiert, was den ursprünglichen `SELL_Close`- und `BUY_Close`-Flags entspricht.
5. Schließungen werden immer vor Eröffnungen ausgeführt, sodass die Strategie eine entgegengesetzte Position ausgleichen kann, bevor sie in die neue Richtung einsteigt.
6. Aufträge werden mit Market-Execution-Helpern eingereicht (`BuyMarket`/`SellMarket`). Wenn ein Wechsel auftritt, während eine entgegengesetzte Position offen ist, wird die angeforderte Menge automatisch erhöht, um das Exposure vollständig auszugleichen, bevor die neue Position eingegangen wird.

## Risikomanagement
* Optionale Stop-Loss- und Take-Profit-Logik wird durch die `UseStopLoss`/`StopLossPoints`- und `UseTakeProfit`/`TakeProfitPoints`-Parameter bereitgestellt.
* Die Abstände werden in absoluten Preiseinheiten gemessen und spiegeln die "Punkte"-Eingaben im MT5-Skript wider.
* Stops und Ziele werden bei jeder abgeschlossenen Kerze unter Verwendung des Hoch/Tief der Kerze evaluiert, um die Intra-Bar-Auslösung zu emulieren.
* Wenn sowohl ein Stop als auch ein Ziel aktiv sind, hat der Stop Priorität — die Position wird geschlossen, sobald eines der Levels erreicht wird.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | H4-Kerzen | Zeitrahmen für die XD-RangeSwitch-Berechnungen. |
| `Peaks` | 4 | Anzahl der Peaks (Lookback-Länge), die vom Indikator analysiert werden. |
| `SignalBar` | 1 | Anzahl abgeschlossener Balken zurück beim Lesen von Indikatorbuffern. |
| `TradeDirection` | AgainstSignal | Wahl zwischen Gegentrend- oder Trendfolge-Interpretation der Signale. |
| `AllowBuyEntry` / `AllowSellEntry` | true | Neue Positionen in der entsprechenden Richtung aktivieren oder deaktivieren. |
| `AllowBuyExit` / `AllowSellExit` | true | Der Strategie erlauben, bestehende Positionen zu schließen, wenn der Indikator es anfordert. |
| `UseStopLoss` / `StopLossPoints` | true / 1000 | Stop-Loss-Behandlung aktivieren und Distanz in Preiseinheiten festlegen. |
| `UseTakeProfit` / `TakeProfitPoints` | true / 2000 | Take-Profit-Behandlung aktivieren und Distanz in Preiseinheiten festlegen. |

## Hinweise
* Die Hoch/Tief-Buffer werden intern innerhalb der Strategie gepflegt, anstatt sich auf StockSharp-Kollektionen zu verlassen, was der MT5-Implementierung treu bleibt und gleichzeitig den Konvertierungsrichtlinien entspricht.
* Signale werden nur bei abgeschlossenen Kerzen ausgewertet. Wenn `SignalBar` größer als null ist, wird die Order bei der nächsten Kerze nach der platziert, die das Signal erzeugt hat, wie im MT5-Expert.
* Die Indikatorwerte werden in einer kurzen rollierenden Historie gespeichert, die nur etwas über das Maximum von `Peaks` und `SignalBar` hinausgeht, was eine deterministische Speichernutzung auch bei langen Simulationen sicherstellt.
* Die Standardkonfiguration spiegelt die MT5-Standardwerte wider: H4-Kerzen, `Peaks = 4`, `SignalBar = 1`, Gegentrend-Handel und ein 1.000/2.000-Punkte-Risikorahmen.
