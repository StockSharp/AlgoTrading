# Pairs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Pairs-Trading-Strategie kauft, wenn der Referenzwert über seinem Eröffnungskurs schließt, während das aktuelle Symbol eine Abwärtskerze bildet. Die Position wird geschlossen, wenn der Kurs über das Hoch der vorherigen Kerze bricht.

## Details

- **Einstiegskriterien**: Referenzwert aufwärts und aktuelle Kerze abwärts.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Schlusskurs über vorherigem Hoch.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = 1 Minute
- **Filter**:
  - Kategorie: Pair-Trading
  - Richtung: Nur Long
  - Indikatoren: Price action
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
