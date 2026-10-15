# Multi-Faktor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Multi-Faktor-Strategie kombiniert MACD, RSI und zwei gleitende Durchschnitte für trendkonforme Trades. Long-Trades entstehen, wenn die MACD-Linie über ihrer Signallinie liegt, der RSI unter 70 ist, der Kurs über dem 50-Perioden-SMA liegt und der 50-SMA über dem 200-SMA liegt. Short-Trades verwenden entgegengesetzte Bedingungen.

Stops und Ziele basieren auf ATR-Vielfachen.

## Details

- **Einstiegskriterien**:
  - **Long**: `MACD > Signal` && `RSI < 70` && `Close > SMA50` && `SMA50 > SMA200`.
  - **Short**: `MACD < Signal` && `RSI > 30` && `Close < SMA50` && `SMA50 < SMA200`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-basierter Stop-Loss und Take-Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `RsiLength` = 14
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 2
  - `ProfitAtrMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MACD, RSI, SMA, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
