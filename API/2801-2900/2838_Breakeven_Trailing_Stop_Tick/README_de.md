# Breakeven Trailing Stop Tick-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Tick-basierter Trailing-Stop-Manager, konvertiert vom MetaTrader Expert Advisor `e_Breakeven_v4`.
- Überwacht jeden Trade-Tick, um einen virtuellen Stop-Loss zu verschieben, sobald der Preis weit genug vom Einstieg wegläuft.
- Schließt Long- oder Short-Positionen zu Marktpreisen, wenn das Trailing-Niveau erreicht wird, und repliziert das Breakeven-plus-Schritt-Verhalten des ursprünglichen EAs.
- Enthält einen optionalen Demo-Modus, der während Tests zufällig Positionen öffnet, um die Trailing-Logik ohne externe Signalquelle zu demonstrieren.

## So funktioniert es
1. Die Strategie abonniert Trade-Ticks (`DataType.Ticks`), um den in MQL5 verwendeten `OnTick`-Callback zu emulieren.
2. Wenn eine Position existiert und der Trailing Stop (in Pips) plus der Trailing Step überschritten wurde, wird das Stop-Niveau näher an den Preis verschoben.
3. Für Long-Positionen wird der Stop bei `aktueller Preis - Trailing Stop` platziert, wenn die Bewegung vom Einstieg `Trailing Stop + Trailing Step` übersteigt.
4. Für Short-Positionen wird der Stop bei `aktueller Preis + Trailing Stop` platziert, wenn der Preis sich um die gleiche Distanz nach unten bewegt.
5. Wenn der Live-Preis das gespeicherte Stop-Niveau berührt oder kreuzt, verlässt die Strategie die gesamte Position zu Marktpreisen und setzt den Trailing-Zustand zurück.
6. Eine interne Pip-Konvertierung multipliziert den Broker-Preisschritt mit 10, wenn das Instrument 3 oder 5 Dezimalstellen hat, was der Punkt-zu-Pip-Anpassung von MQL5 entspricht.
7. Wenn der Demo-Modus aktiviert ist, öffnet die Strategie beim ersten neuen Tick nach dem Schließen des vorherigen Einstiegs zufällig einen Long- oder Short-Trade (unter Verwendung des konfigurierten `Volume`).

## Parameter
| Name | Beschreibung | Standard | Hinweise |
| --- | --- | --- | --- |
| `TrailingStopPips` | Abstand in Pips zwischen dem aktuellen Preis und dem Trailing Stop. | 10 | Auf `0` setzen, um Trailing vollständig zu deaktivieren. |
| `TrailingStepPips` | Zusätzlicher Pip-Abstand erforderlich, bevor der Stop wieder vorgerückt wird. | 1 | Muss größer als null sein, wenn der Trailing Stop aktiv ist, was die EA-Validierungsregel reproduziert. |
| `EnableDemoEntries` | Aktiviert zufällige Einstiege für Backtests ohne externes Signal. | `false` | Wenn `true`, wirft die Strategie bei jedem Tick, während sie flach ist, eine Münze, um die Richtung zu entscheiden. |

## Positionsverwaltungsregeln
- Die Strategie öffnet keine Positionen von sich aus, es sei denn `EnableDemoEntries` ist auf `true` gesetzt.
- Trailing ist symmetrisch für Long- und Short-Positionen und funktioniert mit jeder Volumengröße.
- Stop-Niveaus werden intern (virtuell) verwaltet und mit Marktausstiegen durchgesetzt, um explizite Stop-Orders zu vermeiden, die möglicherweise nicht von jedem Connector unterstützt werden.
- Jeder manuelle Trade oder jede externe Strategie kann die Einstiege liefern; diese Komponente verwaltet nur den Trailing Stop.

## Verwendungshinweise
- Funktioniert am besten mit Instrumenten, die Trade-Ticks liefern, damit das Trailing sofort reagiert.
- Stellen Sie sicher, dass `Volume` auf die Lot-Größe konfiguriert ist, die den eingehenden Positionen entspricht, wenn der Demo-Modus verwendet wird.
- Die Pip-Konvertierung setzt FX-ähnliche Preisgebung voraus, bei der Symbole mit 3 oder 5 Dezimalstellen einen ×10-Multiplikator benötigen, um Punkte in Pips umzuwandeln.
- Der Ausstieg wird beim ersten Tick ausgelöst, der den gespeicherten Stop-Preis kreuzt, was dem sofortigen Modifizieren-und-Schließen-Ablauf der MQL-Logik entspricht.

## Unterschiede zum ursprünglichen MQL5-Experten
- Verwendet virtuelle Stops mit Marktausstiegen anstatt Broker-seitige Stop-Loss-Orders zu modifizieren, weil StockSharp-Strategien Ausstiege typischerweise über Strategielogik verwalten.
- Ersetzt den MetaTrader-Tester-Zufallseinstiegsblock durch das konfigurierbare `EnableDemoEntries`-Flag.
- Konvertiert die Punkt-zu-Pip-Logik mit `Security.PriceStep` und Dezimalzählung anstatt `Symbol().Digits()`.
- Alle Kommentare und Protokollierungen sind jetzt gemäß Repository-Richtlinien auf Englisch.
