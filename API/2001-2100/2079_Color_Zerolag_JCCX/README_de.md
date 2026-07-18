# Color Zerolag JCCX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie inspiriert vom ColorZerolagJCCX-Indikator aus MetaTrader. Sie approximiert den originalen Oszillator mit zwei einfachen gleitenden Durchschnitten.
Die Strategie geht Long, wenn der schnelle Durchschnitt den langsamen von oben nach unten kreuzt, und Short, wenn der schnelle Durchschnitt den langsamen von unten nach oben kreuzt.

## Details

- **Einstiegskriterien**:
  - Long: `Schneller MA kreuzt langsamen MA von oben nach unten`
  - Short: `Schneller MA kreuzt langsamen MA von unten nach oben`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: `StartProtection()`
- **Standardwerte**:
  - `FastPeriod` = 8
  - `SlowPeriod` = 21
  - `CandleType` = 4-Stunden-Kerzen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Gleitender Durchschnitt
  - Stops: Optional
  - Komplexität: Grundlegend
  - Zeitrahmen: Swing
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
