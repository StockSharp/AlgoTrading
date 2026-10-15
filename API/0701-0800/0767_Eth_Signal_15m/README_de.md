# ETH Signal 15m-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die ETH Signal 15m-Strategie verwendet den Supertrend-Indikator, um Richtungsänderungen zu erkennen, und den RSI, um Einstiege zu filtern. Eine Long-Position wird eröffnet, wenn die Supertrend-Richtung abnimmt und der RSI unter dem Überkauft-Niveau liegt. Eine Short-Position wird eröffnet, wenn die Supertrend-Richtung zunimmt und der RSI über dem Überverkauft-Niveau liegt. Ausstiege verwenden ATR-basierten Stop Loss und Take Profit.

## Details

- **Einstiegskriterien**:
  - **Long**: Supertrend-Richtung nimmt ab und RSI liegt unter `RsiOverbought`.
  - **Short**: Supertrend-Richtung nimmt zu und RSI liegt über `RsiOversold`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: ATR-basierter Stop Loss und Take Profit.
- **Stops**: Stop Loss 4×ATR, Take Profit 2×ATR für Long, Take Profit 2.237×ATR für Short.
- **Standardwerte**:
  - `AtrPeriod` = 12
  - `Factor` = 2.76
  - `RsiLength` = 12
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Supertrend, RSI, ATR
  - Stops: ATR Stop Loss und Take Profit
  - Komplexität: Niedrig
  - Zeitrahmen: 15m
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
