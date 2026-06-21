# Moving-Average-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kauft, wenn der kurze SMA den langen SMA von unten kreuzt, und verkauft, wenn er ihn von oben kreuzt. Positionen werden bei entgegengesetzten Signalen umgekehrt.

## Details

- **Einstiegskriterien**:
  - Long, wenn der kurze SMA den langen SMA nach oben kreuzt.
  - Short, wenn der kurze SMA den langen SMA nach unten kreuzt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Umkehr bei entgegengesetztem Crossover.
- **Stops**: Nein.
- **Standardwerte**:
  - `ShortLength` = 9
  - `LongLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Crossover
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
