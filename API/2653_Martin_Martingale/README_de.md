# Martin Martingale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie reproduziert das Verhalten des ursprünglichen "Martin"-Expertenberaters aus MQL, indem sie ein abgesichertes Martingale-Grid um den aktuellen Preis betreibt. Sie wechselt kontinuierlich zwischen Long- und Short-Positionen und verdoppelt das gehandelte Volumen bei jeder Umkehr, bis der kumulierte Gewinn des gesamten Korbs das konfigurierte Ziel erreicht. Kerzen werden nur als Treiber für die Entscheidungslogik verwendet, während die tatsächlichen Ausführungen auf Marktorders und Stop-Orders basieren, die durch die High-Level-API von StockSharp bereitgestellt werden.

## Funktionsweise
1. Beim Start liest die Strategie den `PriceStep` des Instruments, um die Parameter `EntryOffsetPoints` und `StepPoints` in absolute Preisabstände umzurechnen. Fehlt der Preisschritt, wird der Wert 1 angenommen.
2. Wenn keine offene Position und kein aktiver Martingale-Zyklus vorhanden ist, platziert die Strategie eine Kauf-Stop-Order und eine Verkauf-Stop-Order um den letzten Schlusskurs. Die Abstände betragen `EntryOffsetPoints * PriceStep`, was dem 10-Punkte-Abstand im ursprünglichen MQL-Code entspricht.
3. Wenn eine der Stop-Orders ausgeführt wird, wird die entgegengesetzte ausstehende Order storniert. Die Ausführung definiert den ersten Trade der Martingale-Sequenz: Die Strategie speichert Preis, Volumen und Richtung und setzt den internen Level-Zähler auf 1.
4. Bei jedem anschließenden Kerzenschluss wird der aktuelle Schlusskurs mit dem Preis der zuletzt ausgeführten Order verglichen. Hat sich der Markt mindestens `martingaleLevel * StepPoints * PriceStep` gegen diese Order bewegt, wird eine Marktorder in entgegengesetzter Richtung mit doppeltem Volumen gegenüber dem vorherigen Trade eingereicht. Die letzte Trade-Information wird nach jeder Ausführung aktualisiert.
5. Der unrealisierte Gewinn wird als `PnL + Position * (closePrice - PositionPrice)` bewertet. Wenn dieser aggregierte Gewinn den Parameter `ProfitTarget` übersteigt, sendet die Strategie `CloseAll()`, um jede Position im Korb zu schließen, storniert alle verbleibenden Orders und setzt den Zyklus zurück, damit ein neues Stop-Order-Paar platziert werden kann.
6. Dieselbe Zurücksetzung erfolgt automatisch, wenn alle Positionen manuell geschlossen werden: Die internen Zähler werden gelöscht und neue Stop-Orders werden bei der nächsten Kerze erstellt.

Dieser Workflow spiegelt die alternierende Kauf-/Verkaufslogik des ursprünglichen Expertenberaters wider und hält die Implementierung vollständig innerhalb der High-Level-API von StockSharp.

## Parameter
- `StepPoints` – Anzahl der Preisschritte zur Berechnung des Umkehrschwellenwerts für die nächste Durchschnittsorder. Standardmäßig 10 und kann optimiert werden.
- `EntryOffsetPoints` – Abstand für die initialen Kauf-/Verkauf-Stop-Orders in Preisschritten. Ebenfalls standardmäßig 10 Punkte wie die MQL-Version.
- `ProfitTarget` – absoluter Währungsgewinn, der zum Schließen des gesamten Martingale-Korbs erforderlich ist. Sobald der kombinierte realisierte und unrealisierte PnL diesen Wert übersteigt, werden alle Positionen liquidiert.
- `CandleType` – Kerzenabonnement zum Antreiben der Strategielogik. Der Standard ist der Ein-Minuten-Zeitrahmen, aber jeder vom Marktplatz unterstützte `DataType` kann ausgewählt werden.

Die Basis-Handelsgröße wird aus der `Volume`-Eigenschaft der Strategie entnommen. Jede neue Umkehr multipliziert diese Basis nach der klassischen Martingale-Methode mit Zweierpotenzen.

## Praktische Hinweise
- Konfigurieren Sie `Volume` immer entsprechend der minimalen Lotgröße des Brokers. Das Verdoppelungsschema erhöht das Exposure schnell, daher sollten Risikolimits extern durchgesetzt werden.
- Da die Orderplatzierung durch Kerzenschlüsse gesteuert wird, können schnelle Preisbewegungen innerhalb der Kerze Einstiege etwas später auslösen als die tickbasierte MQL-Version. Die Stop-Orders halten jedoch die Einstiegspreise im Einklang mit der ursprünglichen Logik.
- Die Strategie zeichnet Preiskerzen und eigene Trades im Standarddiagrammbereich für einfachere visuelle Verfolgung.
- Es wird kein automatischer Stop-Loss verwendet. Die einzige Ausstiegsbedingung ist der `ProfitTarget`, daher sollten Instrument und Zeitrahmen sorgfältig gewählt werden, um das Risiko großer adverser Trends zu kontrollieren.

## Unterschiede zum MQL-Experten
- StockSharp verwendet Nettopositionen, daher wird jede Umkehr mit einer Marktorder ausgeführt, die sowohl das vorherige Exposure schließt als auch die neue Position in einem einzigen Trade eröffnet. Der kumulative PnL des Korbs bleibt identisch mit der abgesicherten Implementierung.
- Die Tick-für-Tick-Logik wurde durch Kerzenschlüsse für die Signalauswertung ersetzt, um im Rahmen der empfohlenen High-Level-API-Nutzung zu bleiben.
- Order-Identifikatoren werden verfolgt, um die Verarbeitung partieller Ausführungen mehrfach zu vermeiden und so sicherzustellen, dass die Volumen-Verdoppelungslogik konsistent bleibt.

Diese Änderungen halten das Handelsverhalten der Quellstrategie treu, während sie an das StockSharp-Framework angepasst werden.
