# Strategie Ha MaZi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert Heikin Ashi-Kerzen, einen EMA-Filter und ZigZag-Pivot-Bestätigung. Ein Long-Trade wird eröffnet, wenn sich eine bullische Heikin Ashi-Kerze an einem neuen ZigZag-Tief oberhalb der EMA bildet. Shorts erscheinen bei einer bärischen Kerze an einem neuen ZigZag-Hoch unterhalb der EMA. Positionen werden durch festen Stop-Loss oder Take-Profit geschlossen.

## Details
- **Einstiegskriterien**: ZigZag-Pivot mit Heikin Ashi-Richtung und EMA-Filter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Fester Stop und Ziel.
- **Standardwerte**:
  - `MaPeriod` = 40
  - `ZigzagLength` = 13
  - `StopLoss` = 70
  - `TakeProfit` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Heikin Ashi, EMA, ZigZag
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
