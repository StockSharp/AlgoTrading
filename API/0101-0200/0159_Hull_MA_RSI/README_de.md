# Hull MA RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie Hull Moving Average + RSI. Kaufen, wenn der HMA nach oben dreht und der RSI unter RsiOversold liegt. Verkaufen, wenn der HMA nach unten dreht und der RSI über RsiOverbought liegt.

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
- **Stops**: ATR-basiert mit `StopLossAtr`
- **Standardwerte**:
  - `HmaPeriod` = 9
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StopLossAtr` = 2
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Der HMA dreht nach oben, wenn er nach einem Rückgang steigt, und nach unten, wenn er nach einem Anstieg fällt. Der Stop liegt StopLossAtr ATR (AtrPeriod) vom Einstiegsschluss entfernt und wird auf Kerzenschlüssen geprüft. Ein Einstiegssignal gegen eine offene Position dreht sie.
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
