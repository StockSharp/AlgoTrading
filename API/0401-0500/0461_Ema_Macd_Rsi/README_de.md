# EMA MACD RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den Trendfilter mit EMA, MACD-Kreuzungen und RSI-Niveaus kombiniert.

Kauft, wenn die schnelle EMA über der langsamen EMA liegt, MACD seine Signallinie nach oben kreuzt und RSI zwischen RsiBuyLevel und 70 liegt. Verkauft, wenn die schnelle EMA unter der langsamen EMA liegt, MACD seine Signallinie nach unten kreuzt und RSI zwischen 30 und RsiSellLevel liegt.

## Details

- **Einstiegskriterien**: Trendfilter mit EMA, MACD-Kreuzung, RSI-Niveau.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuyLevel` = 45m
  - `RsiSellLevel` = 55m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, MACD, RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
