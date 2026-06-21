# Mutanabby AI Algo Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Mutanabby AI Algo Pro-Strategie geht long, wenn ein bullisches Engulfing-Muster mit einem RSI-Wert unter einem Schwellenwert und einem Kursrückgang über eine bestimmte Anzahl von Kerzen zusammentrifft. Exits erfolgen bei einem bärischen Engulfing-Muster oder wenn der Stop-Loss ausgelöst wird.

## Details
- **Einstiegskriterien**: Bullisches Engulfing, stabile Kerze, RSI unter Schwellenwert, Kurs unter dem Wert vor N Kerzen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Bärisches Engulfing oder Stop-Loss.
- **Stops**: Optional.
- **Standardwerte**:
  - `CandleStabilityIndex` = 0.5
  - `RsiIndex` = 50
  - `CandleDeltaLength` = 5
  - `DisableRepeatingSignals` = false
  - `EnableStopLoss` = true
  - `StopLossMethod` = EntryPriceBased
  - `EntryStopLossPercent` = 2.0
  - `LookbackPeriod` = 10
  - `StopLossBufferPercent` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
