# Zero-Lag MA Trendfolge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolgesystem, das wartet, bis ein Zero-Lag-MA eine EMA kreuzt, und dann bei einem Preisausbruch aus einer ATR-großen Box einsteigt. Positionen beinhalten risikobasierte Kursziele.

## Details

- **Einstiegskriterien**: Zero-Lag-MA-Kreuzung und Box-Ausbruch.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-basierter Stop oder Take Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `Length` = 34
  - `AtrPeriod` = 14
  - `RiskReward` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ZLEMA, EMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
