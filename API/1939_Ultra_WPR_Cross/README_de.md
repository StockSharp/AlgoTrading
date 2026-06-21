# Ultra-WPR-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen Williams %R-Oszillator, der durch zwei gleitende Durchschnitte geglättet wird. Die Kreuzung der schnellen und langsamen geglätteten Linien erzeugt Handelssignale. Eine Long-Position wird eröffnet, wenn die schnelle Linie über die langsame Linie steigt, und eine Short-Position, wenn die schnelle Linie unter die langsame Linie fällt.

Der Ansatz verfolgt entstehendes Momentum und begrenzt dabei das Risiko mit konfigurierbaren Take-Profit- und Stop-Loss-Niveaus.

## Details
- **Einstiegskriterien**:
  - **Long**: Schnelle Linie kreuzt über langsame Linie
  - **Short**: Schnelle Linie kreuzt unter langsame Linie
- **Long/Short**: Beide Seiten
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn schnelle Linie unter langsame Linie kreuzt
  - **Short**: Ausstieg wenn schnelle Linie über langsame Linie kreuzt
- **Stops**: Ja, preisbasierter Take-Profit und Stop-Loss
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromHours(4)
  - `WprPeriod` = 13
  - `FastLength` = 3
  - `SlowLength` = 53
  - `TakeProfit` = 0.2m
  - `StopLoss` = 0.1m
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Williams %R, Moving Average
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: H4
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
