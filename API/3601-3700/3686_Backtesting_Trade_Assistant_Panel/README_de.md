# Backtesting der Trade Assistant Panel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Backtesting Trade Assistant Panel-Strategie** ist ein manueller Helfer, der aus dem MetaTrader 4-Expertenberater *Backtesting Trade Assistant Panel V1.10* konvertiert wurde. Das ursprüngliche Skript erstellte ein grafisches Bedienfeld im Tester, mit dem der Bediener die Losgröße, Stop-Loss- und Take-Profit-Abstände ändern und sofort KAUF- oder VERKAUF-Marktaufträge erteilen konnte. Der StockSharp-Port bietet den gleichen Workflow innerhalb einer Strategiekomponente, indem er stark typisierte Parameter und öffentliche Hilfsmethoden anstelle von Widgets auf dem Diagramm verfügbar macht.

Hauptfunktionen:

- Behalten Sie ein konfigurierbares Auftragsvolumen zusammen mit Stop-Loss- und Take-Profit-Abständen im MetaTrader-Stil bei (gemessen in „Punkten“).
- Erteilen Sie bei Bedarf Long- oder Short-Market-Orders über die Helfer `ManualBuy()` und `ManualSell()`.
- Fügen Sie nach jeder manuellen Eingabe mithilfe der konvertierten Punktwerte automatisch Stop-Loss- und Take-Profit-Offsets hinzu.
- Stellen Sie Hilfsmethoden bereit, die das Handelsvolumen und die Risikodistanzen zur Laufzeit aktualisieren und dabei die bearbeitbaren Textfelder des MT4-Panels nachahmen.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `OrderVolume` | Volumen in Lots, das auf manuelle Marktaufträge angewendet wird. Durch Ändern des Werts wird auch die Basis `Strategy.Volume` aktualisiert. | `0.1` |
| `StopLossPips` | Abstand vom Füllpreis bis zum Schutzstopp, ausgedrückt in MetaTrader Punkten. Auf `0` setzen, um die automatische Stop-Loss-Platzierung zu deaktivieren. | `50` |
| `TakeProfitPips` | Abstand vom Füllpreis zum Gewinnziel, ausgedrückt in MetaTrader Punkten. Auf `0` setzen, um die automatische Take-Profit-Platzierung zu deaktivieren. | `100` |
| `MagicNumber` | Vom ursprünglichen EA für die Buchhaltung beibehaltener Bezeichner. Es wird nicht direkt von der StockSharp-Ausführungslogik verwendet, kann aber in benutzerdefinierten Erweiterungen referenziert werden. | `99` |

## Manuelle Operationen
Das Original EA basierte auf anklickbaren Schaltflächen. In StockSharp stehen dieselben Aktionen als öffentliche Methoden zur Verfügung:

- `SetOrderVolume(decimal volume)` – synchronisiert den Parameter `OrderVolume` und den internen Wert `Strategy.Volume`.
- `SetStopLoss(decimal pips)` / `SetTakeProfit(decimal pips)` – Passen Sie die Schutzabstände an, während die Strategie ausgeführt wird. Werte werden in MetaTrader Punkten interpretiert, genau wie die MT4-Textfelder.
- `ManualBuy()` – sendet eine Marktkauforder mit dem aktuellen Volumen. Nach der Ausführung wandelt die Strategie die konfigurierten Stop-Loss- und Take-Profit-Punkte in Preisversätze um (unter Verwendung von Symbolmetadaten) und ruft `SetStopLoss`/`SetTakeProfit` auf, um Schutzaufträge für die resultierende Nettoposition zu registrieren.
- `ManualSell()` – symmetrischer Helfer für Marktverkaufsaufträge.
- `CloseAllPositions()` – schließt das gesamte Engagement zum Marktpreis. Dies spiegelt den Arbeitsablauf wider, bei dem der Tester Positionen manuell reduzieren konnte.

Alle Schutzabstände werden mit der gleichen Pip-Größen-Heuristik wie in MT4 umgerechnet: Fünf- und dreistellige Symbole multiplizieren `PriceStep` mit zehn, um einen einzelnen „Punkt“ zu erhalten, während andere Symbole auf dem rohen `PriceStep` basieren. Wenn Marktdaten nicht die erforderlichen Metadaten bereitstellen, wird eine Fallback-Größe von `0.0001` verwendet, um ein konsistentes Verhalten aufrechtzuerhalten.

## Verhaltensnotizen
- Die Strategie abonniert Level1-Updates, um den besten Geld-/Briefkurs im Auge zu behalten. Wenn diese Preise nicht verfügbar sind, wird auf den letzten Handelspreis zurückgegriffen, bevor Schutzausgleiche vorgenommen werden.
- Es werden keine automatischen Handelssignale generiert – dieses Modul fungiert genau wie das MT4-Panel ausschließlich als manueller Ausführungsassistent.
- Da StockSharp Schutzanordnungen nativ verwaltet, ist keine explizite magische Zahl erforderlich. Das Feld ist lediglich aus Gründen der Parität mit dem Quell-Expert Advisor enthalten.
- Stop-Loss- und Take-Profit-Abstände können jederzeit vor dem Auslösen von `ManualBuy()`/`ManualSell()` angepasst werden, um die Bearbeitung der MT4-Textfelder vor dem Drücken der Tasten zu emulieren.

## Unterschiede zum Original EA
- Die MetaTrader-Benutzeroberfläche wird durch Strategieparameter und Methodenaufrufe ersetzt. Alle Funktionen stehen programmgesteuert zur Verfügung, ohne dass Diagrammsteuerelemente gerendert werden müssen.
- Die Slippage-Behandlung aus dem MT4-Aufruf `OrderSend` (festgelegt auf 50 Punkte) wird nicht reproduziert, da die `BuyMarket`/`SellMarket`-Helfer von StockSharp kein direktes Slippage-Argument offenlegen. Die Umgebung sollte bei Bedarf die Ausführungstoleranz verwalten.
- Schutzanordnungen werden mit den übergeordneten `SetStopLoss`/`SetTakeProfit`-Helfern von StockSharp statt mit direkten `OrderSend`-Aufrufen erstellt, wodurch die Implementierung im Einklang mit den StockSharp-Konventionen bleibt.

## Anwendungstipps
1. Konfigurieren Sie wie gewohnt das gewünschte Symbol, Portfolio und Connector in StockSharp und starten Sie dann die Strategie.
2. Passen Sie `OrderVolume`, `StopLossPips` und `TakeProfitPips` über das Parameterraster oder die bereitgestellten Setter-Methoden an.
3. Rufen Sie `ManualBuy()` oder `ManualSell()` an, wenn eine diskretionäre Eingabe erforderlich ist. Der Helfer fügt die angeforderten Schutzanordnungen automatisch an.
4. Verwenden Sie `CloseAllPositions()`, um das Risiko während Backtests oder diskretionären Live-Handelssitzungen sofort zu reduzieren.
