# Estrategia MA RSI EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia MA RSI EA** reproduce la lógica del asesor experto original de MetaTrader que combina una media móvil rápida con un filtro RSI de período corto. La estrategia opera en la serie de velas seleccionada, evalúa nuevas órdenes solo en barras terminadas y utiliza dimensionamiento de posición dinámico basado en el saldo o patrimonio de la cuenta. Cuando el beneficio flotante de todas las posiciones abiertas se vuelve positivo, cada posición se cierra inmediatamente para asegurar la ganancia.

## Indicadores
- **Moving Average** – método configurable (simple, exponencial, suavizado, ponderado linealmente) con selección de fuente de precio y desplazamiento opcional.
- **Relative Strength Index (RSI)** – oscilador a corto plazo que lee de la misma familia de precios de velas que en la versión MQL.

## Lógica de trading
1. Para cada vela completada, la estrategia calcula los valores de media móvil y RSI usando las fuentes de precio configuradas.
2. El valor de media móvil más reciente puede desplazarse por un número de barras definido por el usuario para coincidir con el comportamiento MQL.
3. Evalúa el PnL flotante de la posición neta actual:
   - Si el resultado flotante de todas las posiciones abiertas es **mayor que cero**, la estrategia cierra la posición completa para realizar la ganancia.
   - Si el resultado flotante es **negativo**, el lado con la pérdida menor (lado comprador vs. lado vendedor) se refuerza abriendo una operación adicional en esa dirección.
4. Si no hay señal de promediado, se aplica el filtro RSI + MA:
   - **Entrada corto** – RSI ≥ `RsiOverbought` y el precio de apertura de la vela está por debajo de la media móvil desplazada.
   - **Entrada largo** – RSI ≤ `RsiOversold` y el precio de apertura de la vela está por encima de la media móvil desplazada.

## Lógica de salida
- El PnL flotante positivo activa `CloseAllPositions`, aplanando la estrategia inmediatamente.
- Las señales de reversión manual desde la lógica de promediado cierran la exposición opuesta porque StockSharp trabaja con posiciones netas.

## Dimensionamiento de posición
`LotSizingModes` refleja la selección `OptLot` del EA:
- **Fixed** – siempre envía el volumen `LotSize`.
- **Balance** – convierte `PercentOfBalance` del valor de la cartera en volumen usando el precio de cierre de la vela.
- **Equity** – convierte `PercentOfEquity` del patrimonio actual de la cartera en volumen.

El volumen calculado se redondea al `Security.VolumeStep` más cercano (cuando esté disponible) para que las órdenes cumplan con el tamaño de lote del instrumento.

## Parámetros
| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|---------------------|
| `LotOption` | Modo de cálculo de volumen (`Fixed`, `Balance`, `Equity`). | `Balance` |
| `LotSize` | Valor de lote fijo para el modo `Fixed`. | `0.01` |
| `PercentOfBalance` | Porcentaje de saldo usado en el modo `Balance`. | `2` |
| `PercentOfEquity` | Porcentaje de patrimonio usado en el modo `Equity`. | `3` |
| `FastMaPeriod` | Longitud de la media móvil. | `4` |
| `FastMaShift` | Desplazamiento aplicado al resultado de la media móvil. | `0` |
| `FastMaMethod` | Método de cálculo de la media móvil (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `LinearWeighted` |
| `FastMaPrice` | Fuente de precio de vela para la media móvil. | `Open` |
| `RsiPeriod` | Longitud del RSI. | `4` |
| `RsiPrice` | Fuente de precio de vela para el RSI. | `Open` |
| `RsiOverbought` | Nivel RSI que define un mercado sobrecomprado. | `80` |
| `RsiOversold` | Nivel RSI que define un mercado sobrevendido. | `20` |
| `CandleType` | Serie de velas usada por la estrategia. | `Marco temporal de 15 minutos` |

## Fuentes de precio de vela
`CandlePriceSources` replica la lista de precios aplicados de MQL:
- `Open`, `High`, `Low`, `Close`
- `Median` = (High + Low) / 2
- `Typical` = (High + Low + Close) / 3
- `Weighted` = (High + Low + Close + Close) / 4

## Notas
- Las órdenes se generan solo cuando la estrategia está en línea y la vela ha terminado, coincidiendo con el EA original que se activa en nuevas barras.
- Dado que StockSharp mantiene una posición neta, las señales de promediado automáticamente reducen o invierten la exposición actual en lugar de crear posiciones de cobertura.
- La implementación en Python se omite intencionalmente según se solicitó.
