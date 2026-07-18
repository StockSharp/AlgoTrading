# RangeEA Weekly Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

RangeEA Weekly Grid Strategy ist ein Limit-Order-Grid-System, das vom ursprünglichen MetaTrader-Expertenberater umgewandelt wurde. Der Algorithmus identifiziert den Strom
ermittelt die wöchentliche Handelsspanne und füllt sie mit einer konfigurierbaren Anzahl ausstehender Limit-Orders. Jede Bestellung verwendet dynamische Stop-Loss- und
Take-Profit-Offsets werden im Verhältnis zum Abstand zwischen dem Grenzpreis und dem aktuellen Marktpreis skaliert und dabei ebenfalls berücksichtigt
Mindestabstände, ausgedrückt in Punkten. Gewinne können gesperrt werden, indem das gesamte Buch geschlossen wird, sobald das Portfolio-Eigenkapital um a wächst
vordefinierter Prozentsatz.

Die Implementierung nutzt das übergeordnete API von StockSharp: Kerzen steuern die Entscheidungslogik, ausstehende Aufträge werden mit verwaltet
Strategiehilfsmethoden und Risikokontrollen werden als optimierungsbereite Parameter bereitgestellt.

## Handelslogik

1. Abonnieren Sie zwei Kerzenstreams:
   - Ein benutzerdefinierter Zeitrahmen (standardmäßig 1 Stunde), der die Netzwartung steuert.
   - Wöchentliche Kerzen, die zur Schätzung der aktuellen Handelsspanne verwendet werden.
2. Aktualisieren Sie für jede fertige wöchentliche Kerze das höchste Hoch und das niedrigste Tief der letzten zwei Wochen. Ihr Unterschied wird
die aktive Handelsspanne.
3. Auf jeder fertigen Handelskerze:
   - Beachten Sie das konfigurierte Handelsfenster (`StartTradeHour` bis `EndTradeHour`).
   - Optional können Sie das Raster zu Beginn jedes Handelstages zurücksetzen.
   - Wenn keine ausstehenden Limit-Orders vorhanden sind, verteilen Sie neue Orders gleichmäßig zwischen dem Range-Tief und dem Range-Hoch.
   - Nachdem bereits zwei Orders ausgeführt wurden, ersetzen Sie die vorletzte Ausführung durch eine neue Order zum gleichen Preis wie im Raster
schrumpft auf `NumberOfOrders - 2` Elemente.
   - Überwachen Sie kontinuierlich das Eigenkapital Ihres Kontos und liquidieren Sie alles, wenn der konfigurierte Gewinnprozentsatz erreicht ist.
4. Wenn sich das Handelsfenster schließt und `CloseAllAtEndTrade` aktiviert ist, stornieren Sie alle ausstehenden Aufträge und verlassen Sie bestehende Positionen.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `CandleType` | Handelszeitrahmen, der zum Auslösen der Netzwartung verwendet wird. | 1 Stunde Kerzen |
| `WeeklyCandleType` | Zeitrahmen, der zur Ableitung der Bereichsgrenzen verwendet wird. | 1 Woche Kerzen |
| `StartTradeHour` | Tageszeit, zu der neue Bestellungen aufgegeben werden können. | 0 |
| `EndTradeHour` | Tageszeit, zu der der Handel stoppt. | 24 |
| `CloseAllAtEndTrade` | Schließen Sie alle Aufträge und Positionen außerhalb des Handelsfensters. | wahr |
| `MaxOpenOrders` | Maximale Anzahl gleichzeitiger Aufträge und Positionen. | 5 |
| `NumberOfOrders` | Anzahl der Limit-Orders im Raster. | 10 |
| `OrderVolume` | Für jede Bestellung verwendetes Volumen. | 0,01 |
| `ResetOrdersDaily` | Bauen Sie das Raster zu Beginn jedes Handelstages neu auf. | wahr |
| `StopLossPoints` | Minimaler Stop-Loss-Abstand in Punkten. | 60 |
| `TakeProfitPoints` | Mindest-Take-Profit-Distanz in Punkten. | 60 |
| `StopLossMultiplier` | Auf die dynamische Stop-Loss-Distanz angewendeter Multiplikator. | 3 |
| `TakeProfitMultiplier` | Auf die dynamische Take-Profit-Distanz angewendeter Multiplikator. | 1 |
| `TargetPercentage` | Prozentsatz des Eigenkapitalgewinns, der die Liquidation auslöst. | 8 |

## Risikomanagement

- Die Strategie respektiert das `MaxOpenOrders`-Limit, um die Anzahl der aktiven Orders und Positionen unter Kontrolle zu halten.
- Stop-Loss- und Take-Profit-Level sind immer mindestens die konfigurierte Anzahl von Punkten vom Einstieg entfernt und können optional auch sein
um die Multiplikatorparameter erweitert.
- Die Option zum täglichen Zurücksetzen verhindert, dass veraltete Bestellungen in eine neue Sitzung übernommen werden.
- Ein Aktienziel auf Portfolioebene ermöglicht es der Strategie, Gewinne durch eine Abflachung des Buchbestands zu sichern.

## Notizen

- Stellen Sie sicher, dass das ausgewählte Wertpapier wöchentliche Kerzen bereitstellt; Andernfalls kann die Strategie die Reichweite nicht berechnen.
- Wenn Sie Instrumente mit nicht standardmäßigen Preisschritten verwenden, passen Sie die punktbasierten Einstellungen an die zugrunde liegende Tick-Größe an.
- Die Optimierung von `NumberOfOrders`, `OrderVolume` und den Stop/Take-Multiplikatoren hilft dabei, das Raster an verschiedene Ebenen anzupassen
Volatilität.
