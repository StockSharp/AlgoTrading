# Nur-Long MTF EMA Wolken-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA-Wolken-Kreuzungsstrategie, die long handelt, wenn die kurze EMA die lange EMA nach oben kreuzt. Verwendet feste prozentuale Stop-Loss- und Take-Profit-Werte.

## Details

- **Einstiegskriterien**: Kurze EMA kreuzt lange EMA nach oben.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Kurs erreicht Stop-Loss oder Take-Profit.
- **Stops**: Fester prozentualer Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `ShortLength` = 21
  - `LongLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 2.0m
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
