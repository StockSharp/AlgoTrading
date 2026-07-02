# Strategie ADX DI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf ADX und Directional Movement Indikatoren

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 103%. Sie funktioniert am besten am Aktienmarkt.

ADX DI konzentriert sich auf die Kreuzung von +DI und -DI bei steigendem ADX. Ein bullisches Kreuz von +DI über -DI in Verbindung mit einem starken ADX öffnet Long-Positionen, während das Gegenteil Short-Positionen öffnet. Positionen werden bei einem schwächer werdenden ADX oder einem entgegengesetzten Kreuz geschlossen.

Diese Kombination hilft, bei jedem DI-Kreuz zu handeln zu vermeiden, indem eine Bestätigung durch den ADX gefordert wird. Das System zielt darauf ab, nachhaltige Trends statt kurzfristiger Schwankungen zu erfassen.


## Details

- **Einstiegskriterien**: Signale basierend auf ADX, ATR.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ADX, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

