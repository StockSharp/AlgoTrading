# Strategie Harmony Signal Flow By Arun
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Harmony Signal Flow By Arun nutzt einen RSI mit kurzer Periode, um Umkehrungen mit festen Stop-Loss- und Zielwerten zu erfassen. Die Strategie geht Long, wenn der RSI die untere Schwelle nach oben kreuzt, und Short, wenn er die obere Schwelle nach unten kreuzt. Positionen werden durch Stop, Ziel oder um 15:25 Uhr täglich geschlossen.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: RSI kreuzt `LowerThreshold` nach oben.
  - **Short**: RSI kreuzt `UpperThreshold` nach unten.
- **Ausstiegskriterien**: Stop-Loss oder Ziel erreicht, oder Schließung um 15:25 Uhr.
- **Stops**: Fester Stop-Loss und Ziel.
- **Standardwerte**:
  - `RsiPeriod` = 5
  - `LowerThreshold` = 30
  - `UpperThreshold` = 70
  - `BuyStopLoss` = 100
  - `BuyTarget` = 150
  - `SellStopLoss` = 100
  - `SellTarget` = 150
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long & Short
  - Indikatoren: RSI
  - Komplexität: Niedrig
  - Risikolevel: Mittel
