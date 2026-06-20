# AUD/USD Scalping-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie scalpt AUD/USD auf kurzen Zeitrahmen mit einer Kombination aus EMA-Trendfilter, Bollinger Bands und RSI. Die schnellen und langsamen EMAs definieren die Trendrichtung. Long-Trades werden in Aufwärtstrends eröffnet, wenn der Preis das untere Bollinger Band berührt und der RSI über dem Überverkauft-Schwellenwert liegt. Shorts werden in Abwärtstrends eingegangen, wenn der Preis das obere Band erreicht und der RSI unter dem Überkauft-Niveau liegt. Feste Take-Profit- und Stop-Loss-Level verwalten das Risiko.

## Details

- **Einstiegskriterien**:
  - **Long**: Schneller EMA über langsamem EMA, Preis am oder unterhalb des unteren Bollinger Bands, RSI über Überverkauft-Niveau.
  - **Short**: Schneller EMA unter langsamem EMA, Preis am oder oberhalb des oberen Bollinger Bands, RSI unter Überkauft-Niveau.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Fester Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `EmaShort` = 13
  - `EmaLong` = 26
  - `RsiPeriod` = 4
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `TakeProfit` = 0.0005
  - `StopLoss` = 0.0004
- **Filter**:
  - Kategorie: Scalping
  - Richtung: Beide
  - Indikatoren: EMA, Bollinger Bands, RSI
  - Stops: Fest
  - Komplexität: Niedrig
  - Zeitrahmen: 1 Minute
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
