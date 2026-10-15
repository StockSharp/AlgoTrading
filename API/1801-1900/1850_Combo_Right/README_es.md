# Estrategia Combo Right
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia convertida desde el script MQL `combo_right.mq5`.
El sistema combina una señal básica de CCI con tres perceptrones simples que trabajan sobre diferencias de precios.

## Lógica

1. **Señal básica** – valor del Commodity Channel Index (CCI). Los valores positivos favorecen operaciones largas, los negativos favorecen operaciones cortas.
2. **Perceptrones** – cada perceptrón examina un conjunto de precios de cierre desplazados y aplica pesos lineales. El parámetro de modo `Pass` selecciona qué perceptrones están activos:
   - `1`: solo señal básica de CCI.
   - `2`: el perceptrón de venta puede anular el CCI y abrir posiciones cortas.
   - `3`: el perceptrón de compra puede anular el CCI y abrir posiciones largas.
   - `4`: el perceptrón general supervisa tanto los perceptrones de compra como de venta.

Si un perceptrón activo emite una señal, reemplaza la salida básica del CCI. De lo contrario, se utiliza el valor del CCI.

## Parámetros

- `TakeProfit1`, `StopLoss1` – objetivos de beneficio y pérdida para la señal básica de CCI (en ticks).
- `CciPeriod` – período de lookback del indicador CCI.
- Pesos y períodos de cada perceptrón (`x12`, `x22`, …, `p4`).
- `Pass` – modo de operación.
- `Shift` – índice de barra usado para datos de precios (0 actual, 1 anterior).
- `Volume` – volumen de operación.
- `CandleType` – tipo de vela para los cálculos.

## Indicadores

- CCI.
