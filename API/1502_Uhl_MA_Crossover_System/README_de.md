# Uhl MA Crossover System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Uhl MA Crossover System baut zwei adaptive Linien (CTS und CMA) mithilfe von Varianz zur Anpassung der Glättung auf. Eine Long-Position wird eröffnet, wenn CTS CMA nach oben kreuzt, und eine Short-Position, wenn es nach unten kreuzt.

## Details

- **Einstiegskriterien**: CTS kreuzt CMA nach oben.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: CTS kreuzt CMA nach unten.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 100
  - `Multiplier` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA, Variance
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
