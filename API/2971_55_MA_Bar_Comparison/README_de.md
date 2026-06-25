# FiftyFiveMaBarComparisonStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Strategie repliziert den MetaTrader 5 Expert Advisor "55 MA", indem sie zwei Punkte eines gleitenden 55-Perioden-Durchschnitts vergleicht und handelt, sobald ihre Differenz einen konfigurierbaren Schwellenwert überschreitet. Alle Berechnungen werden auf abgeschlossenen Kerzen innerhalb einer benutzerdefinierten Intraday-Sitzung durchgeführt, und die Handelsrichtung kann optional invertiert werden. Der Algorithmus bewahrt das ursprüngliche Verhalten, bei dem eine Short-Position eröffnet wird, wenn keine bullische Bedingung erfüllt ist.

## Handelslogik
1. Die ausgewählte Kerzenserie abonnieren und einen gleitenden Durchschnitt mit der gewählten Länge, Methode und dem angewandten Preis berechnen.
2. Die neuesten Werte des gleitenden Durchschnitts in einem Puffer halten, damit auf die Werte bei den Balkenindizes `BarA` und `BarB` zugegriffen werden kann, auch wenn ein horizontaler MA-Shift verwendet wird.
3. Wenn eine abgeschlossene Kerze innerhalb des `[StartHour, EndHour)`-Fensters eintrifft:
   - Den MA-Wert bei `BarA + MaShift` und `BarB + MaShift` abrufen.
   - Wenn der Wert bei `BarA` den Wert bei `BarB` um mehr als `DifferenceThreshold` überschreitet, eine Long-Position eröffnen, es sei denn, `ReverseSignals` ist aktiviert.
   - Wenn der Wert bei `BarA` niedriger ist als der Wert bei `BarB` um mehr als `DifferenceThreshold`, eine Short-Position eröffnen (oder eine Long-Position wenn `ReverseSignals` aktiviert ist).
   - Andernfalls behält die Strategie das ursprüngliche EA-Verhalten bei und löst einen Short-Einstieg aus.
4. Orders werden immer zum Marktpreis mit dem Strategie-`Volume` gesendet. Wenn `CloseOppositePositions` aktiviert ist, wird die angeforderte Größe erhöht, um eine etwaige entgegengesetzte Exposition zu schließen, bevor die neue Position aufgebaut wird.
5. Optionale Stop-Loss- und Take-Profit-Schutzmaßnahmen werden über `StartProtection` angehängt. Abstände werden in Pips angegeben, wobei ein Pip dem `PriceStep` multipliziert mit 10 für Instrumente mit 3 oder 5 Dezimalstellen entspricht.

## Eingaben
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-Minuten-Zeitrahmen | Kerzenserie für Berechnungen und Signale. |
| `StopLossPips` | `int` | 30 | Stop-Loss-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `TakeProfitPips` | `int` | 50 | Take-Profit-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `StartHour` | `int` | 8 | Inklusive Stunde (0-23) die den Beginn der Handelssitzung markiert. |
| `EndHour` | `int` | 21 | Exklusive Stunde (0-23) die das Ende der Handelssitzung markiert. Muss größer als `StartHour` sein. |
| `DifferenceThreshold` | `decimal` | 0.0001 | Minimale absolute Differenz zwischen den verglichenen MA-Werten, die ein Richtungssignal auslöst. |
| `BarA` | `int` | 0 | Index des ersten Balkens für den MA-Vergleich (0 = aktuelle Kerze). |
| `BarB` | `int` | 1 | Index des zweiten Balkens für den MA-Vergleich. |
| `ReverseSignals` | `bool` | `false` | Invertiert die bullischen und bärischen Bedingungen. |
| `CloseOppositePositions` | `bool` | `false` | Wenn aktiviert, wird die Ordergröße erhöht, um eine Position in der entgegengesetzten Richtung zu schließen, bevor der neue Trade eröffnet wird. |
| `MaShift` | `int` | 0 | Horizontale Verschiebung auf die Linie des gleitenden Durchschnitts. Positive Werte greifen auf ältere MA-Punkte zu. |
| `MaLength` | `int` | 55 | Periode des gleitenden Durchschnitts. |
| `MaMethod` | `MovingAverageMethods` | `Exponential` | Glättungsmethode (`Simple`, `Exponential`, `Smoothed`, `Weighted`). |
| `AppliedPrice` | `AppliedPriceTypes` | `Median` | Preis als MA-Eingabe (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |

## Positionsmanagement
- Das Strategie-`Volume` einstellen, um die Basis-Handelsgröße zu steuern. Es wird mit der aktuellen Position kombiniert, wenn `CloseOppositePositions` aktiv ist.
- Stop-Loss- und Take-Profit-Schutzmaßnahmen sind optional. Sie werden nur angehängt, wenn der jeweilige Pip-Abstand größer als null ist.

## Hinweise
- Das Handelsfenster arbeitet in der Instrumentenzeit; Signale außerhalb von `[StartHour, EndHour)` werden übersprungen.
- Wenn `MaShift` negative Indizes erzeugt, wartet die Strategie, bis genug Geschichte angesammelt ist, und spiegelt damit das ursprüngliche EA-Verhalten wider, bei dem verschobene Puffer `EMPTY_VALUE` zurückgeben können.
- Da der ursprüngliche Expert immer standardmäßig eine Verkaufsorder auslöst, wenn der Differenzschwellenwert nicht erreicht wird, behält die konvertierte Strategie dieselbe Logik für volle Treue. `DifferenceThreshold` anpassen, wenn dieses Verhalten unerwünscht ist.
