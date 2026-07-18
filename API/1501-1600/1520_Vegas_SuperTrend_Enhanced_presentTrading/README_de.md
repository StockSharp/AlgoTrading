# Vegas SuperTrend Enhanced presentTrading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert einen Vegas-Kanal mit einem angepassten SuperTrend.
Einstieg, wenn der SuperTrend die Richtung wechselt, mit volatilitätsbasiertem Multiplikator.

## Details

- **Einstiegskriterien**: Trendwechsel erkannt durch angepassten SuperTrend
- **Long/Short**: Beide (konfigurierbar)
- **Ausstiegskriterien**: entgegengesetzter Trendwechsel
- **Stops**: Nein
- **Standardwerte**:
  - `AtrPeriod` = 10
  - `VegasWindow` = 100
  - `SuperTrendMultiplier` = 5
  - `VolatilityAdjustment` = 5
  - `TradeDirection` = "Both"
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, SMA, StandardDeviation
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
