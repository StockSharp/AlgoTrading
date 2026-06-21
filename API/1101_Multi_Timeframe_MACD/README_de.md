# Multi-Timeframe MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Multi-Timeframe MACD kombiniert MACD-Signale aus dem Arbeitszeitrahmen und einem höheren Zeitrahmen. Einstiege erfolgen, wenn beide Zeitrahmen durch Linienkreuzungen oder Nulllinienkreuzungen übereinstimmen.

## Details
- **Daten**: Preiskerzen aus zwei Zeitrahmen.
- **Einstiegskriterien**:
  - **Long**: Abhängig vom Parameter `Entry`. Standardmäßig bullisches Crossover in beiden Zeitrahmen.
  - **Short**: Gegenteil von Long.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Trailing-Stop.
- **Stops**: Optionaler Trailing-Stop.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = tf(5)
  - `HigherCandleType` = tf(1d)
  - `ShowCurrentTimeframe` = true
  - `ShowHigherTimeframe` = true
  - `Entry` = Crossover
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 2
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long & Short
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Multi-Timeframe (5m/1d)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
