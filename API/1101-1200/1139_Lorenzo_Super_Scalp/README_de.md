# Lorenzo SuperScalp-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Scalping-Strategie kombiniert RSI, Bollinger Bänder und MACD. Sie kauft, wenn der RSI unter 45 liegt, der Preis nahe am unteren Band ist und der MACD nach oben kreuzt. Sie verkauft, wenn der RSI über 55 liegt, der Preis nahe am oberen Band ist und der MACD nach unten kreuzt. Eine Mindestanzahl von Bars zwischen Trades verhindert schnelles Wiedereinsteigen.

## Details

- **Einstiegskriterien**:
  - **Long**: `RSI < 45` && `Close < LowerBand * 1.02` && `MACD` kreuzt über die Signallinie.
  - **Short**: `RSI > 55` && `Close > UpperBand * 0.98` && `MACD` kreuzt unter die Signallinie.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegenseitiges Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `RSI Length` = 14
  - `Bollinger Length` = 20
  - `Bollinger Multiplier` = 2
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Min Bars` = 15
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
