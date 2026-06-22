# IStochastic Handelsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die IStochastic Handelsstrategie ist ein direkter StockSharp-Port des MetaTrader 5 Expert Advisors "IStochastic_Trading". Der Bot verwendet den Stochastischen Oszillator, um überkaufte und überverkaufte Bedingungen zu erkennen und erstellt dann eine Martingale-ähnliche Positionsleiter, während er jeden Einstieg mit Stop-Loss, Take-Profit und einem Trailing-Stop verwaltet. Die Implementierung arbeitet auf abgeschlossenen Kerzen, die über die High-Level-API von StockSharp bezogen werden, und verwendet ausschließlich Market-Orders.

## Handelslogik
1. Einen Stochastischen Oszillator mit konfigurierbarer %K-Länge, %D-Glättung und einem zusätzlichen Verlangsamungsfaktor berechnen.
2. Wenn keine aktiven Positionen vorhanden sind, die zuletzt abgeschlossene Kerze auswerten:
   - Eine Long-Position öffnen, wenn %K über %D liegt und %D unter der konfigurierten Kaufzone liegt.
   - Eine Short-Position öffnen, wenn %K unter %D liegt und %D über der konfigurierten Verkaufszone liegt.
3. Wenn eine Position besteht, die letzte Füllung in der Leiter überwachen:
   - Wenn sich der Markt um mindestens den konfigurierten Gap (in Pips) gegen den Trade bewegt, eine neue Position in derselben Richtung mit dem doppelten Vorvolumen öffnen, solange die maximale Anzahl von Positionen nicht überschritten wird.
4. Für jeden Einstieg Stop-Loss- und Take-Profit-Niveaus pro Trade beibehalten, die aus Pip-Abständen abgeleitet werden, die mit dem `PriceStep` und der Anzahl der Dezimalstellen des Instruments in Preispunkte umgerechnet werden. Wenn der Schlusskurs den Stop oder das Ziel erreicht, verlässt die Strategie die spezifische Position mit einer Market-Order.
5. Nach jedem Kerzenschluss einen Trailing-Stop anwenden. Wenn sich der Trade weit genug in die günstige Richtung bewegt, wird der Stop-Preis um den angegebenen Trailing-Schritt gestrafft, was das per-Position-Trailing-Verhalten des Terminals approximiert.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `OrderVolume` | `0.1` | Anfängliche Positionsgröße in Lots. Zusätzliche Einstiege verdoppeln das vorherige Volumen. |
| `TakeProfitPips` | `50` | Take-Profit-Abstand in Pips. Der Wert wird intern in Preispunkte umgerechnet. |
| `StopLossPips` | `50` | Stop-Loss-Abstand in Pips für jede Position. |
| `TrailingStopPips` | `10` | Trailing-Stop-Abstand in Pips. Auf null setzen, um Trailing zu deaktivieren. |
| `TrailingStepPips` | `5` | Minimale günstige Bewegung (in Pips), bevor der Trailing-Stop angepasst wird. |
| `MaxPositions` | `3` | Maximale Anzahl gleichzeitig offener Martingale-Schritte. Ein Wert von `0` entfernt das Limit. |
| `GapPips` | `7` | Preislücke in Pips, die erforderlich ist, bevor in die aktuelle Richtung verdoppelt wird. |
| `KPeriod` | `5` | Anzahl der Kerzen, die zur Erstellung der %K-Linie verwendet werden. |
| `DPeriod` | `3` | Periode des %D-Glättungsdurchschnitts. |
| `Slowing` | `3` | Zusätzliche Glättung, die auf %K angewendet wird. |
| `ZoneBuy` | `30` | %D-Schwellenwert zur Validierung von Long-Einstiegen (überverkaufte Zone). |
| `ZoneSell` | `70` | %D-Schwellenwert zur Validierung von Short-Einstiegen (überkaufte Zone). |
| `CandleType` | `15-Minuten-Zeitrahmen` | Kerzenserie für Berechnungen. |

## Implementierungshinweise
- Pip-Abstände werden mit `PriceStep` in Preise umgerechnet. Für 3- und 5-stellige Kurse wird ein zusätzlicher Faktor von 10 verwendet, um MetaTraders angepasste Punkt-Logik zu imitieren.
- Stop-Loss-, Take-Profit- und Trailing-Stop-Überprüfungen basieren auf geschlossenen Kerzenpreisen, um die Logik im Backtester deterministisch zu halten. Echtzeit-Ausführung kann angepasst werden, wenn Intrabar-Management erforderlich ist.
- Die Strategie öffnet nur eine Richtungsleiter gleichzeitig; alle Positionen müssen geschlossen sein, bevor die Richtung gewechselt wird.
- Die Python-Implementierung wird wie angefordert absichtlich weggelassen.
