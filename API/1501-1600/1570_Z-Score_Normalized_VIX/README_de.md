# Z-Score Normalized VIX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Z-Scores mehrerer VIX-Indizes mittelt und long geht, wenn der kombinierte Wert unter einen negativen Schwellenwert fällt.

Der Algorithmus berechnet den Z-Score für VIX, VIX3M, VIX9D und VVIX. Die ausgewählten Z-Scores werden gemittelt, um einen einzigen Indikator zu bilden, der die allgemeine Volatilitätsstimmung darstellt.

## Details

- **Einstiegskriterien**: Kombinierter Z-Score unterhalb von `-Threshold`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Kombinierter Z-Score steigt über `-Threshold`.
- **Stops**: Nein.
- **Standardwerte**:
  - `ZScoreLength` = 6
  - `Threshold` = 1
  - `UseVix` = true
  - `UseVix3m` = true
  - `UseVix9d` = true
  - `UseVvix` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Long
  - Indikatoren: Z-Score
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
