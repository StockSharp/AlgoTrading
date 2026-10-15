# Trend-Erfassung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie, die Parabolic SAR mit einem ADX-Filter kombiniert. Long-Trades entstehen, wenn der Preis über dem SAR-Wert schließt, während der ADX unter einem Schwellenwert bleibt, was einen aufkeimenden Trend signalisiert. Short-Trades werden bei der umgekehrten Bedingung eröffnet.

## Details

- **Einstiegskriterien**: Preis über/unter Parabolic SAR bei ADX unter `AdxLevel`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop Loss, Take Profit oder entgegengesetztes Signal.
- **Stops**: Fester Stop Loss, Take Profit und Break-Even-Anpassung.
- **Standardwerte**:
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `AdxPeriod` = 14
  - `AdxLevel` = 20
  - `StopLoss` = 1800 Punkte
  - `TakeProfit` = 500 Punkte
  - `BreakEven` = 50 Punkte
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Parabolic SAR, ADX
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
