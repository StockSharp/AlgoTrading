# CCI-Schwellenwert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die kauft, wenn der CCI unter einen Schwellenwert fällt, und schließt, wenn der Schlusskurs den vorherigen Schlusskurs übersteigt.
Optionaler Stop-Loss und Take-Profit in absoluten Punkten.

## Details

- **Einstiegskriterien**:
  - Long: `CCI < BuyThreshold`
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - `ClosePrice > previous ClosePrice`
- **Stops**: Optional über `UseStopLoss` und `UseTakeProfit`
- **Standardwerte**:
  - `LookbackPeriod` = 12
  - `BuyThreshold` = -90
  - `StopLossPoints` = 100m
  - `TakeProfitPoints` = 150m
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: CCI
  - Stops: Optional
  - Komplexität: Niedrig
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
