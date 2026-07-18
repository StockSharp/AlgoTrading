# Estrategia ATR Aleatoria - Bybit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia genera una señal aleatoria determinista basada en los rangos de precios recientes y la fecha actual. Entra largo cuando la señal es 1 y corto cuando es 0. La gestión del riesgo utiliza niveles de stop-loss y take-profit basados en ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: la señal aleatoria es igual a 1.
  - **Corto**: la señal aleatoria es igual a 0.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: stop-loss o take-profit.
- **Stops**: `SlAtrRatio * ATR` para stop-loss, take-profit en `SlAtrRatio * TpSlRatio * ATR`.
- **Valores predeterminados**:
  - `AtrLength` = 14
  - `SlAtrRatio` = 3
  - `TpSlRatio` = 1
- **Filtros**: ninguno.
- **Complejidad**: simple.
- **Marco temporal**: configurable.
