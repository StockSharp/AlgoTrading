# ADX CCI MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kombiniert ADX, CCI und einen konfigurierbaren gleitenden Durchschnitt, um starke Trends zu handeln.

Das System kauft, wenn +DI über -DI kreuzt, CCI > 100 und ADX den Schwellenwert überschreitet (optional: Schlusskurs über MA). Es geht short, wenn -DI über +DI kreuzt, CCI < -100 und ADX den Schwellenwert überschreitet (Schlusskurs unter MA).

Beinhaltet prozentualen Stop-Loss und Take-Profit sowie optionales MA-Risikomanagement, das nach mehreren gegen den gleitenden Durchschnitt gerichteten Kerzen aussteigt.

## Details

- **Einstiegskriterien**: +DI/-DI-Kreuzung mit CCI-Extremwert und ADX > `AdxThreshold`, optional Schlusskurs vs. MA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit erreicht, optionales MA-Risikomanagement.
- **Stops**: Ja, Take-Profit und Stop-Loss.
- **Standardwerte**:
  - `EnableLong` = true
  - `EnableShort` = true
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CciPeriod` = 15
  - `AdxLength` = 10
  - `AdxThreshold` = 20m
  - `UseMaTrend` = true
  - `MaType` = MovingAverageTypeEnum.Simple
  - `MaLength` = 200
  - `UseMaRiskManagement` = false
  - `MaRiskExitCandles` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ADX, CCI, MA
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
