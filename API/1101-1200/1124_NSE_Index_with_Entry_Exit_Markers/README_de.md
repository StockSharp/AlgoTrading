# NSE Index Strategie mit Ein- und Ausstiegsmarkierungen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht Long, wenn der Preis über einem Trend-SMA liegt und der RSI über den überverkauften Level nach oben kreuzt. Ein ATR-basierter Stop-Loss und Take-Profit verwalten die Position.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis liegt über dem SMA und RSI kreuzt den überverkauften Level nach oben.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Long-Position schließen, wenn der Preis den ATR-basierten Stop oder Take-Profit erreicht.
- **Stops**: ATR-basierter Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `SmaPeriod` = 200.
  - `RsiPeriod` = 14.
  - `RsiOversold` = 40.
  - `AtrPeriod` = 14.
  - `AtrMultiplier` = 1.5.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: SMA, RSI, ATR
  - Stops: ATR-basiert
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
