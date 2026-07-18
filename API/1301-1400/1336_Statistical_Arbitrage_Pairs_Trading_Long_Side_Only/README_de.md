# Statistischer Arbitrage-Paarhandel - Nur Long-Seite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie führt einen einfachen Paarhandelsansatz durch, der auf dem Z-Score-Spread zwischen zwei Instrumenten basiert. Sie eröffnet eine Long-Position, wenn der Spread unter einen benutzerdefinierten Schwellenwert fällt, und schließt die Position, wenn der Spread über null kreuzt.

## Details

- **Einstiegskriterien**: Spread-Z-Score unter dem Schwellenwert.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Spread-Z-Score kreuzt über null.
- **Stops**: Nein.
- **Standardwerte**:
  - `ZScoreLength` = 20
  - `ExtremeLevel` = -1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: SMA, StandardDeviation
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
