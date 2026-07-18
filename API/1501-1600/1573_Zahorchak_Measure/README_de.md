# Zahorchak Measure-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Berechnet einen gewichteten Score mithilfe mehrerer gleitender Durchschnitte. Kauft, wenn der Score positiv wird, und verkauft, wenn er negativ wird.

## Details

- **Einstiegskriterien**: Score kreuzt über null
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegenläufiges Signal
- **Stops**: Nein
- **Standardwerte**:
  - `Points` = 1
  - `EmaLength` = 10
- **Filter**:
  - Kategorie: Marktbreite
  - Richtung: Beide
  - Indikatoren: SMA, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
