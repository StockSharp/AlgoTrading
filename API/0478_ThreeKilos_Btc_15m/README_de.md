# Three Kilos BTC 15m-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Three Kilos BTC 15m-Strategie kombiniert drei Triple Exponential Moving Averages (TEMA) mit einem Supertrend-Filter. Eine Long-Position wird eröffnet, wenn das mittlere TEMA über das kurze TEMA kreuzt, über dem langsamen TEMA bleibt und der Supertrend einen Aufwärtstrend anzeigt. Eine Short-Position wird eröffnet, wenn das kurze TEMA über das mittlere TEMA kreuzt, unter dem langsamen TEMA bleibt und der Supertrend einen Abwärtstrend zeigt. Ein fester prozentualer Take-Profit und Stop-Loss steuern das Risiko.

## Details

- **Einstiegskriterien**:
  - **Long**: TEMA2 kreuzt über TEMA1, TEMA2 > TEMA3, Supertrend-Aufwärtstrend.
  - **Short**: TEMA1 kreuzt über TEMA2, TEMA2 < TEMA3, Supertrend-Abwärtstrend.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Take-Profit oder Stop-Loss.
- **Stops**: 1% Take-Profit und 1% Stop-Loss.
- **Standardwerte**:
  - `ShortPeriod` = 30
  - `LongPeriod` = 50
  - `Long2Period` = 140
  - `AtrLength` = 10
  - `Multiplier` = 2
  - `TakeProfit` = 1%
  - `StopLoss` = 1%
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: TEMA, Supertrend, ATR
  - Stops: Take-Profit und Stop-Loss
  - Komplexität: Moderat
  - Zeitrahmen: 15m
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
