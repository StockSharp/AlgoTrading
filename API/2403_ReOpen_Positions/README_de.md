# Strategie zur Wiedereröffnung von Positionen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MQL5-Beispiels `Exp_ReOpenPositions`. Es demonstriert, wie Positionen wiedereröffnet werden, wenn der aktuelle Trade profitabel wird.

## Logik

1. Die Strategie eröffnet zu Beginn eine erste Long-Position.
2. Wenn sich der Preis um `ProfitThreshold` Punkte vom letzten Einstiegspreis wegbewegt, wird eine weitere Long-Position eröffnet.
3. Jeder neue Einstieg aktualisiert Stop-Loss- und Take-Profit-Niveaus relativ zu seinem eigenen Preis.
4. Wenn der Preis den Stop-Loss oder Take-Profit erreicht, werden alle Positionen geschlossen und der Zyklus beginnt von vorne.

Die gleichen Regeln gelten für Short-Trades, wenn die erste Position short ist.

## Parameter

- `ProfitThreshold` – Preisbewegung in Punkten, die zum Hinzufügen einer neuen Position erforderlich ist.
- `MaxPositions` – maximale Anzahl geöffneter Positionen.
- `StopLossPoints` – Abstand vom Einstieg zum Schutz-Stop.
- `TakeProfitPoints` – Abstand vom Einstieg zum Gewinnziel.
- `CandleType` – Kerzendatentyp für die Verarbeitung.

## Hinweise

Das Beispiel ist für Bildungszwecke vereinfacht und verwaltet kein Handelsvolumen oder Geldmanagement wie im Originalskript.
