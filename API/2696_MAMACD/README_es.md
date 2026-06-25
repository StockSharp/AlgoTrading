# Estrategia MAMACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión directa del asesor experto de MetaTrader 5 **MAMACD (edición de barabashkakvn)** de la carpeta `MQL/19334` a la API de alto nivel de StockSharp. El enfoque combina la detección de tendencia en precios mínimos mediante dos medias móviles ponderadas linealmente (LWMA) con un disparador de media móvil exponencial (EMA) rápida y confirmación de la línea principal de MACD. Las operaciones se realizan una vez por vela completada y mantienen la lógica del EA original, incluidos los indicadores de reinicio que requieren que la EMA rápida salga del canal LWMA antes de permitir una nueva entrada.

## Indicadores
- **LWMA #1 (precio mínimo, predeterminado 85)** – filtro de línea base lento aplicado a los mínimos de las velas.
- **LWMA #2 (precio mínimo, predeterminado 75)** – filtro ligeramente más rápido sobre los mínimos de las velas para confirmación del canal.
- **Disparador EMA (precio de cierre, predeterminado 5)** – disparador de Momentum que debe cruzar por encima/por debajo de ambas LWMA para armar una operación.
- **Línea principal MACD (rápida 15, lenta 26)** – filtro de confirmación; los largos requieren MACD positivo o ascendente, los cortos requieren MACD negativo o descendente.

## Lógica de entrada
1. La estrategia espera únicamente velas completadas (`CandleStates.Finished`).
2. Cuando la EMA disparadora cae por debajo de ambas LWMA, se establece una **indicador de listo para largo**. Una posición larga puede abrirse una vez que la EMA regresa por encima de ambas LWMA **y** el MACD está por encima de cero o es mayor que su valor anterior. Solo se puede abrir una posición larga a la vez.
3. Cuando la EMA disparadora sube por encima de ambas LWMA, se establece una **indicador de listo para corto**. Una posición corta puede abrirse después de que la EMA regresa por debajo de ambas LWMA y el MACD está por debajo de cero o es menor que su valor anterior. Solo hay una posición corta activa a la vez.
4. El dimensionamiento de posición usa la propiedad `Volume` de la estrategia. Al cambiar de dirección, el algoritmo cierra primero la exposición contraria.

## Lógica de salida
- No se codifica lógica de salida discrecional en el EA original. Las órdenes de protección se gestionan a través del `StartProtection` de StockSharp con distancias opcionales de stop-loss y take-profit medidas en pips. Alcanzar cualquiera de las protecciones cierra la posición automáticamente.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `FirstLowMaLength` | Período de la primera LWMA aplicada a precios mínimos (predeterminado 85). |
| `SecondLowMaLength` | Período de la segunda LWMA aplicada a precios mínimos (predeterminado 75). |
| `TriggerEmaLength` | Período del disparador EMA rápido en precios de cierre (predeterminado 5). |
| `MacdFastLength` | Longitud de la EMA rápida de la línea principal MACD (predeterminado 15). |
| `MacdSlowLength` | Longitud de la EMA lenta de la línea principal MACD (predeterminado 26). |
| `StopLossPips` | Distancia de stop-loss en pips; poner en cero para deshabilitar (predeterminado 15). |
| `TakeProfitPips` | Distancia de take-profit en pips; poner en cero para deshabilitar (predeterminado 15). |
| `CandleType` | Marco temporal de las velas procesadas por la estrategia (predeterminado 1 hora). |

## Notas de implementación
- El tamaño del pip se deriva de `Security.PriceStep`. Para símbolos de 3 y 5 dígitos, el código multiplica automáticamente el paso por 10 para imitar la definición de pip de MT5.
- El búfer de historial MACD coincide con el EA: el primer valor MACD válido se almacena y se usa como referencia para la barra siguiente antes de evaluar las señales.
- Los indicadores `_readyForLong` y `_readyForShort` replican la máquina de estados `startb`/`starts` original, asegurando que el precio tenga que salir del canal LWMA antes de tomar una nueva operación.
- Las áreas del gráfico visualizan la serie de precios con medias móviles y un panel MACD separado para facilitar la verificación de la conversión.

## Mapeo de conversión
| Elemento MT5 | Equivalente en StockSharp |
| --- | --- |
| `iMA` en mínimo/cierre | `WeightedMovingAverage` (alimentación de mínimos) y `ExponentialMovingAverage` (alimentación de cierres) |
| Línea principal `iMACD` | Salida principal de `MovingAverageConvergenceDivergence` |
| Verificaciones de posición (`buy`, `sell`) | Signo de `Position` y manejo de volumen vía `BuyMarket` / `SellMarket` |
| Número mágico y deslizamiento | No requerido en la API de alto nivel de StockSharp |
| Stop-loss / Take-profit (pips) | `StartProtection` con desplazamientos de precio absolutos calculados desde el tamaño del pip |

El comportamiento resultante refleja la versión MT5 mientras aprovecha el ciclo de vida de estrategia, el enlace de indicadores y los asistentes de gestión de riesgo de StockSharp.
