# Trailing-Monster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die KAMA-Trenderkennung mit RSI-Filter und einem Trailing-Stop kombiniert. Positionen werden eröffnet, wenn der RSI in der Richtung des KAMA-Trends extreme Niveaus kreuzt. Nach einer Verzögerung schützt ein prozentualer Trailing-Stop die Gewinne.

## Details
- **Einstiegskriterien**:
  - **Long**: RSI > `RsiOverbought`, Schlusskurs über SMA, KAMA steigt
  - **Short**: RSI < `RsiOversold`, Schlusskurs unter SMA, KAMA fällt
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Prozentualer Trailing-Stop nach `DelayBars`
- **Stops**: Trailing-Stop in Prozent
- **Standardwerte**:
  - `KamaLength` = 40
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `SmaLength` = 200
  - `BarsBetweenEntries` = 3
  - `TrailingStopPct` = 12m
  - `DelayBars` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: KAMA, RSI, SMA
  - Stops: Trailing
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
