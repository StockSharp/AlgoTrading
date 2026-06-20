# ALMA & UT Bot Confluence-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die ALMA & UT Bot Confluence-Strategie kombiniert einen Arnaud Legoux Moving Average-Filter mit einem UT Bot-Trailing-Stop. Eine Long-Position wird eröffnet, wenn der Preis über der langfristigen EMA und ALMA liegt, das Volumen seinen Durchschnitt überschreitet, der RSI Momentum signalisiert, der ADX die Trendstärke bestätigt, die Kerze unter dem oberen Bollinger-Band liegt und der UT Bot ein Kaufsignal generiert. Short-Einstiege erfolgen, wenn der UT Bot bärisch wird und der Preis unter die schnelle EMA fällt, unter denselben Filtern. Ausstiege nutzen entweder den UT Bot Trailing-Stop oder festen ATR-basierten Stop-Loss und Take-Profit.

## Details

- **Einstiegskriterien**:
  - Long: Preis > EMA & ALMA, RSI > 30, ADX > 30, Preis < oberes Bollinger-Band, UT Bot Kaufsignal, Volumen- und ATR-Filter, Abkühlzeit.
  - Short: Preis kreuzt unter die schnelle EMA mit UT Bot Verkaufssignal und Filtern.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - UT Bot Trailing-Stop oder ATR-basierter Stop-Loss/Take-Profit und optionaler Zeitausstieg.
- **Stops**: ATR oder Trailing.
- **Standardwerte**:
  - `FastEmaLength` = 20
  - `EmaLength` = 72
  - `AtrLength` = 14
  - `AdxLength` = 10
  - `RsiLength` = 14
  - `BbMultiplier` = 3.0
  - `StopLossAtrMultiplier` = 5.0
  - `TakeProfitAtrMultiplier` = 4.0
  - `UtAtrPeriod` = 10
  - `UtKeyValue` = 1
  - `VolumeMaLength` = 20
  - `BaseCooldownBars` = 7
  - `MinAtr` = 0.005
- **Filter**:
  - Kategorie: Trendfolge mit Volatilitätsfilter
  - Richtung: Long/Short
  - Indikatoren: EMA, ALMA, ADX, RSI, Bollinger Bands, UT Bot
  - Stops: ATR oder Trailing
  - Komplexität: Hoch
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
