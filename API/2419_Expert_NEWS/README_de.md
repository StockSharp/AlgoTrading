# Expert NEWS-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Expert NEWS-Strategie ist eine direkte Portierung des MQL5-Roboters "Expert_NEWS". Die Strategie platziert kontinuierlich symmetrische Stop-Orders ober- und unterhalb des aktuellen Marktpreises und verwaltet die resultierenden Positionen mit Break-Even-Schutz, Trailing Stops und geplanten Aktualisierungen ausstehender Orders. Die Implementierung basiert auf Level1-Quotes und hält das Standard-Handelsvolumen bei 0.1 Lots.

## Handelslogik
1. **Quote-Abonnement** – die Strategie lauscht auf beste Bid/Ask-Updates und berechnet Order-Preise aus den neuesten Werten.
2. **Initiale Stop-Orders** – wenn keine Long-Position oder kein Buy Stop vorhanden ist, wird ein neuer Buy Stop bei `ask + EntryOffsetTicks * PriceStep` platziert. Wenn keine Short-Position oder kein Sell Stop vorhanden ist, wird ein Sell Stop bei `bid - EntryOffsetTicks * PriceStep` platziert.
3. **Order-Aktualisierung** – alle `OrderRefreshSeconds` storniert die Strategie einen ausstehenden Stop und erstellt ihn neu, wenn der erforderliche Preis um mehr als `TrailingStepTicks` Ticks abweicht.
4. **Positionsschutz** – nach einer Ausführung öffnet die Strategie schützende Stop- und Take-Profit-Orders, wenn die angeforderten Abstände die `MinimumStopTicks`-Bedingung erfüllen.
5. **Break-Even-Kontrolle** – wenn `UseBreakEven` aktiviert ist, wird der Stop auf `Einstieg ± BreakEvenProfitTicks` gezogen, sobald sich der Markt weit genug bewegt und der neue Stop den Mindestabstand zum aktuellen Quote respektiert.
6. **Trailing Stop** – sobald der Preis um `TrailingStartTicks` vorschreitet, folgt der Stop mit `TrailingStopTicks` als Abstand und `TrailingStepTicks` als minimaler Verbesserungsschritt.
7. **Bereinigung** – das Schließen der Position storniert alle verbleibenden Schutz-Orders.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `StopLossTicks` | Initialer Schutz-Stop-Abstand (Ticks). Auf null setzen, um die initiale Stop-Order zu deaktivieren. |
| `TakeProfitTicks` | Initialer Take-Profit-Abstand (Ticks). Auf null setzen, um die Ziel-Order zu deaktivieren. |
| `TrailingStopTicks` | Abstand des Trailing Stops (Ticks). |
| `TrailingStartTicks` | Gewinn in Ticks, der erforderlich ist, bevor die Trailing-Logik aktiviert wird. |
| `TrailingStepTicks` | Mindestverbesserung beim Aktualisieren des Trailing Stops oder der ausstehenden Einstiegs-Orders. |
| `UseBreakEven` | Aktiviert die Break-Even-Verschiebung des Stops, sobald genügend Gewinn vorhanden ist. |
| `BreakEvenProfitTicks` | Zusätzliches Gewinnpolster beim Verschieben des Stops auf Break-Even. |
| `EntryOffsetTicks` | Abstand zwischen aktuellem Quote und jeder neuen Stop-Einstiegs-Order. |
| `OrderRefreshSeconds` | Zeitintervall zwischen automatischen Aktualisierungsversuchen für ausstehende Stop-Orders. |
| `MinimumStopTicks` | Manueller Fallback für die Stop-Level-Anforderung des Brokers. Stops, die näher als dieser Abstand sind, werden nicht übermittelt. |

## Positionsverwaltung
- Schutz-Orders entsprechen immer dem Netto-Positionsvolumen. Teilausführungen skalieren die Stop- und Take-Profit-Orders automatisch.
- Break-Even- und Trailing-Logik funktionieren auch wenn der initiale Stop deaktiviert ist; der Stop wird dynamisch erstellt, sobald die Regeln erfüllt sind.
- Die Strategie speichert den letzten Stop-Preis im Speicher, sodass Trailing-Updates ein monotones Verhalten gewährleisten.

## Verwendungshinweise
- Stellen Sie sicher, dass `Security.PriceStep` konfiguriert ist; jeder Tick-Abstandsparameter wird mit diesem Wert multipliziert.
- Das Standard-Volumen beträgt `0.1`, um den ursprünglichen Roboter zu spiegeln. Passen Sie die `Volume`-Eigenschaft an, wenn eine andere Größe erforderlich ist.
- `MinimumStopTicks` sollte auf die Stop-Level-Anforderung des Handelsplatzes gesetzt werden, wenn dieser eine vorschreibt. Lassen Sie es auf null, um die engstmöglichen Stops zu erlauben.
- Der Algorithmus ist nicht auf historische Bars angewiesen und kann nur mit Streaming-Quotes betrieben werden.
