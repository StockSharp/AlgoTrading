# Intelle city World Cycle ATH ATL Logarithmische Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet skalierte gleitende Durchschnitte, um Signale beim Allzeithoch (ATH) und Allzeittief (ATL) basierend auf dem Pi-Cycle-Konzept zu markieren.

Das System verkauft, wenn der skalierte lange ATH-MA den kurzen MA nach unten kreuzt, und kauft, wenn der skalierte lange ATL-MA den kurzen MA nach oben kreuzt.

## Details

- **Einstiegskriterien**: Skalierter langer ATH-SMA kreuzt den kurzen ATH-SMA nach unten für Verkauf. Skalierter langer ATL-SMA kreuzt den kurzen ATL-SMA nach oben für Kauf.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `AthLongLength` = 350
  - `AthShortLength` = 111
  - `AtlLongLength` = 471
  - `AtlShortLength` = 150
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
