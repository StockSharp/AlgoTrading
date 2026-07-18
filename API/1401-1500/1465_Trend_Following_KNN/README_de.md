# Trendfolge-Strategie KNN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trend Following KNN ist eine vereinfachte Strategie, die die durchschnittliche Preisänderung über ein Fenster misst und den Preis mit einem gleitenden Durchschnitt vergleicht.
Sie kauft, wenn die durchschnittliche Änderung positiv und der Preis über dem gleitenden Durchschnitt liegt, und verkauft, wenn die durchschnittliche Änderung negativ und der Preis darunter liegt.

## Details

- **Einstiegskriterien**: positive/negative durchschnittliche Änderung mit Preis über/unter dem gleitenden Durchschnitt
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `WindowSize` = 20
  - `MaLength` = 50
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
