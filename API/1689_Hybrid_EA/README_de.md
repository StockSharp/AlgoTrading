# Hybrid-EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Hybrid-EA-Strategie verwendet den Relative Vigor Index (RVI) und seine Signallinie.
Sie eröffnet eine Long-Position, wenn der RVI die Signallinie um eine angegebene Differenz übersteigt, und eine Short-Position, wenn er um denselben Betrag darunter fällt. Positionen werden durch feste Take-Profit- und Stop-Loss-Niveaus in Preispunkten geschützt.

## Details

- **Einstiegskriterien**: RVI minus Signal überschreitet Schwellenwert
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetzter Schwellenwert-Kreuzung oder Take-Profit/Stop-Loss
- **Stops**: Ja, fester Abstand in Punkten
- **Standardwerte**:
  - `Volume` = 1
  - `RviLength` = 10
  - `SignalLength` = 4
  - `DifferenceThreshold` = 0.05
  - `TakeProfit` = 18
  - `StopLoss` = 9
  - `CandleType` = 5 minute candles
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RVI, SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
