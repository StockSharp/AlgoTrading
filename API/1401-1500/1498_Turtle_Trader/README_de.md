# Turtle Trader Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Turtle Trader folgt dem klassischen Turtle-Ausbruchssystem mit Donchian-Kanälen und ATR-basiertem Moneymanagement. Es kauft, wenn der Kurs neue Hochs durchbricht, und verkauft, wenn er unter neue Tiefs fällt. Pyramiding fügt Gewinnerpositionen hinzu, wenn sich der Kurs in die gewünschte Richtung bewegt.

## Details

- **Einstiegskriterien**: Ausbruch über Hochs/Tiefs von `S1` oder `S2`
- **Long/Short**: Beide
- **Ausstiegskriterien**: gegenläufiger Ausbruch oder ATR-Stop
- **Stops**: ATR-basiert
- **Standardwerte**:
  - `RiskPercent` = 1
  - `AtrPeriod` = 20
  - `StopMultiplier` = 1.5
  - `PyramidProfit` = 0.5
  - `S1Long` = 20
  - `S2Long` = 55
  - `S1LongExit` = 10
  - `S2LongExit` = 20
  - `S1Short` = 15
  - `S2Short` = 55
  - `S1ShortExit` = 7
  - `S2ShortExit` = 20
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ATR, Highest, Lowest
  - Stops: ATR
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
