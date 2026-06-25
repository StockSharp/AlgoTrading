# Urdala Trol Hedging-Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Urdala Trol Hedging-Grid-Strategie** ist eine direkte Portierung des MetaTrader 5 Expert Advisors `Urdala_Trol.mq5` in die StockSharp High-Level-API. Die Strategie hält kontinuierlich Positionen in beiden Richtungen und skaliert Positionen mithilfe eines martingalähnlichen Grids, wenn Stops ausgelöst werden. Sie arbeitet ausschließlich mit Level1-Daten (bestes Bid/Ask) ohne Indikatoren.

## Handelslogik
1. **Initiales Hedging (Schritt 0)** – wenn keine aktiven Positionen vorhanden sind, eröffnet die Strategie sofort eine Long- und eine Short-Marktorder unter Verwendung des *Base Volume*-Parameters.
2. **Skalierung der Verlustseite (Schritt 1.2)** – wenn nur eine Richtung offen bleibt und die verlustreichste Position auf dieser Seite mindestens `Grid Step` Pips vom aktuellen Preis entfernt ist, eröffnet die Strategie eine zusätzliche Position in derselben Richtung. Das neue Volumen entspricht dem Volumen der am wenigsten profitablen Position plus `Min Lots Multiplier * minVolumeStep`, wobei `minVolumeStep` aus dem `VolumeStep` oder `MinVolume` des Instruments abgeleitet wird.
3. **Stop-Loss-Behandlung (Schritt 1.1)** – wenn eine Position durch den Stop-Loss (einschließlich Trailing-Anpassungen) mit negativem Ergebnis geschlossen wird, tritt die Strategie in dieselbe Richtung wieder ein, sofern nicht bereits ein aktiver Trade näher als `Min Nearest` Pips am Ausstiegspreis vorhanden ist.
4. **Reaktion auf profitablen Stop (Schritt 2.1)** – wenn der Stop eine Position mit Gewinn schließt, eröffnet die Strategie sofort einen Trade in der entgegengesetzten Richtung mit dem skalierten Volumen.
5. **Trailing Stop** – sobald der Preis um `Trailing Stop + Trailing Step` Pips über den Einstieg hinaus vorrückt, wird der Stop nachgezogen, um einen Abstand von `Trailing Stop` Pips zu halten. Das Trailing ist optional und wird nur aktiviert, wenn beide Parameter größer als null sind.

Alle in Pips ausgedrückten Abstände werden über den `PriceStep` des Instruments in absolute Preisverschiebungen umgerechnet. Bei Fünf- oder Dreistelligen Notierungen wird der Schritt mit zehn multipliziert, um die "adjusted point"-Logik des ursprünglichen MQL zu replizieren.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `BaseVolume` | 0.1 | Initiale Lotgröße zum Eröffnen des ersten Hedge-Paares. |
| `MinLotsMultiplier` | 3 | Anzahl der Mindestlots, die beim Skalieren zum Verlusthandels-Volumen hinzugefügt werden. |
| `StopLossPips` | 50 | Stop-Loss-Abstand in Pips. Ein Wert von null deaktiviert Stop und Trailing-Logik. |
| `TrailingStopPips` | 5 | Trailing-Stop-Abstand in Pips. Auf null setzen, um Trailing zu deaktivieren. |
| `TrailingStepPips` | 5 | Zusätzlicher Pip-Abstand, bevor der Trailing Stop sich bewegt. Muss positiv sein, wenn Trailing aktiviert ist. |
| `GridStepPips` | 50 | Mindestpreisabstand (in Pips) zwischen der Verlustposition und dem aktuellen Preis, bevor eine neue Scale-In-Order platziert wird. |
| `MinNearestPips` | 3 | Wenn eine bestehende Position näher als dieser Abstand am letzten Stop-Preis liegt, überspringt die Strategie den sofortigen Wiedereinstieg. |

## Implementierungshinweise
- Verwendet `SubscribeLevel1()` zum Verfolgen von Bid/Ask-Updates und zum Ausführen der Entscheidungsmaschine bei jedem Tick.
- Orders werden über den High-Level-Helper `RegisterOrder` registriert, was präzises Tracking über `OnOwnTradeReceived` ermöglicht.
- Individuelle Positionsobjekte werden intern verwaltet, um das Hedging-Verhalten zu reproduzieren, da StockSharp-Portfolios standardmäßig nettopositionsbasiert sind.
- Stop-Loss- und Trailing-Logik werden innerhalb der Strategie durch das Senden von Marktorders bei Überschreiten der Schwellenwerte ausgeführt; keine nativen Stop-Orders werden registriert.

## Verwendungshinweise
1. Weisen Sie der Strategie ein liquides Instrument und ein Portfolio zu und stellen Sie sicher, dass `PriceStep`, `VolumeStep` sowie Min-/Max-Volumenwerte für genaue Konvertierungen konfiguriert sind.
2. Starten Sie die Strategie; sie wird sofort ein gehedgtes Paar aufbauen und dann gemäß der originalen MQL-Logik auf Stop-Ereignisse reagieren.
3. Passen Sie die Pip-Parameter an die Volatilität des Instruments an. Große `Grid Step`-Werte reduzieren die Häufigkeit zusätzlicher Orders, während ein größerer `Min Lots Multiplier` das Martingale-Wachstum beschleunigt.
4. Überwachen Sie das resultierende Exposure sorgfältig; das Martingale-Verhalten kann das Volumen schnell eskalieren, wenn mehrere Stops nacheinander ausgelöst werden.

Eine Python-Implementierung wird in diesem Ordner absichtlich nicht bereitgestellt, entsprechend den Anforderungen dieser Konvertierungsaufgabe.
