# EMA RSI Swing-Trendfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt den Crossover von EMA20 und EMA50 in Richtung des EMA200-Trendfilters.
Ein optionaler RSI-Filter begrenzt Long-Einstiege bei überkauftem RSI und Short-Einstiege bei überverkauftem RSI.

## Details

- **Einstiegskriterien**: EMA20 kreuzt EMA50 mit Kursposition relativ zu EMA200 und optionalem RSI-Filter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Optionaler Ausstieg beim gegenteiligen EMA-Crossover.
- **Stops**: Nein.
- **Standardwerte**:
  - `EmaFastPeriod` = 20
  - `EmaSlowPeriod` = 50
  - `EmaTrendPeriod` = 200
  - `RsiLength` = 14
  - `UseRsiFilter` = true
  - `RsiMaxLong` = 70
  - `RsiMinShort` = 30
  - `RequireCloseConfirm` = true
  - `ExitOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
