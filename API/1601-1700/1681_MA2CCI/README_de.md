# MA2CCI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen schnellen und langsamen Simple Moving Average (SMA) Crossover mit dem Commodity Channel Index (CCI) als Bestätigungsfilter. Eine Position wird nur eröffnet, wenn sowohl die gleitenden Durchschnitte als auch der CCI ihre Niveaus in der gleichen Richtung kreuzen. Der Average True Range (ATR) definiert den anfänglichen Stop-Loss-Abstand.

Das System kann in beide Richtungen handeln. Es gibt kein Take-Profit; Positionen werden bei einem entgegengesetzten Signal oder beim Auslösen des ATR-basierten Stop-Loss geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: Schneller SMA kreuzt über den langsamen SMA **und** CCI kreuzt über 0.
  - **Short**: Schneller SMA kreuzt unter den langsamen SMA **und** CCI kreuzt unter 0.
- **Ausstiegskriterien**:
  - Umgekehrter SMA-Crossover.
  - ATR-basierter Stop-Loss.
- **Indikatoren**: SMA, CCI, ATR.
- **Zeitrahmen**: Konfigurierbar über `CandleType`.
- **Standardparameter**:
  - `Fast MA Period` = 4
  - `Slow MA Period` = 8
  - `CCI Period` = 4
  - `ATR Period` = 4
- **Long/Short**: Beide.
- **Stops**: Ja, dynamischer Stop mit ATR.
