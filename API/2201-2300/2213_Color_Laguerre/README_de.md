# Strategie Color Laguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie basierend auf dem Color Laguerre-Oszillator.

Der Color Laguerre-Oszillator glättet die Preisreihe mithilfe eines Laguerre-Filters und kennzeichnet die Trendrichtung durch Farbwechsel. Die Strategie kauft, wenn der Oszillator bullisch wird, und verkauft, wenn er bärisch wird. Extreme Niveaus können Ausstiege erzwingen, wenn das Preismomentum nachlässt.

## Details

- **Einstiegskriterien**: Oszillator kreuzt das mittlere Niveau.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `Gamma` = 0.7m
  - `HighLevel` = 85
  - `MiddleLevel` = 50
  - `LowLevel` = 15
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Oszillator
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

