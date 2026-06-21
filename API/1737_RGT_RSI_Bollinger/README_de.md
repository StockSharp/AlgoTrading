# RGT RSI Bollinger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den Relative Strength Index (RSI) mit Bollinger-Bändern, um Mean-Reversion-Gelegenheiten zu erkennen. Eine Long-Position wird eröffnet, wenn der RSI einen überverkauften Markt anzeigt und der Preis unter dem unteren Bollinger-Band handelt. Eine Short-Position wird eingegangen, wenn der RSI einen überkauften Markt zeigt und der Preis über dem oberen Band steigt. Die Strategie setzt einen anfänglichen Stop-Loss und verfolgt den Stop später, sobald ein Mindestgewinn erreicht ist.

Der Trailing Stop sichert Gewinne, indem er dem Preis bei einem festen Abstand folgt, sobald sich der Trade günstig entwickelt. Positionen werden geschlossen, wenn der Trailing Stop ausgelöst wird.

## Details

- **Einstiegskriterien**: RSI unter `RsiLow` und Preis unter dem unteren Band für Long; RSI über `RsiHigh` und Preis über dem oberen Band für Short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Trailing Stop ausgelöst.
- **Stops**: Anfänglicher Stop-Loss und Trailing Stop.
- **Standardwerte**:
  - `RsiPeriod` = 8
  - `RsiHigh` = 90
  - `RsiLow` = 10
  - `StopLossPips` = 70
  - `TrailingStopPips` = 35
  - `MinProfitPips` = 30
  - `Volume` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: RSI, Bollinger-Bänder
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
