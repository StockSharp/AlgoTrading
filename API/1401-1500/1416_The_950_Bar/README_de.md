# Die 9:50-Uhr-Bar-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt die 9:50 Uhr New Yorker Fünf-Minuten-Kerze. Nach Abschluss der Kerze wird eine Position in Richtung der Kerze eröffnet, mit festem Gewinnziel und Stop in Ticks.

## Details
- **Einstiegskriterien**: Richtung der 9:50 Uhr NY Fünf-Minuten-Bar.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Zielniveau oder Stop erreichen.
- **Stops**: Fester Stop und Ziel.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `TickSize` = 0.25
  - `TargetTicks` = 150
  - `StopTicks` = 200
- **Filter**:
  - Kategorie: Zeit
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Fest
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
