# Strategie Reiner Preis-Aktions-Ausbruch mit 1:5 RR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie Reiner Preis-Aktions-Ausbruch mit 1:5 RR nutzt einen Crossover zweier EMAs, bestätigt durch RSI und Volumen. Der Stop-Loss basiert auf ATR und das Take-Profit beträgt das Fünffache des Risikos.

## Details

- **Einstiegskriterien**:
  - **Long**: Schnelle EMA kreuzt über die langsame EMA, RSI > 50, Volumen über 20-Perioden-SMA.
  - **Short**: Schnelle EMA kreuzt unter die langsame EMA, RSI < 50, Volumen über 20-Perioden-SMA.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - ATR-basierter Stop-Loss und Take-Profit mit 1:5-Risiko-Rendite.
- **Stops**: Stop-Loss = 1.5 × ATR, Take-Profit = 5 × Risiko.
- **Standardwerte**:
  - `FastPeriod` = 9
  - `SlowPeriod` = 21
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `VolumePeriod` = 20
  - `StopLossFactor` = 1.5
  - `RiskRewardRatio` = 5
  - `MaxTradesPerDay` = 5
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: EMA, RSI, ATR, Volume SMA
  - Stops: ATR-Stop-Loss, 1:5-Take-Profit
  - Komplexität: Niedrig
  - Zeitrahmen: 5m oder 15m
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
