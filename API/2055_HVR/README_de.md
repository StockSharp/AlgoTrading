# HVR-Strategie (Historisches Volatilitätsverhältnis)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Historical Volatility Ratio (HVR). Sie vergleicht die kurzfristige Volatilität über 6 Bars mit der langfristigen Volatilität über 100 Bars anhand von logarithmischen Renditen. Wenn das Verhältnis über den Schwellenwert steigt, geht das System long und erwartet eine Volatilitätsausweitung. Wenn es unter den Schwellenwert fällt, geht das System short.

## Details

- **Einstiegskriterien**:
  - Long: `HVR > RatioThreshold`
  - Short: `HVR < RatioThreshold`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `ShortPeriod` = 6
  - `LongPeriod` = 100
  - `RatioThreshold` = 1.0
  - `CandleType` = `TimeSpan.FromMinutes(15).TimeFrame()`
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Beide
  - Indikatoren: Historische Volatilität (kurz und lang)
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
