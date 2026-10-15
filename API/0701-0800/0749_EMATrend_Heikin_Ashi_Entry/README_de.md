# EMA-Trend-Heikin-Ashi-Einstiegs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Bollinger-Bänder auf Heikin-Ashi-Kerzen mit einem EMA-Trendfilter auf dem übergeordneten Zeitrahmen verwendet. Kauft nach aufeinanderfolgenden bärischen Heikin-Ashi-Kerzen, die das untere Band berühren, gefolgt von einer bullischen Kerze darüber, wenn die schnelle EMA des übergeordneten Zeitrahmens über der langsamen EMA liegt. Verkauft umgekehrt.

Nach dem Einstieg wird ein erstes Ziel in Höhe des Risikos genommen und der Stop anhand der Extrema der vorherigen Kerze nachgezogen.

## Details

- **Einstiegskriterien**:
  - Long: mindestens zwei bärische HA-Kerzen am unteren Band, dann bullische darüber mit schneller EMA des übergeordneten Zeitrahmens über langsamer EMA
  - Short: mindestens zwei bullische HA-Kerzen am oberen Band, dann bärische darunter mit schneller EMA des übergeordneten Zeitrahmens unter langsamer EMA
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: erstes Ziel bei 1R, dann Trailing-Stop an vorherigen Tiefs
  - Short: erstes Ziel bei 1R, dann Trailing-Stop an vorherigen Hochs
- **Stops**: Vorheriges Kerzentief/-hoch
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `HigherTimeframe` = TimeSpan.FromMinutes(180).TimeFrame()
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Heikin Ashi, EMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
