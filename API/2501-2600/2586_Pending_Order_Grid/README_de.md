# Strategie für Pending-Order-Raster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert das Verhalten des MetaTrader-Expertenberaters für das "AntiFragile" Pending-Order-Raster. Sie baut kontinuierlich ein symmetrisches Raster von Stop-Orders rund um den aktuellen Marktpreis auf und wendet Schutzausstiege an, sobald Positionen eröffnet werden.

## Kernlogik
- Beim Start speichert die Strategie das beste Bid und Ask aus Level-1/Orderbuch-Daten und platziert Buy-Stop-Orders oberhalb des Preises und Sell-Stop-Orders unterhalb des Preises.
- Orderpreise werden vom Markt durch den Parameter *Distance* versetzt, und jedes nachfolgende Level ist durch *Spacing (ticks)* multipliziert mit dem Instrumentpreisschritt beabstandet.
- Jedes neue Raster-Level erhöht das Ordervolumen um *Volume Increase %* relativ zur Ausgangsgröße, was die Martingale-Skalierung der MQL-Version implementiert.
- Wenn eine Order ausgeführt wird, ist die resultierende Nettoposition mit Stop-Loss- und Take-Profit-Orders geschützt. Optionale Trailing-Stop-Logik verwendet das neueste Bid/Ask, um den Stop zu straffen, wenn der unrealisierte Gewinn die Trailing-Distanz überschreitet.
- Das Raster wird automatisch neu aufgebaut, nachdem alle ausstehenden Orders ausgeführt oder storniert wurden und die Position wieder flat ist.

## Parameter
- **Starting Volume** – Lot/Kontraktgröße für die erste ausstehende Order. Nachfolgende Orders skalieren mit *Volume Increase %*.
- **Volume Increase %** – Prozentualer Zuwachs, der jedem neuen Raster-Level hinzugefügt wird (0,1 entspricht +0,1% pro Level).
- **Distance** – Absoluter Preisversatz, der vor der ersten Order hinzugefügt wird (in Instrumentwährung interpretiert).
- **Spacing (ticks)** – Anzahl der Preisschritte zwischen aufeinanderfolgenden Raster-Orders.
- **Orders per side** – Maximale Anzahl von Raster-Orders für Longs und Shorts separat.
- **Take Profit (ticks)** – Abstand des Gewinnziels vom Durchschnittseinstieg, ausgedrückt in Preisschritten.
- **Stop Loss (ticks)** – Stop-Abstand vom Durchschnittseinstieg. Auf null setzen, um den anfänglichen Stop zu deaktivieren.
- **Trailing Stop (ticks)** – Trailing-Distanz. Auf null setzen, um Trailing-Anpassungen zu deaktivieren.
- **Enable Long Grid / Enable Short Grid** – Schalter zum Platzieren von Buy-Stop- oder Sell-Stop-Orders.

## Implementierungshinweise
- StockSharp-Strategien verwenden Nettopositionen, daher werden entgegengesetzte Ausführungen sich gegenseitig aufheben, anstatt wie in MT4 abgesicherte Körbe zu erstellen. Das Raster bleibt symmetrisch, aber nur das Nettoengagement wird verfolgt.
- Volumen und Preise werden auf die Instrumentschrittgrößen gerundet, bevor Orders eingereicht werden.
- Trailing-Stops werden durch Stornieren der vorherigen Stop-Order und Senden einer neuen auf dem engeren Level neu erstellt, sobald der Gewinn die Trailing-Distanz überschreitet.
- Die Strategie erfordert Orderbuchdaten (SubscribeOrderBook) für die Preisverfolgung und Trailing-Logik.

## Verwendungstipps
1. Konfigurieren Sie *Starting Volume* und *Volume Increase %* konservativ; die ursprünglichen Standardwerte nehmen Forex-Lot-Sizing an und können schnell wachsen.
2. Stellen Sie sicher, dass das Portfolio Stop-Orders für das Ziel-Venue unterstützt. Alle Raster-Eintritte sind Stop-Market-Orders.
3. Überwachen Sie die Margeanforderungen, da eine große Anzahl ausstehender Orders reserviertes Kapital verbrauchen kann.
