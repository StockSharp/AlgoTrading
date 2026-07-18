# Aufeinanderfolgende Bearish-Kerzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Geht nach einer Reihe von bearishen Kerzen long und steigt aus, wenn der Kurs das vorherige Hoch überschreitet.

Dieser Mean-Reversion-Ansatz kauft nach übermäßigem Abwärtsdruck und sucht eine Erholung, sobald die Verkäufer erschöpft sind.

## Details

- **Einstiegskriterien**: `N` aufeinanderfolgende bearishe Kerzen innerhalb des Zeitfensters.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Schlusskurs über dem vorherigen Hoch.
- **Stops**: Nein.
- **Standardwerte**:
  - `Lookback` = 3
  - `CandleType` = TimeSpan.FromDays(1)
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: Price Action
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
