# Pullback Pro Dow-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nutzt Dow-Theorie-Pivots zur Bestimmung der Trendrichtung und steigt bei EMA-Pullbacks ein, wenn die Trendstärke durch den ADX bestätigt wird. Das System skaliert bei zwei Risiko-Rendite-Zielen aus.

Backtests zeigen stabiles Verhalten bei Indexfutures wie US30.

## Details

- **Einstiegskriterien**:
  - Long: höhere Hochs und höhere Tiefs, Tief unterschreitet EMA, ADX über Schwellenwert
  - Short: niedrigere Hochs und niedrigere Tiefs, Hoch überschreitet EMA, ADX über Schwellenwert
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop am letzten Pivot, Gewinnmitnahme an zwei R:R-Zielen
- **Stops**: Pivot-basiert
- **Standardwerte**:
  - `PivotLookback` = 10
  - `EmaLength` = 21
  - `RiskReward1` = 1.5m
  - `Tp1Percent` = 50
  - `RiskReward2` = 3m
  - `UseAdxFilter` = true
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, Average Directional Index
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
