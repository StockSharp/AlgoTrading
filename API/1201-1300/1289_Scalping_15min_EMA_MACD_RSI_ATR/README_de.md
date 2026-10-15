# Scalping-Strategie 15m EMA MACD RSI ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Scalping-Strategie, die einen EMA-50-Trendfilter, das Momentum des MACD-Histogramms und RSI-Niveaus kombiniert. Das Risikomanagement verwendet ATR-basierte Stop-Loss- und Take-Profit-Level.

Die Strategie kauft, wenn der Kurs über der EMA liegt, das MACD-Histogramm positiv ist und der RSI zwischen 50 und dem Überkauft-Niveau liegt. Short-Positionen entstehen, wenn der Kurs unter der EMA liegt, das Histogramm negativ ist und der RSI zwischen dem Überverkauft-Niveau und 50 liegt. Stops und Ziele folgen dem Schlusskurs um ATR-Vielfache.

## Details

- **Einstiegskriterien**: Kursposition relativ zur EMA, Vorzeichen des MACD-Histogramms, RSI-Niveau.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-basierter Stop-Loss oder Take-Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `EmaPeriod` = 50
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `AtrPeriod` = 14
  - `SlAtrMultiplier` = 1m
  - `TpAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Scalping
  - Richtung: Beide
  - Indikatoren: EMA, MACD, RSI, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
