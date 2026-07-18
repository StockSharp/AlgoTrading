# Chande Kroll Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den Chande Kroll Stop mit einem SMA-Trendfilter kombiniert. Eine Long-Position wird eröffnet, wenn der Schlusskurs den unteren Stop kreuzt und über dem SMA liegt. Die Position wird geschlossen, wenn der Schlusskurs unter den oberen Stop fällt. Die Positionsgröße basiert auf dem niedrigsten Schlusskurs über 1560 Bars und dem Risikomultiplikator.

## Details

- **Einstiegskriterien**:
  - Long: `previous close <= previous low stop && Close > low stop && Close > SMA`
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - Long: `Close < high stop`
- **Stops**: Chande Kroll Stop (Donchian-Extremwerte ± ATR)
- **Standardwerte**:
  - `CalcMode` = CalcMode.Exponential
  - `RiskMultiplier` = 5m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `StopLength` = 21
  - `SmaLength` = 21
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: ATR, Donchian, SMA, Lowest
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
