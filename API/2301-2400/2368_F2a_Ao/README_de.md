# F2a AO-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den originalen MetaTrader Expert Advisor "F2a_AO". Sie filtert den Awesome Oscillator mit einer kurzen SMA und eröffnet Trades nur in Richtung einer Referenzkerze auf einem höheren Zeitrahmen.

Der Oszillator wird auf seinem eigenen Zeitrahmen berechnet. Wenn die Referenzkerze über ihrer Eröffnung schließt, löst ein positiv gefilterter AO einen Long-Einstieg aus und schließt alle Shorts. Wenn die Referenzkerze unter ihrer Eröffnung schließt, löst ein negativ gefilterter AO einen Short-Einstieg aus und schließt alle Longs.

## Details

- **Einstiegskriterien**:
  - **Long**: Referenzkerze ist bullisch und gefilterter AO > 0.
  - **Short**: Referenzkerze ist bärisch und gefilterter AO < 0.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gefilterter AO < 0 schließt Long-Positionen.
  - Gefilterter AO > 0 schließt Short-Positionen.
- **Stops**: Kein expliziter Stop-Loss oder Take-Profit, Schutzmodul ist aktiviert.
- **Standardwerte**:
  - `IndicatorTimeFrame` = 12 Stunden.
  - `TrendTimeFrame` = 1 Tag.
  - `FastPeriod` = 13.
  - `SlowPeriod` = 144.
  - `FilterLength` = 3.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Awesome Oscillator, SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
