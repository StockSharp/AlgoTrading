# PVT-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des Crossovers des Price Volume Trend (PVT) Indikators und seiner exponentiellen gleitenden Durchschnittslinie (EMA). Eine Long-Position wird eröffnet, wenn der PVT seine EMA nach oben kreuzt, eine Short-Position, wenn er sie nach unten kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: PVT kreuzt seine EMA nach oben.
  - **Short**: PVT kreuzt seine EMA nach unten.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Position bei entgegengesetztem Signal umkehren.
- **Stops**: Nein.
- **Standardwerte**:
  - `EmaLength` = 20.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: PVT, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
