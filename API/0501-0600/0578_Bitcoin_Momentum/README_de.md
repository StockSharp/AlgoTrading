# Bitcoin Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Momentum-Strategie für Bitcoin, die nur handelt, wenn der Preis über einem EMA eines höheren Zeitrahmens liegt und Vorsichtsbedingungen vermieden werden. Ein ATR-basierter Trailing-Stop schützt Gewinne.

## Details

- **Einstiegskriterien**: Preis über wöchentlichem EMA und keine Vorsichtsbedingung.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Preis unter Trailing-Stop oder wöchentlichem EMA.
- **Stops**: ATR-basierter Trailing-Stop.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `EmaLength` = 20
  - `AtrLength` = 5
  - `TrailStopLookback` = 7
  - `TrailStopMultiplier` = 0.2m
  - `StartTime` = 2000-01-01
  - `EndTime` = 2099-01-01
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: EMA, ATR, Highest
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
