# Lineare Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Lineare Mean-Reversion-Strategie verwendet den Z-Score des Preises relativ zu einem gleitenden Durchschnitt, um Mean Reversion mit einem festen Stop Loss in Punkten zu handeln.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: z-score < -EntryThreshold.
  - **Short**: z-score > EntryThreshold.
- **Ausstiegskriterien**: Z-Score kehrt in Richtung null zurück (z-score > -ExitThreshold für Longs, z-score < ExitThreshold für Shorts).
- **Stops**: Fester Stop Loss in Punkten.
- **Standardwerte**:
  - `HalfLife` = 14
  - `Scale` = 1
  - `EntryThreshold` = 2
  - `ExitThreshold` = 0.2
  - `StopLossPoints` = 50
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long & Short
  - Indikatoren: SMA, StandardDeviation
  - Stops: Ja
  - Komplexität: Niedrig
  - Risikolevel: Mittel
