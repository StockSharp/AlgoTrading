# PS Januar-Barometer-Backtester-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert den Januar-Barometer, bei dem eine Long-Position eingegangen wird, wenn der Schlusskurs von Februar bis Juni das Januar-Hoch überschreitet. Optionale Filter erfordern eine positive Santa-Claus-Rally und/oder eine positive Rendite der ersten fünf Handelstage.

## Details

- **Einstiegskriterien**: Schlusskurs von Februar bis Juni über dem Januar-Hoch mit optionalen saisonalen Filtern
- **Long/Short**: Long
- **Ausstiegskriterien**: Position im Dezember schließen
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = 1 month
  - `UseSantaClausRally` = false
  - `UseFirstFiveDays` = false
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Nur Long
  - Indikatoren: Saisonalität
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Monatlich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
