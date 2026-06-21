# Trend Magic mit EMA, SMA und Auto-Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet die CCI-basierte Trend-Magic-Linie zusammen mit EMA(45), SMA(90) und SMA(180) als Filter. Ein Long-Trade wird eröffnet, wenn Trend Magic bei bullischer Ausrichtung der gleitenden Durchschnitte auf Blau wechselt. Short-Trades treten auf, wenn die Linie rot wird und die Durchschnitte bärisch ausgerichtet sind. Jede Position hat einen Stop bei SMA90 und einen Take-Profit basierend auf einem festen Risiko-Ertrags-Verhältnis.

## Details

- **Einstiegskriterien**:
  - **Long**: `EMA45 > SMA90 > SMA180` und Trend Magic wird blau.
  - **Short**: `EMA45 < SMA90 < SMA180` und Trend Magic wird rot.
- **Ausstiege**: Stop-Loss bei SMA90 beim Einstieg erfasst und Take-Profit bei `entry ± risk * ratio`.
- **Stops**: Sowohl Stop-Loss als auch Take-Profit.
- **Standardwerte**:
  - `CCI Period` = 21
  - `ATR Period` = 7
  - `ATR Multiplier` = 1.0
  - `Risk Reward` = 1.5
