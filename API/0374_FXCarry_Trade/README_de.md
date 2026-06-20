# FX-Carry-Trade-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Währungsstrategie stuft ein Universum von Währungsinstrumenten nach dem Zinsdifferenzial zwischen Basis- und Kurswährung ein. Zu Beginn jedes Monats geht sie bei den `TopK` Symbolen mit dem höchsten Carry long und bei den `TopK` mit dem niedrigsten short. Die Gewinne zielen darauf ab, den positiven Carry bei Longs zu vereinnahmen und den negativen bei Shorts zu zahlen.

Zinsdifferenziale werden aus den Renditedaten jedes Wertpapiers gewonnen. Positionen werden gleichgewichtig dimensioniert und monatlich rebalanciert; jedes Instrument, das die Spitzen- oder Schlussgruppen verlässt, wird geschlossen und ersetzt.

## Details

- **Einstiegskriterien**:
  - Am ersten Handelstag des Monats das Zinsdifferenzial für jede Währung berechnen.
  - Die `TopK` Währungen mit dem höchsten Carry long und die `TopK` mit dem niedrigsten Carry short gehen, wenn die Orderwerte `MinTradeUsd` übersteigen.
- **Long/Short**: Long bei hohem Carry, Short bei niedrigem Carry.
- **Ausstiegskriterien**: Positionen werden geschlossen, wenn eine Währung beim nächsten Rebalancing die ausgewählten Gruppen verlässt.
- **Stops**: Keine.
- **Standardwerte**:
  - `Universe` – Liste der Währungsinstrumente.
  - `TopK` = 3.
  - `CandleType` = 1 Tag.
  - `MinTradeUsd` – Mindesttransaktionswert.
- **Filter**:
  - Kategorie: Carry.
  - Richtung: Long und Short.
  - Zeitrahmen: Monatlich.
  - Rebalancing: Monatlich.

