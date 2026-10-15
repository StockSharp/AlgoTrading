# Angebot-Nachfrage-Engulfment-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die bullische und bärische Engulfing-Muster in der Nähe von Donchian-Unterstützungs- und Widerstandszonen handelt.

## Details

- **Einstiegskriterien**: Engulfing-Muster an Zonengrenzen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `ZonePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Donchian
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja (engulfing)
  - Risikolevel: Mittel
