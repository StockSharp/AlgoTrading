# Bullische B's RSI-Divergenz
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet RSI, um reguläre und versteckte bullische Divergenzen mit Pivot-Punkten zu erkennen. Eröffnet Long-Trades bei Divergenz und schließt sie bei bärischen Signalen, RSI-Ziel oder Trailing-Stop.

## Details

- **Einstiegskriterien**:
  - **Long**: Reguläre oder versteckte bullische RSI-Divergenz.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Bärische Divergenz, RSI kreuzt über Ziel oder Trailing-Stop.
- **Stops**: Optionaler Trailing-Stop basierend auf ATR oder Prozent.
- **Standardwerte**:
  - `RsiPeriod` = 9
  - `PivotLookbackRight` = 3
  - `PivotLookbackLeft` = 1
  - `TakeProfitRsiLevel` = 80
  - `RangeUpper` = 60
  - `RangeLower` = 5
  - `StopType` = None
  - `StopLoss` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 3.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Divergenz
  - Richtung: Long
  - Indikatoren: RSI, ATR
  - Stops: Optionaler Trailing-Stop
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
