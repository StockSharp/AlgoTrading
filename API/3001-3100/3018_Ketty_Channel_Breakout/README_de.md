# Ketty Kanalausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Ketty Kanalausbruch-Strategie ist eine direkte C#-Konvertierung des ursprünglichen Ketty.mq5-Experten-Advisors. Sie baut während eines konfigurierbaren Vormarkt-Fensters einen kurzfristigen Preiskanal auf und wartet, bis der Markt außerhalb dieses Bereichs ausbricht. Wenn ein Ausbruch erfolgt, platziert die Strategie eine Stop-Order auf der gegenüberliegenden Seite des Kanals mit optionalem Stop-Loss- und Take-Profit-Schutz und spiegelt damit den ausstehenden Order-Workflow wider, der im MQL5-Skript implementiert ist.

## Handelslogik
1. **Täglicher Reset** – Bei der ersten Kerze jedes Handelstages löscht die Strategie ausstehende Orders (und Schutzorders, wenn keine Position offen ist) und setzt die Kanalstatistiken zurück.
2. **Kanalaufbau** – Zwischen `ChannelStartHour:ChannelStartMinute` und `ChannelEndHour:ChannelEndMinute` werden das höchste Hoch und das niedrigste Tief des ausgewählten `CandleType` verfolgt. Der erkannte Bereich stellt den Ausbruchskanal für den Rest des Tages dar.
3. **Order-Preise** – Der geplante Buy-Stop ist `channelHigh + OrderPriceShiftPips`, während der geplante Sell-Stop `channelLow - OrderPriceShiftPips` ist. Die Pip-zu-Preis-Konvertierung entspricht dem ursprünglichen Roboter: wenn das Instrument 3 oder 5 Dezimalstellen hat, entspricht ein Pip zehn Preisschritten; andernfalls wird ein einzelner Preisschritt verwendet.
4. **Signalerkennung** – Sobald der Kanal verfügbar ist und die aktuelle Zeit zwischen `PlacingStartHour` und `PlacingEndHour` liegt, wird die zuletzt abgeschlossene Kerze untersucht. Ein Kauf-Setup erscheint, wenn das Tief der Kerze den Kanal um mindestens `ChannelBreakthroughPips` unterschreitet. Ein Verkauf-Setup erscheint, wenn das Hoch der Kerze den Kanal um die gleiche Distanz übertrifft.
5. **Verwaltung ausstehender Orders** – Immer ist nur eine ausstehende Order aktiv. Sobald ein Signal generiert wird, wird die vorherige ausstehende Order (falls vorhanden) storniert und die neue Stop-Order registriert. Alle ausstehenden Orders werden nach `PlacingEndHour` automatisch entfernt.
6. **Schutzorders** – Nach der Ausführung der ausstehenden Order reicht die Strategie sofort den entsprechenden Schutz-Stop (wenn `StopLossPips` positiv ist) und das Gewinnziel (wenn `TakeProfitPips` positiv ist) ein. Diese Orders werden storniert, wenn die Position vollständig geschlossen ist.

## Parameter
- `EntryVolume` – Standardvolumen für neue Orders.
- `StopLossPips` – Abstand zwischen Einstiegspreis und Schutz-Stop-Order; auf null setzen, um zu deaktivieren.
- `TakeProfitPips` – Abstand zwischen Einstiegspreis und Take-Profit-Order; auf null setzen, um zu deaktivieren.
- `ChannelStartHour` / `ChannelStartMinute` – Tageszeit, zu der die Kanalberechnung beginnt.
- `ChannelEndHour` / `ChannelEndMinute` – Tageszeit, zu der die Kanalberechnung endet. Der Kanal kann über Mitternacht hinausgehen, weil die Implementierung das Zeitfenster normalisiert.
- `PlacingStartHour` – Stunde des Tages, ab der ausstehende Orders erscheinen können.
- `PlacingEndHour` – Stunde des Tages, nach der alle ausstehenden Orders storniert werden.
- `ChannelBreakthroughPips` – Ausbruchspuffer, der von der letzten Kerze durchbrochen werden muss, bevor eine Stop-Order ausgelöst wird.
- `OrderPriceShiftPips` – Zusätzlicher Versatz, der beim Platzieren der ausstehenden Stop-Order zur Kanalgrenze hinzugefügt wird.
- `VisualizeChannel` – Wenn aktiviert, zeichnet die Strategie zwei horizontale Linien, die den aktuellen Kanal auf dem Diagramm darstellen.
- `CandleType` – Zeitrahmen, der zum Aufbau und zur Überwachung des Kanals verwendet wird.

## Zusätzliche Hinweise
- Die Strategie geht davon aus, dass das Instrument kontinuierlich handelt; wenn Daten innerhalb des Kanal-Fensters fehlen, wartet das System auf neue Kerzen, bevor Orders ausgelöst werden.
- Schutzorders werden nach der Einstiegsfüllung mit separaten Stop-/Limit-Orders registriert, weil StockSharp SL/TP nicht direkt an ausstehende Orders anhängt wie MetaTrader.
- Stellen Sie sicher, dass `EntryVolume` dem Lot-Schritt des Brokers entspricht und dass der ausgewählte `CandleType` einem liquiden Zeitrahmen entspricht (der ursprüngliche Roboter wurde für Ein-Minuten-Balken entwickelt).
