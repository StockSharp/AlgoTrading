# Long Explosive V1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Long Explosive V1 eröffnet eine Long-Position, wenn der Schlusskurs um einen definierten Prozentsatz gegenüber der vorherigen Kerze ansteigt. Die Position wird geschlossen, wenn der Kurs um den konfigurierten Prozentsatz fällt oder bevor ein neuer Long-Handel eröffnet wird.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close - PrevClose > Close * Price increase (%) / 100`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: `Close - PrevClose < -Close * Price decrease (%) / 100` oder vor einem neuen Long-Einstieg.
- **Stops**: Keine.
- **Standardwerte**:
  - `Price increase (%)` = 1
  - `Price decrease (%)` = 1
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: Preis
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
