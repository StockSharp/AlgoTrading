# Strategie Simple MA ADX EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die eine EMA mit dem Average Directional Index kombiniert, um die Trendstärke zu bestätigen.

Sie kauft, wenn die EMA steigt, der vorherige Schlusskurs über der EMA liegt, ADX einen Schwellenwert überschreitet und +DI größer als -DI ist. Sie verkauft, wenn die entgegengesetzten Bedingungen auftreten. Stop-Loss- und Take-Profit-Niveaus steuern das Risiko.

## Details

- **Einstiegskriterien**: EMA-Richtung, Preis vs. EMA, ADX, +DI/-DI.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Schutzorders.
- **Stops**: Ja.
- **Standardwerte**:
  - `AdxPeriod` = 8
  - `MaPeriod` = 8
  - `AdxThreshold` = 22m
  - `StopLoss` = 30m
  - `TakeProfit` = 100m
  - `Volume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, ADX
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
