# Chaikin-Momentum-Scalping-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Scalping-Strategie verwendet den Chaikin-Oszillator, um Momentum-Wechsel zu erfassen. Long-Trades entstehen, wenn der Oszillator über null kreuzt und der Preis über dem 200-Perioden-SMA liegt. Short-Trades entstehen bei einem Kreuz unter null mit dem Preis unter dem SMA. ATR-Vielfache definieren Stop-Loss- und Take-Profit-Niveaus.

## Details

- **Einstiegskriterien**: Chaikin-Oszillator kreuzt über/unter null bei Preis über/unter dem SMA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: ATR-basierter Stop-Loss und Take-Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastLength` = 3
  - `SlowLength` = 10
  - `SmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplierSL` = 1.5m
  - `AtrMultiplierTP` = 2.0m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Chaikin Oscillator, SMA, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
