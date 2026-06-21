# Trendfolge-Strategie MM3 Hoch Tief
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet einen einfachen gleitenden Durchschnitt der Hochs und Tiefs über 3 Perioden. Eine Long-Position wird eröffnet, wenn der Preis über dem SMA der Hochs schließt, und geschlossen, wenn der Preis unter den SMA der Tiefs fällt.

## Details

- **Einstiegskriterien**: Close > SMA(high).
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Close < SMA(low).
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
