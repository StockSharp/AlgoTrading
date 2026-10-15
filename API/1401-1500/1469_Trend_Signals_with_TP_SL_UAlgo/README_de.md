# Trendsignal-Strategie mit TP und SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet einen ATR-basierten Kanal zur Bestimmung der Trendrichtung. Ein neuer Aufwärtstrend beginnt, wenn der Preis über das obere Band ausbricht und einen Long-Einstieg auslöst. Ein Abwärtstrend beginnt, wenn der Preis unter das untere Band fällt und einen Short-Einstieg auslöst. Jeder Trade platziert Stop-Loss- und Take-Profit-Orders mithilfe von ATR-Multiplikatoren.

## Details

- **Einstiegskriterien**:
  - **Long**: Trend dreht nach oben.
  - **Short**: Trend dreht nach unten.
- **Ausstiege**: Stop-Loss bei `entry ∓ ATR * SL` und Take-Profit bei `entry ± ATR * TP`.
- **Stops**: Ja, sowohl Stop-Loss als auch Take-Profit.
- **Standardwerte**:
  - `Sensitivity` = 2
  - `ATR Length` = 14
  - `ATR TP Multiplier` = 2
  - `ATR SL Multiplier` = 1
