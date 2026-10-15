# Long EMA mit erweitertem Ausstieg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Long EMA mit erweitertem Ausstieg ist eine reine Long-Strategie, die einsteigt, wenn ein kurzfristiger gleitender Durchschnitt einen mittelfristigen nach oben kreuzt und der Kurs über einem langfristigen gleitenden Durchschnitt liegt. Ausstiege können durch MACD-Kreuz nach unten, Kursschluss unter einem ausgewählten gleitenden Durchschnitt, MA-Kreuz nach unten, Trailing-Stop oder einen ATR-basierten Volatilitätsfilter ausgelöst werden.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: Kurzfristige MA kreuzt mittelfristige MA nach oben und Kurs liegt über der langfristigen MA.
- **Ausstiegskriterien**: MACD-Kreuz nach unten, Kurs unter gewählter MA, kurzfristige MA kreuzt mittelfristige MA nach unten, optionaler Trailing-Stop.
- **Stops**: Optionaler Trailing-Stop.
- **Standardwerte**:
  - `MaType` = EMA
  - `EntryConditionType` = Crossover
  - `LongTermPeriod` = 200
  - `ShortTermPeriod` = 5
  - `MidTermPeriod` = 10
  - `EnableMacdExit` = true
  - `MacdCandleType` = TimeSpan.FromDays(7).TimeFrame()
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 15
  - `UseMaCloseExit` = false
  - `MaCloseExitPeriod` = 50
  - `UseMaCrossExit` = true
  - `UseVolatilityFilter` = false
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: MA, MACD, ATR
  - Komplexität: Mittel
  - Risikolevel: Mittel
