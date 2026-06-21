# MFS 3-Balken-Muster Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erkennt eine bullische Umkehrsequenz aus drei Balken innerhalb eines Abwärtstrends. Sie sucht nach einem großen grünen „Zündungs"-Balken, einem kleinen roten Rücksetzer und einem bullischen Bestätigungsbalken, der über dem Hochpunkt des Rücksetzers schließt. Der Trendfilter erfordert, dass lange SMA > mittlere SMA > kurze SMA ist und der Zündungsschluss unterhalb der kurzen SMA liegt.

Sobald das Muster erscheint, eröffnet die Strategie eine Long-Position, platziert den Stop-Loss am Tief des Zündungsbalkens und einen Take-Profit bei einem konfigurierbaren Risiko-Rendite-Vielfachen.

## Details

- **Einstiegskriterien**: Zündungs-, Rücksetzer- und Bestätigungsbalken in einem Abwärtstrend.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Stop-Loss am Zündungstief oder Take-Profit beim Risiko-Rendite-Vielfachen.
- **Stops**: Ja, Stop- und Zielorders.
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `SmaShortLength` = 20
  - `SmaMedLength` = 50
  - `SmaLongLength` = 200
  - `IgniteMultiplier` = 3
  - `MaxPullbackSize` = 0.33
  - `MinConfirmationSize` = 0.33
  - `RiskReward` = 2
- **Filter**:
  - Kategorie: Muster
  - Richtung: Long
  - Indikatoren: Candlestick, Moving Average
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
