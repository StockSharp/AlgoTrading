# Korrelationsarrays-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet eine gleitende Korrelationsmatrix für bis zu sechs Wertpapiere. Sie protokolliert Korrelationsniveaus mithilfe konfigurierbarer Schwellenwerte, um die Beziehungen zwischen Vermögenswerten zu bewerten. Die Strategie dient nur der Analyse und führt keine Trades durch.

## Details
- **Einstiegskriterien**: Keine (nur Analyse)
- **Long/Short**: Keine
- **Ausstiegskriterien**: Keine
- **Stops**: Keine
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `LookbackPeriod` = 100
  - `PositiveWeak` = 0.3
  - `PositiveMedium` = 0.5
  - `PositiveStrong` = 0.7
  - `NegativeWeak` = -0.3
  - `NegativeMedium` = -0.5
  - `NegativeStrong` = -0.7
- **Filter**:
  - Kategorie: Statistische Analyse
  - Richtung: Keine
  - Indikatoren: Korrelation
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
