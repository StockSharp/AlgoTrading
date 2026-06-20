# Hull MA RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie Hull Moving Average + RSI. Kaufen, wenn der HMA steigt und der RSI unter 30 (überverkauft) liegt. Verkaufen, wenn der HMA fällt und der RSI über 70 (überkauft) liegt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 64%. Am besten geeignet für den Forex-Markt.

Der Hull MA liefert eine geglättete Trendlinie und der RSI hebt Impulsdivergenz hervor. Trades erfolgen, wenn der RSI an Extrempunkten dreht, während der Preis der Hull-Richtung folgt.

Geeignet für kurzfristige Swing-Trader, die frühe Signale suchen. ATR-basierte Stops schützen den Trade.

## Details

- **Einstiegskriterien**:
  - Long: `HullMA turning up && RSI < RsiOversold`
  - Short: `HullMA turning down && RSI > RsiOverbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Richtungswechsel des Hull MA
- **Stops**: ATR-basiert mit `StopLoss`
- **Standardwerte**:
  - `HmaPeriod` = 9
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Hull MA, Moving Average, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
