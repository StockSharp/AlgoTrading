# DCA Unterstützung und Widerstand mit RSI und Trendfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dollar-Cost-Averaging-Strategie unter Verwendung von Unterstützungs-/Widerstandsniveaus, RSI und EMA-Trendfilter. Kauft an der Unterstützung in einem Aufwärtstrend, wenn der RSI überverkauft ist, und verkauft am Widerstand in einem Abwärtstrend, wenn der RSI überkauft ist.

## Details

- **Einstiegskriterien**:
  - Long: Preis an der Unterstützung, RSI unter überverkauft, über EMA
  - Short: Preis am Widerstand, RSI über überkauft, unter EMA
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: Preis erreicht Widerstand oder RSI über überkauft
  - Short: Preis erreicht Unterstützung oder RSI unter überverkauft
- **Stops**: Keine
- **Standardwerte**:
  - `LookbackPeriod` = 50
  - `RsiLength` = 14
  - `Overbought` = 70
  - `Oversold` = 40
  - `EmaPeriod` = 200
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: RSI, EMA, Highest, Lowest
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
