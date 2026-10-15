# NextBar Momentum Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche, die auftreten, wenn der zuletzt abgeschlossene Balken weit entfernt von einem älteren Referenzbalken schließt. Sie wurde vom MetaTrader Expert Advisor "Nextbar" inspiriert und behält die ursprünglichen Money-Management-Funktionen wie pip-basierte Stops, Trailing-Logik und begrenztes Positions-Lifetime bei.

Die Standardkonfiguration zielt auf schnell bewegende FX- oder Index-Futures-Charts auf dem 15-Minuten-Zeitrahmen, aber die Logik funktioniert auf jedem Symbol, das regelmäßige Kerzen bereitstellt. Jede Order wird zum Marktpreis mit der konfigurierten Positionsgröße gesendet.

## Handelslogik

- **Signalerkennung**
  - Wenn ein neuer Balken abgeschlossen ist, vergleicht der Algorithmus den Schlusskurs des vorherigen Balkens mit dem Schlusskurs, der vor `SignalBar` Balken aufgetreten ist.
  - Wenn der vorherige Schlusskurs höher ist als der entfernte Schlusskurs um mehr als `MinDistancePips`, wird ein Long-Setup generiert.
  - Wenn der vorherige Schlusskurs niedriger ist als der entfernte Schlusskurs um mehr als `MinDistancePips`, erscheint ein Short-Setup.
  - Der `ReverseSignals`-Schalter dreht die Richtung jedes Setups um, um konträren Arbeitsabläufen gerecht zu werden.
- **Orderhandling**
  - Orders werden ignoriert, während eine Position offen ist. Die Strategie hält genau wie der ursprüngliche Expert Advisor nur eine einzelne Position auf einmal.
  - Jede Füllung speichert den Eintrittspreis und berechnet vorab die Stop-Loss- und Take-Profit-Niveaus in Preiseinheiten. Pip-basierte Werte werden mithilfe des Preisschritts des Wertpapiers konvertiert (5-stellige Instrumente verwenden automatisch einen 10×-Multiplikator, um der MetaTrader-Pip-Größe zu entsprechen).

## Ausstiegsregeln

- **Stop Loss / Take Profit** – Beide Niveaus sind optional. Ein Wert von null deaktiviert den entsprechenden Schutz. Die Strategie überwacht Kerzen-Hochs und -Tiefs, um Ausstiege auszulösen, wenn Niveaus gekreuzt werden.
- **Trailing Stop** – Wenn aktiviert (`TrailingStopPips` > 0), wird der Stop näher an den aktuellen Preis bewegt, sobald der Gewinn `TrailingStopPips + TrailingStepPips` überschreitet. Der Abstand vom Preis zum Stop schrumpft nie, was ein monotones Trailing-Verhalten gewährleistet.
- **Positionslebensdauer** – Nach dem Verbleib im Markt für `LifetimeBars` abgeschlossene Kerzen wird die Position bei der nächsten Balkeneröffnung unabhängig vom Gewinn geschlossen. Dies reproduziert den ursprünglichen "nach N Balken ablaufen"-Mechanismus.

## Parameter

- `CandleType` – Zeitrahmen für die Signalauswertung. Standardmäßig 15-Minuten-Kerzen.
- `OrderVolume` – Mit jeder Marktorder gesendete Menge.
- `StopLossPips` – Abstand vom Eintrittspreis zum Schutz-Stop, ausgedrückt in Pips.
- `TakeProfitPips` – Abstand vom Eintrittspreis zum Gewinnziel, ausgedrückt in Pips.
- `TrailingStopPips` – Vom Trailing Stop gehaltener Abstand. Auf null setzen, um die Trailing-Logik zu deaktivieren.
- `TrailingStepPips` – Zusätzlicher Gewinn, der benötigt wird, bevor der Trailing Stop erneut vorrückt. Wird ignoriert, wenn Trailing deaktiviert ist.
- `SignalBar` – Anzahl der Balken zwischen den Vergleichsschlusskursen. Muss mindestens zwei sein, um das Referenzieren des aktuellen Balkens zu vermeiden.
- `MinDistancePips` – Mindest-Pip-Abstand zwischen den verglichenen Schlusskursen, bevor ein Signal akzeptiert wird.
- `LifetimeBars` – Maximale Anzahl abgeschlossener Kerzen, die eine Position offen bleiben darf. Auf null setzen, um den Timer zu deaktivieren.
- `ReverseSignals` – Invertiert Long/Short-Signale wenn aktiviert.

## Implementierungshinweise

- Die Strategie verlässt sich auf eine kurze gleitende Liste vorheriger Schlusskurse anstatt auf schwere historische Strukturen, was die Signalberechnung leichtgewichtig hält.
- Pips werden mit dem Preisschritt des Wertpapiers in Preiseinheiten umgerechnet. Instrumente mit 3 oder 5 Dezimalstellen werden automatisch auf die traditionelle Pip-Definition abgebildet.
- Alle Risikokontrollen werden auf abgeschlossenen Kerzen angewandt. Wenn Sie Intrabar-Schutz benötigen, kombinieren Sie die Strategie mit Exchange-nativen Stop-Orders über die Plattformkonfiguration.
- Keine automatisierten Tests werden mit diesem Beispiel geliefert. Validieren Sie es auf historischen Daten, bevor Sie es in der Produktion einsetzen.
