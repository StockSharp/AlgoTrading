# Crypto MVRV ZScore-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie wendet das MVRV Z-Score-Konzept an, um Extremwerte zwischen Marktwert und realisiertem Wert zu erkennen.
Positionen werden eröffnet, wenn der Spread-Z-Score vordefinierte Schwellenwerte kreuzt, und bei entgegengesetzten Kreuzungen geschlossen.

## Details

- **Einstiegskriterien**:
  - Long, wenn der Spread-Z-Score `LongEntryThreshold` von unten kreuzt.
  - Short, wenn der Spread-Z-Score `ShortEntryThreshold` von oben kreuzt.
- **Long/Short**: Konfigurierbar (`TradeDirection`).
- **Ausstiegskriterien**:
  - Kreuzung des entgegengesetzten Schwellenwerts.
- **Stops**: Keine.
- **Standardwerte**:
  - `ZScoreCalculationPeriod` = 252
  - `LongEntryThreshold` = 0.382
  - `ShortEntryThreshold` = -0.382
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: SMA, StandardDeviation, Z-Score
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
