# Williams %R Kreuzungs-Strategie mit 200 MA-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt Williams %R-Kreuzungen um das Niveau -50 mit einem 200-Perioden-SMA-Trendfilter.
Positionen werden mit festen Take-Profit- und Stop-Loss-Abständen geschlossen.

## Details

- **Einstiegskriterien**: %R kreuzt Schwellenwerte bei Preislage relativ zur 200 SMA
- **Long/Short**: Beide
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss
- **Stops**: Ja
- **Standardwerte**:
  - `WrLength` = 14
  - `CrossThreshold` = 10
  - `TakeProfit` = 30
  - `StopLoss` = 20
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: WilliamsR, SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
