# Megabar Ausbruch (Range & Volumen & RSI)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Megabar Breakout erkennt große Kerzen, die durch hohes Volumen und RSI-Bestätigung gestützt werden. Die Strategie geht bei bullischen Megabars long und bei bärischen short.

Sie multipliziert den durchschnittlichen Range und das Volumen, um Megabars zu finden. Der gleitende Durchschnitt des RSI filtert die Trades.

## Details

- **Einstiegskriterien**: Kerzenkörper und Volumen übersteigen ihre gleitenden Durchschnitte um die angegebenen Multiplikatoren. RSI MA über dem Long-Schwellenwert für Käufe und unter dem Short-Schwellenwert für Verkäufe.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VolumeAveragePeriod` = 20
  - `VolumeMultiplier` = 3
  - `RangeAveragePeriod` = 20
  - `RangeMultiplier` = 4
  - `RsiPeriod` = 14
  - `RsiMaPeriod` = 14
  - `LongRsiThreshold` = 50
  - `ShortRsiThreshold` = 70
  - `TakeProfit` = 400
  - `StopLoss` = 300
  - `FilterTradeHours` = false
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Volumen, Range, RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
