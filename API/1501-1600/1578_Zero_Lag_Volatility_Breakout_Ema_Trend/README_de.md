# Zero-lag Volatilität-Ausbruch EMA-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruchssystem, das die Zero-Lag-EMA-Differenz mit Bollinger-Bändern und einem EMA-Trendfilter verwendet. Positionen können optional bis zu einem Gegensignal gehalten werden.

## Details

- **Einstiegskriterien**: Dif kreuzt über das obere Band mit EMA-Steigungsfilter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Optionaler Ausstieg bei Mittelbandkreuzung.
- **Stops**: Keine expliziten Stops.
- **Standardwerte**:
  - `EmaLength` = 200
  - `StdMultiplier` = 2m
  - `UseBinary` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, Bollinger Bands
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
