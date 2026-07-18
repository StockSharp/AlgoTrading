# US-Index Erste-30m-Kerze-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt den Ausbruch aus der ersten 30-Minuten-Range der US-Sitzung mit einem Trade pro Tag.

## Details

- **Einstiegskriterien**: Nachdem die erste 30m-Range festgelegt ist, bricht der Preis über das Hoch oder unter das Tief
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop auf der gegenüberliegenden Range-Grenze, Ziel bei Range-Größe * Risiko/Ertrag
- **Stops**: Ja
- **Standardwerte**:
  - `RiskReward` = 1
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
