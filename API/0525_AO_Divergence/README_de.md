# AO Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie sucht nach bullischen und bärischen Divergenzen zwischen dem Awesome Oscillator (AO) und dem Preis. Eine bullische Divergenz tritt auf, wenn der Preis ein niedrigeres Tief bildet, während der AO ein höheres Tief bildet. Eine bärische Divergenz erscheint, wenn der Preis ein höheres Hoch bildet, während der AO ein niedrigeres Hoch bildet.

Wird eine bullische Divergenz erkannt, eröffnet die Strategie eine Long-Position. Eine bärische Divergenz löst eine Short-Position aus. Positionen werden bei entgegengesetzten Signalen umgekehrt.

## Details

- **Einstiegskriterien**: Bullische oder bärische AO-Divergenz mit dem Preis.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Divergenzsignal.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = 5 Minuten
  - `FastLength` = 5
  - `SlowLength` = 34
  - `Lookback` = 5
  - `UseEma` = false
- **Filter**:
  - Kategorie: Indikator
  - Richtung: Beide
  - Indikatoren: Awesome Oscillator
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
