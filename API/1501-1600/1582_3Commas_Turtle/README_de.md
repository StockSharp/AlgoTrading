# 3Commas Turtle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Vereinfachtes Turtle-Ausbruchssystem mit Donchian-Kanälen. Kauft bei Ausbrüchen über den schnellen Kanal, wenn der Preis auch über dem langsamen Kanal liegt, und verkauft bei Ausbrüchen unter den schnellen Kanal mit Bestätigung durch den langsamen Kanal. Ausstiege erfolgen, wenn der Preis den Austrittskanal in der entgegengesetzten Richtung kreuzt.

## Details
- **Einstiegskriterien**: Ausbruch des schnellen Kanals mit Bestätigung durch den langsamen Kanal.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis kreuzt Austrittskanal.
- **Stops**: Kanalbasiert.
- **Standardwerte**:
  - `PeriodFast` = 20
  - `PeriodSlow` = 20
  - `PeriodExit` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Donchian-Kanäle
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
