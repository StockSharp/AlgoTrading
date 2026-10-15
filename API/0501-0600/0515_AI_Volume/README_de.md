# AI-Volumen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die AI-Volumen-Strategie sucht nach plötzlichen Teilnahmeausbrüchen. Ein Volumenspike tritt auf, wenn das aktuelle Volumen seine EMA um einen bestimmten Multiplikator überschreitet. Wenn der Spike mit der 50-Perioden-Preis-EMA und der Kerzenfarbe übereinstimmt, tritt die Strategie in diese Richtung ein. Jeder Trade wird nach einer festen Anzahl von Bars geschlossen.

## Details

- **Einstiegskriterien**: Volumen > VolumeEMA * VolumeMultiplier und Preis über/unter 50 EMA mit passender Kerzenfarbe.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Position nach `ExitBars` Kerzen geschlossen.
- **Stops**: Keine.
- **Standardwerte**:
  - `VolumeEmaLength` = 20
  - `VolumeMultiplier` = 2.0
  - `ExitBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Volumenausbruch
  - Richtung: Beide
  - Indikatoren: EMA, Volume EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
