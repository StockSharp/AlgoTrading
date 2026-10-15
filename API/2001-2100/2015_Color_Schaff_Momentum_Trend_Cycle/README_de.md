# Color Schaff Momentum Trend-Zyklus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet den Color Schaff Momentum Trend Cycle (STC), um Trendwenden zu erkennen, wenn der Indikator überkaufte oder überverkaufte Zonen verlässt.

## Details

- **Einstiegskriterien**:
  - Kaufen, wenn die vorherige STC-Farbe über der oberen Zone war (>5) und die aktuelle Farbe unter 6 fällt, dabei werden Short-Positionen geschlossen.
  - Verkaufen, wenn die vorherige STC-Farbe unter der unteren Zone war (<2) und die aktuelle Farbe über 1 steigt, dabei werden Long-Positionen geschlossen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Das umgekehrte Signal schließt die entgegengesetzte Position.
- **Stops**: Kein expliziter Stop-Loss oder Take-Profit.
- **Standardwerte**:
  - `FastMomentum` = 23
  - `SlowMomentum` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true

