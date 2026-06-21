# Estrategia de Rompimiento de Momentum Anclado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Rompimiento de Momentum Anclado utiliza la razón entre una media móvil exponencial (EMA) y una media móvil simple (SMA) para medir el momentum. Cuando la EMA a corto plazo comienza a subir más rápido que la SMA a largo plazo, indica un momentum alcista creciente. Por el contrario, una razón decreciente señala un momentum bajista en fortalecimiento.

## Cómo Funciona
1. **Indicadores**
   - EMA con período configurable.
   - SMA con período configurable.
2. **Cálculo del Momentum**
   - `Momentum = 100 * (EMA / SMA - 1)`
   - Momentum positivo significa que la EMA está por encima de la SMA; momentum negativo significa que la EMA está por debajo de la SMA.
3. **Lógica de Trading**
   - Si el momentum ha estado disminuyendo y luego gira hacia arriba, la estrategia entra en una posición larga.
   - Si el momentum ha estado aumentando y luego gira hacia abajo, la estrategia entra en una posición corta.
   - El tamaño de posición incluye automáticamente la posición existente para revertir cuando sea necesario.
4. **Gestión de Riesgo**
   - Los niveles de stop-loss y take-profit se establecen como porcentajes del precio de entrada usando el mecanismo de protección integrado.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `SmaPeriod` | Período para el indicador SMA. |
| `EmaPeriod` | Período para el indicador EMA. |
| `StopLossPercent` | Porcentaje para el stop-loss. |
| `TakeProfitPercent` | Porcentaje para el take-profit. |
| `CandleType` | Marco temporal de velas usado para los cálculos. |

## Notas
- La estrategia trabaja solo con velas completadas.
- Todas las acciones de trading se ejecutan usando órdenes de mercado.
- Los valores de los indicadores se obtienen a través de la API de alto nivel `Bind` sin acceder directamente a los búferes históricos.
