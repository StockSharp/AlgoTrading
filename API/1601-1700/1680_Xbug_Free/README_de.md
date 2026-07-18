# Xbug Free-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konträre Moving-Average-Strategie, die kauft, wenn der Preis unter seinen gleitenden Durchschnitt kreuzt, und verkauft, wenn der Preis darüber kreuzt. Verwendet symmetrische Take-Profit- und Stop-Loss-Abstände.

## Details

- **Einstiegskriterien**: Preis kreuzt unter/über den einfachen gleitenden Durchschnitt
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensignal oder Schutz-Stop
- **Stops**: Ja
- **Standardwerte**:
  - `MaPeriod` = 19
  - `MaShift` = 15
  - `StopPoints` = 270
  - `Volume` = 0.1
  - `CandleType` = 4-hour
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: MA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Swing
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
