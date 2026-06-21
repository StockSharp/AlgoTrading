# PulseWave-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie unter Verwendung von VWAP, MACD-Crossover und RSI-Filter.

Die Strategie kauft, wenn der Kurs über dem VWAP liegt, der MACD die Signallinie nach oben kreuzt und der RSI unter dem Überkauft-Schwellenwert liegt. Sie steigt aus, wenn der Kurs unter den VWAP fällt, der MACD die Signallinie nach unten kreuzt und der RSI über dem Überverkauft-Schwellenwert liegt.

## Details

- **Einstiegskriterien**: Kurs über VWAP, MACD-Crossover nach oben, RSI unter Überkauft.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Kurs unter VWAP, MACD-Crossover nach unten, RSI über Überverkauft.
- **Stops**: Nein.
- **Standardwerte**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: VWAP, MACD, RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
