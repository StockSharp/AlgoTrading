# Range-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie misst die höchsten und niedrigsten Preise innerhalb der letzten `RangePeriod` Kerzen. Wenn die Kerze außerhalb dieser Spanne schließt und die Gesamtbreite der Spanne kleiner als `MaxRangePoints` ist, geht die Strategie in die Ausbruchsrichtung über.

## Teilnahmebedingungen
- **Long**: Kerzenschluss >= höchstes Hoch des Lookback-Bereichs UND Bereich in Punkten <= `MaxRangePoints` UND keine offene Position.
- **Short**: Kerzenschluss <= niedrigstes Tief des Lookback-Bereichs UND Bereich in Punkten <= `MaxRangePoints` UND keine offene Position.

## Ausgangsregeln
- Schützender Stop-Loss und Take-Profit werden sofort nach Eröffnung der Position angewendet.
- Es werden keine zusätzlichen Ausstiegsregeln verwendet; Die Position bleibt geöffnet, bis der Schutz sie schließt.

## Parameter
- `RangePeriod` – Anzahl der Kerzen für die Höchst-/Tiefstberechnung.
- `MaxRangePoints` – maximale Breite des Bereichs in Punkten, um den Handel zu ermöglichen.
- `CandleType` – Zeitrahmen der Kerzen, die für Analyse und Handel verwendet werden.
- `Volume` – Market-Order-Volumen.
- `StopLossPoints` – Stop-Loss-Distanz in Punkten.
- `TakeProfitPoints` – Take-Profit-Distanz in Punkten.

## Indikatoren
- Höchste
- Am niedrigsten
