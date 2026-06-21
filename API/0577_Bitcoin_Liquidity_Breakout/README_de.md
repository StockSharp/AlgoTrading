# Bitcoin Liquiditäts-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht Long-Positionen ein, wenn Liquidität und Volatilität hoch sind und der kurzfristige Trend bullisch ist. Hohe Liquidität ist definiert als Volumen über seinem gleitenden Durchschnitt multipliziert mit einem Schwellenwert. Volatilität wird bestätigt, wenn ATR seinen gleitenden Durchschnitt überschreitet.

## Details

- **Einstiegskriterien**:
  - `Volumen > SMA(Volumen) * LiquidityThreshold`
  - `Preisänderung (%) > PriceChangeThreshold`
  - `Schneller SMA > Langsamer SMA`
  - `RSI < 65`
  - `ATR > SMA(ATR,10)`
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Schneller SMA kreuzt langsamen SMA nach unten oder RSI > 70.
- **Stops**: Optionale Stop-Loss- und Take-Profit-Prozentsätze.
- **Standardwerte**:
  - `LiquidityThreshold` = 1.3
  - `PriceChangeThreshold` = 1.5
  - `VolatilityPeriod` = 14
  - `LiquidityPeriod` = 20
  - `FastMaPeriod` = 9
  - `SlowMaPeriod` = 21
  - `RsiPeriod` = 14
  - `StopLossPercent` = 0.5
  - `TakeProfitPercent` = 7
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long
  - Indikatoren: SMA, RSI, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: 1h
