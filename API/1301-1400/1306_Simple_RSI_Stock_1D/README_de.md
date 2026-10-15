# Einfache RSI-Aktien-Strategie 1D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses System geht long, wenn der RSI unter ein überverkauftes Niveau fällt und der Preis über dem 200-Tage-SMA bleibt. Die Position verwendet einen ATR-basierten Stop und drei Gewinnziele.

## Details

- **Einstiegskriterien**: RSI unter `OversoldLevel` und Schlusskurs über dem SMA-Filter.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: ATR-Stop oder Erreichen eines Gewinnziels.
- **Stops**: Ja.
- **Standardwerte**:
  - `RsiPeriod` = 5
  - `OversoldLevel` = 30
  - `SmaLength` = 200
  - `AtrLength` = 20
  - `AtrMultiplier` = 1.5
  - `TakeProfit1` = 5
  - `TakeProfit2` = 10
  - `TakeProfit3` = 15
  - `StopLossPercent` = 25
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Long
  - Indikatoren: RSI, SMA, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
