# AcceleratorBot USDJPY H4 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die AcceleratorBot-Strategie ist eine Konvertierung des originalen MQL4-Experten für USDJPY auf dem H4-Zeitrahmen. Sie kombiniert Trendstärke aus dem Average Directional Index (ADX), Momentum aus dem Stochastic Oscillator und Multi-Timeframe-Werte des Acceleration/Deceleration (AC). Kerzenmuster werden als Richtungsfilter verwendet.

## Details

- **Einstiegskriterien**: Trend- oder Momentum-Signale, bestätigt durch Kerzenfilter.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal, Stop-Loss, Take-Profit oder Trailing-Stop.
- **Stops**: Fest und Trailing.
- **Standardwerte**:
  - `StopLossPoints` = 750
  - `TakeProfitPoints` = 9999
  - `TrailPoints` = 0
  - `AdxPeriod` = 14
  - `AdxThreshold` = 20m
  - `X1` = 0
  - `X2` = 150
  - `X3` = 500
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend und Momentum
  - Richtung: Beide
  - Indikatoren: ADX, Stochastic, AC
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: H4
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
