# OBV ATR Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verfolgt das On-Balance Volume (OBV) und eröffnet Trades, wenn OBV sein jüngstes Hoch oder Tief durchbricht. Sie pflegt einen dynamischen Kanal ähnlich einem ATR-Ausbruch und wechselt zwischen bullischen und bärischen Modi.

## Details

- **Einstiegskriterien**: OBV kreuzt über das vorherige Hoch für Long; kreuzt unter das vorherige Tief für Short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensignal oder Schutzaufträge.
- **Stops**: Ja.
- **Standardwerte**:
  - `LookbackLength` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: OBV, Highest, Lowest
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
