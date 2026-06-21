# Estrategia Trend Magic con EMA, SMA y Auto-Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza la línea Trend Magic basada en CCI junto con los filtros EMA(45), SMA(90) y SMA(180). Una operación larga se abre cuando Trend Magic cambia a azul durante una alineación alcista de medias móviles. Las operaciones cortas ocurren cuando la línea se vuelve roja y las medias móviles se alinean bajistamente. Cada posición tiene un stop en SMA90 y un take profit basado en una relación riesgo/recompensa fija.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `EMA45 > SMA90 > SMA180` y Trend Magic se vuelve azul.
  - **Corto**: `EMA45 < SMA90 < SMA180` y Trend Magic se vuelve rojo.
- **Salidas**: Stop-loss en SMA90 capturado al entrar y take-profit en `entry ± risk * ratio`.
- **Stops**: Ambos stop-loss y take-profit.
- **Valores predeterminados**:
  - `CCI Period` = 21
  - `ATR Period` = 7
  - `ATR Multiplier` = 1.0
  - `Risk Reward` = 1.5
