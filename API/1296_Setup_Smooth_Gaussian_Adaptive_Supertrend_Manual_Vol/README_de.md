# Setup: Smooth Gaussian + Adaptive Supertrend (Manual Vol) — Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eröffnet eine Long-Position, wenn der Schlusskurs über einem doppelt geglätteten gleitenden Durchschnitt (Gaussian-Trend) liegt.
Schließt die Position, wenn der Preis unter die Trendlinie fällt. Ein einfacher manueller Volatilitätsfilter kann Einstiege einschränken.

## Details

- **Einstiegskriterien**: Schlusskurs über der Trendlinie und (Volatilitätsfilter deaktiviert oder Volatilität ist 2 oder 3).
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Schlusskurs unter der Trendlinie.
- **Stops**: Keine.
- **Standardwerte**:
  - `TrendLength` = 75
  - `Volatility` = 2
  - `EnableVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
