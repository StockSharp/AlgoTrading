# Getrennter Gleitender Durchschnitt
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Erstellt separate gleitende Durchschnitte für bullische und bärische Schlusskurse. Eine Long-Position wird eröffnet, wenn der bullische Durchschnitt über den bärischen steigt, und eine Short-Position beim umgekehrten Kreuz. Die Strategie unterstützt SMA, EMA oder HMA und kann mit Heikin-Ashi-Kursen arbeiten.

## Details

- **Einstiegskriterien**: Bullischer Durchschnitt kreuzt über den bärischen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Umgekehrter Kreuzung.
- **Stops**: Nein.
- **Standardwerte**:
  - `MaType` = MaType.SMA
  - `Length` = 20
  - `UseHeikinAshi` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA, EMA, HMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

