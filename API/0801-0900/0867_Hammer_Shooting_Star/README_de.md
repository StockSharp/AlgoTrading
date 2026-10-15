# Hammer & Shooting Star-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Hammer- und Shooting-Star-Kerzenmuster.
Eine Long-Position wird eröffnet, wenn die vorherige Kerze ein Hammer ist,
während eine Short-Position auf einen Shooting Star folgt.
Ausstiege nutzen das Hoch und Tief der Signalkerze als Take-Profit und Stop-Loss.

## Details

- **Einstiegskriterien**:
  - Long: vorherige Kerze ist ein Hammer
  - Short: vorherige Kerze ist ein Shooting Star
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss und Take-Profit am Tief/Hoch der vorherigen Kerze
- **Stops**: Ja, fest an den Extremen der Signalkerze
- **Standardwerte**:
  - `WickFactor` = 0.9
  - `MaxOppositeWickFactor` = 0.45
  - `MinBodyRangePct` = 0.2
  - `CandleType` = 1 Minute
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Kerzen
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
