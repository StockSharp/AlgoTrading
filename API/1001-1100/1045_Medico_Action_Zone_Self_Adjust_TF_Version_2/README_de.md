# Medico Action Zone Self Adjust TF Version 2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA-Crossover-Strategie mit Bestätigung durch einen höheren Zeitrahmen. Eine Position wird eröffnet, wenn die schnelle EMA die langsame EMA kreuzt und der Schlusskurs des höheren Zeitrahmens über der schnellen EMA liegt. Bei gegenteiligem Signal wird die Position umgekehrt.

## Details

- **Einstiegskriterien**: Schnelle EMA kreuzt über die langsame EMA, während der Schlusskurs des höheren Zeitrahmens über der schnellen EMA liegt.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegenteiliger Crossover mit Bestätigung.
- **Stops**: Keine.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
