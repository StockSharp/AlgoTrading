# Turtle Trader V1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Turtle Trader V1 kombiniert mehrere Momentum-Oszillatoren mit einem gleitenden Durchschnitt als Filter. Eine Long-Position wird eröffnet, wenn der schnelle EMA über dem langsamen EMA liegt und RSI, Stochastik, CCI, Momentum und der Chaikin-Oszillator alle nach oben zeigen. Short-Positionen erfordern die umgekehrten Bedingungen.

## Details

- **Einstiegskriterien**:
  - Schneller EMA über langsamem EMA (darunter für Shorts)
  - RSI steigend und unter 70 für Longs, RSI fallend und über 30 für Shorts
  - Stochastik %K unter 88 für Longs, über 12 für Shorts
  - CCI und Momentum steigend für Longs, fallend für Shorts
  - Chaikin-Oszillator bewegt sich in Handelsrichtung
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: standardmäßig keine
- **Standardwerte**:
  - `FastMaPeriod` = 10
  - `SlowMaPeriod` = 50
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `CciPeriod` = 20
  - `MomentumPeriod` = 10
  - `ChoFastPeriod` = 3
  - `ChoSlowPeriod` = 10
- **Filter**:
  - Kategorie: Trendfolge / Momentum
  - Richtung: Beide
  - Indikatoren: EMA, RSI, Stochastic, CCI, Momentum, Chaikin Oscillator
  - Stops: Keine
  - Komplexität: Fortgeschritten
  - Zeitrahmen: 1 Stunde
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
