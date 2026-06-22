# Estrategia ColorMETRO Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación en C# del experto MQL5 **exp_colormetro_stochastic.mq5**. Reemplaza el indicador original ColorMETRO Stochastic con el `StochasticOscillator` integrado de StockSharp y opera en eventos de cruce.

## Lógica
- Se suscribe a velas de 8 horas por defecto (configurable).
- Calcula el oscilador Stochastic con los parámetros:
  - Período %K (`KPeriod`)
  - Período %D (`DPeriod`)
  - Suavizado adicional (`Slowing`)
- Almacena los valores anteriores de %K y %D para detectar cruces.
- **Compra** cuando %K cruza por encima de %D.
- **Venta** cuando %K cruza por debajo de %D.
- Aplica un stop-loss y take-profit del 2% mediante `StartProtection`.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `KPeriod` | Período de retroceso para la línea %K (predeterminado 5). |
| `DPeriod` | Período de suavizado para la línea %D (predeterminado 3). |
| `Slowing` | Valor de suavizado adicional (predeterminado 3). |
| `CandleType` | Marco temporal de las velas, predeterminado 8 horas. |

## Notas
La versión MQL original usaba un indicador ColorMETRO Stochastic personalizado con líneas de paso rápido y lento. Esta adaptación aproxima sus señales utilizando el oscilador Stochastic estándar.
