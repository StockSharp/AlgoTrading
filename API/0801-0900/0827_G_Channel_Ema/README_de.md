# G-Channel mit EMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die die G-Channel-Kanallogik mit einem EMA-Trendfilter kombiniert.

Kauft, wenn der letzte Kreuzungspunkt abwärts war und der Preis unter der EMA liegt. Verkauft, wenn der letzte Kreuzungspunkt aufwärts war und der Preis über der EMA liegt.

## Details

- **Einstiegskriterien**: G-Channel-Zustand mit EMA-Filter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensignal.
- **Stops**: Nein.
- **Standardwerte**:
  - `ChannelLength` = 100
  - `EmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: G-Channel, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
