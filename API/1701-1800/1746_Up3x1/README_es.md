# Estrategia Up3x1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Up3x1 usa tres medias móviles simples para capturar cambios de tendencia:

- **SMA rápida**: reacciona rápidamente a los cambios de precio.
- **SMA media**: proporciona confirmación adicional de la tendencia.
- **SMA lenta**: define la dirección global del mercado.

### Reglas de entrada

- **Compra** cuando la SMA rápida cruza por encima de la SMA media y ambas están por debajo de la SMA lenta.
- **Venta** cuando la SMA rápida cruza por debajo de la SMA media y ambas están por encima de la SMA lenta.

### Reglas de salida

- Se aplica un take profit y stop loss fijos a cada posición.
- Un trailing stop opcional puede proteger las ganancias siguiendo al precio tras la entrada.

### Parámetros

- `Volume` – tamaño de la orden.
- `TakeProfit` – objetivo de ganancia en unidades de precio.
- `StopLoss` – límite de pérdida en unidades de precio.
- `TrailingStop` – distancia de trailing; poner 0 para desactivar.
- `FastPeriod`, `MiddlePeriod`, `SlowPeriod` – longitudes de las medias móviles.
- `CandleType` – marco temporal de velas utilizado para los cálculos.

La estrategia está diseñada para demostración y puede personalizarse para instrumentos o condiciones de trading específicas.
