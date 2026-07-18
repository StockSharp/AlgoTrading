# Konfigurierbare BTC-Saisonalitäts-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie nutzt die Intraday-Saisonalität von Bitcoin, indem sie zu benutzerdefinierten UTC-Stunden ein- und aussteigt.
Eine Long-Position wird zur Einstiegsstunde eröffnet und zur Ausstiegsstunde geschlossen.

## Details

- **Einstiegskriterien**: Zeit entspricht der benutzerdefinierten Einstiegsstunde
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Zeit entspricht der benutzerdefinierten Ausstiegsstunde
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = 1 Minute
  - `EntryHour` = 21
  - `ExitHour` = 23
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Nur Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
