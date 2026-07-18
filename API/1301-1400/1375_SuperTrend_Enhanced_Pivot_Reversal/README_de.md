# SuperTrend Erweiterte Pivot-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert die SuperTrend-Richtung mit Pivot-Hoch/Tief-Ausbrüchen. Ein Long-Stop wird oberhalb eines kürzlichen Pivot-Hochs platziert, wenn der SuperTrend bärisch ist. Ein Short-Stop wird unterhalb eines Pivot-Tiefs platziert, wenn der SuperTrend bullisch ist. Positionen werden mit einem prozentualen Stop-Loss vom Pivot abgesichert.

## Details

- **Einstiegskriterien**:
  - Long: Pivot-Hoch gebildet, SuperTrend abwärts → Kauf-Stop über Pivot.
  - Short: Pivot-Tief gebildet, SuperTrend aufwärts → Verkaufs-Stop unter Pivot.
- **Richtung**: Konfigurierbar.
- **Ausstiegskriterien**: Prozentualer Stop-Loss oder entgegengesetzte Richtung für einseitigen Modus.
- **Indikatoren**: SuperTrend, Pivot-Hochs/Tiefs.
- **Standardwerte**:
  - `LeftBars` = 6
  - `RightBars` = 3
  - `AtrLength` = 5
  - `Factor` = 2.618
  - `StopLossPercent` = 20
  - `TradeDirection` = Both
  - `CandleType` = 5 minute
