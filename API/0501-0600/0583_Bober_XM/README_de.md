# Bober XM-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Bober XM-Strategie verwendet einen Doppelkanal-Ansatz basierend auf einer benutzerdefinierten Keltner-Berechnung. Ausbruchs-Einstiege werden durch einen Weighted Moving Average und die allgemeine Trendstärke des ADX bestätigt. Ausstiege basieren auf dem On-Balance Volume, das seinen Moving Average kreuzt, während der ADX stark bleibt.

Geeignet für Trader, die Momentum-Bestätigung mit volumenbasierten Ausstiegen suchen.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close > UpperBand && Close > WMA && ADX > Threshold`
  - **Short**: `Close < LowerBand && Close < WMA && ADX > Threshold`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - **Long**: `OBV < OBV_MA && ADX > Threshold`
  - **Short**: `OBV > OBV_MA && ADX > Threshold`
- **Stops**: Prozentualer Stop-Loss über `StopLossPercent`
- **Standardwerte**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `KeltnerMultiplier` = 1.5m
  - `WmaPeriod` = 15
  - `ObvMaPeriod` = 22
  - `AdxPeriod` = 60
  - `AdxThreshold` = 35m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Keltner Channel, WMA, OBV, ADX
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
