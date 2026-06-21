# Ilan-Dynamic-HT-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Gitterbasierte Martingal-Strategie, die Positionen anhand von RSI-Signalen eröffnet und die Position mithilfe eines dynamischen Preisbereichs ausbaut. Jeder zusätzliche Trade erhöht das Volumen um einen Multiplikator und teilt denselben Take Profit und Stop Loss.

## Details

- **Einstiegskriterien**:
  - Long: RSI unter `RsiMinimum`
  - Short: RSI über `RsiMaximum`
- **Long/Short**: Long und Short
- **Ausstiegskriterien**:
  - Gemeinsamer Take Profit oder Stop Loss wird erreicht
- **Stops**:
  - `TakeProfit` in Punkten
  - `StopLoss` in Punkten
- **Standardwerte**:
  - `LotExponent` = 1.4
  - `MaxTrades` = 10
  - `DynamicPips` = true
  - `DefaultPips` = 120
  - `Depth` = 24
  - `Del` = 3
  - `BaseVolume` = 0.1
  - `RsiPeriod` = 14
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `TakeProfit` = 100
  - `StopLoss` = 500
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Gitter / Martingal
  - Richtung: Long und Short
  - Indikatoren: RSI, Highest, Lowest
  - Stops: Take Profit, Stop Loss
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
