# Renko Live Chart Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie emuliert einen klassischen Renko-Brick-Chart und handelt bei Richtungsänderungen der Bricks. Sie wurde aus dem MetaTrader-Skript **RenkoLiveChart_v600** konvertiert.

## Logik

Die Strategie baut Renko-Bricks aus abgeschlossenen zeitbasierten Kerzen. Wenn der Preis mindestens die gewählte Box-Größe vom letzten Brick-Preis abweicht, wird ein neuer Brick gebildet. Eine Long-Position wird bei einem aufwärts gerichteten Brick eröffnet und eine Short-Position bei einem abwärts gerichteten Brick.

## Parameter

- **Candle Type** – Zeitrahmen der Eingabekerzen, die für den Brick-Aufbau verwendet werden.
- **Brick Size** – Preisschritt, der die Höhe eines Renko-Bricks definiert.
- **Brick Offset** – anfänglicher Versatz in Bricks, der auf den ersten Brick angewendet wird.
- **Show Wicks** – Dochte auf dem Chart beim Zeichnen von Kerzen anzeigen.

## Hinweise

- Trades werden nur auf abgeschlossenen Kerzen ausgeführt.
- Der Positionsschutz wird beim Start der Strategie automatisch gestartet.
- Diese Implementierung konzentriert sich auf das Kern-Renko-Verhalten und ignoriert erweiterte Funktionen des Originalskripts, wie das Handling externer Dateien.
