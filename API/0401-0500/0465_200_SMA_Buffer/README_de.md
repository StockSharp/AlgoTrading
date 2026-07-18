# 200-SMA-Buffer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die 200-SMA-Buffer-Strategie handelt auf Basis des Abstands des Preises von einem langfristigen einfachen gleitenden Durchschnitt. Sie kauft, wenn der Schlusskurs einen bestimmten Prozentsatz über der SMA steigt, und steigt aus, wenn der Preis einen definierten Prozentsatz darunter fällt. Der Ansatz zielt darauf ab, langfristigen Momentum zu erfassen und dabei einen Puffer um den gleitenden Durchschnitt zu erlauben.

## Details

- **Einstiegskriterien**:
  - Schlusskurs > SMA * (1 + Entry %).
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Schlusskurs < SMA * (1 - Exit %).
- **Stops**: Keine.
- **Standardwerte**:
  - `SmaLength` = 200
  - `EntryPercent` = 5
  - `ExitPercent` = 3
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
