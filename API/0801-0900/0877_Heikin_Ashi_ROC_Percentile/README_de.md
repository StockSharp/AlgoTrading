# Heikin Ashi ROC-Perzentil-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie wandelt Kerzen in Heikin Ashi um, glättet den Schlusskurs mit einem SMA und misst dessen Rate of Change. Perzentil-Bänder der jüngsten ROC-Hochs und -Tiefs bilden Ausbruchsniveaus. Ein Überschreiten des unteren Bandes öffnet oder dreht die Long-Position um, während ein Unterschreiten des oberen Bandes auf Short dreht.

## Details

- **Einstiegskriterien**:
  - Long: ROC überschreitet die untere Perzentillinie.
  - Short: ROC unterschreitet die obere Perzentillinie.
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Prozentualer Stop.
- **Standardwerte**:
  - `RocLength` = 100
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
  - `StartDate` = new DateTimeOffset(2015, 3, 3, 0, 0, 0, TimeSpan.Zero)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Heikin Ashi, RateOfChange, Highest, Lowest
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
