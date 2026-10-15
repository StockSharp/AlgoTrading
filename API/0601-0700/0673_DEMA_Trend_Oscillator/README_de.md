# DEMA-Trendoszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie normalisiert den Double Exponential Moving Average (DEMA) mit einem gleitenden Durchschnitt und der Standardabweichung. Geht long, wenn der normalisierte Wert den Long-Schwellenwert überschreitet und der Preis über dem oberen Band bleibt; geht short, wenn er unter dem Short-Schwellenwert liegt und der Preis unter dem unteren Band bleibt. Verwendet einen ATR-basierten Trailing-Stop, einen Band-Stop-Loss und einen Risk-Reward-Take-Profit.

## Details

- **Einstiegskriterien**:
  - Long: normalisierter Wert > `LongThreshold` und Tief > oberes Band
  - Short: normalisierter Wert < `ShortThreshold` und Hoch < unteres Band
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: Preis erreicht Take-Profit, Band-Stop-Loss oder Trailing-Stop
  - Short: Preis erreicht Take-Profit, Band-Stop-Loss oder Trailing-Stop
- **Stops**: Band-Stop-Loss, ATR-Trailing, Risk-Reward-Take-Profit
- **Standardwerte**:
  - `DemaPeriod` = 40
  - `BaseLength` = 20
  - `LongThreshold` = 55m
  - `ShortThreshold` = 45m
  - `RiskReward` = 1.5m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: DEMA, SMA, StandardDeviation, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
