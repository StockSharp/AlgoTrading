# Fearzone Panel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie inspiriert vom FearZone-Panel aus «Framgångsrik Aktiehandel». Sie sucht nach Panikverkäufen, bei denen Angst dominiert.

Die Strategie wartet darauf, dass beide Fearzone-Indikatoren aktiv sind und mindestens ein Panik-Auslöser vorliegt, während der Preis über dem 200-Perioden gleitenden Durchschnitt bleibt.

## Details

- **Einstiegskriterien**: FZ1 und FZ2 aktiv plus negativer Impuls, Ricochет-Zone oder Stochastik überverkauft, mit Schlusskurs über MA200.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Preis fällt unter MA200.
- **Stops**: Nein.
- **Standardwerte**:
  - `LookbackPeriod` = 22
  - `BollingerPeriod` = 200
  - `ImpulsePeriod` = 10
  - `ImpulsePercent` = 0.1m
  - `MaPeriod` = 200
  - `StochThreshold` = 30m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Nur Long
  - Indikatoren: BollingerBands, RateOfChange, StochasticOscillator, SimpleMovingAverage, Highest
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
