# Estrategia Coensio Swing Trader V06
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la lógica de ruptura del Coensio Swing Trader original. Utiliza el Canal de Donchian para definir soporte y resistencia dinámicos. Se abre una operación cuando el precio rompe por encima de la banda superior o por debajo de la banda inferior en un umbral configurable.

## Detalles

- **Entrada**:
  - **Largo**: El precio de cierre rompe por encima de la banda superior del Canal de Donchian + `Entry Threshold` pips.
  - **Corto**: El precio de cierre rompe por debajo de la banda inferior del Canal de Donchian - `Entry Threshold` pips.
- **Salidas**:
  - `Stop Loss` y `Take Profit` fijos en pips medidos desde el precio de entrada.
  - Movimiento opcional a punto de equilibrio después de `Break Even` pips de ganancia.
  - Trailing stop opcional que sigue el precio por `Trailing Step` pips después del punto de equilibrio.
- **Stops**: Stop-loss, take-profit, punto de equilibrio, trailing stop.
- **Valores predeterminados**:
  - `Channel Period` = 20
  - `Entry Threshold` = 15 pips
  - `Stop Loss` = 50 pips
  - `Take Profit` = 80 pips
  - `Break Even` = 25 pips
  - `Trailing Step` = 5 pips
  - `Enable Trailing` = false
  - `Candle Type` = velas de 15 minutos
