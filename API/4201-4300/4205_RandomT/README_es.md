# Estrategia aleatoria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp del MetaTrader 4 asesores expertos "RandomT". El EA original espera una oscilación en ZigZag que coincida con un fractal confirmado y luego filtra la entrada con una comparación MACD. La versión StockSharp mantiene el mismo proceso de decisión: observa un número configurable de velas (`BarWatch`), confirma que un fractal de cinco barras marca el extremo de oscilación más reciente y solo opera cuando la línea principal MACD está por encima o por debajo de la línea de señal en la misma barra histórica.

## Lógica comercial
- Cree buffers de velas móviles y calcule la señal MACD en cada barra terminada del período de tiempo seleccionado (`CandleType`).
- Mire `Shift` barras hacia el pasado y compruebe si esa barra forma un fractal hacia arriba o hacia abajo (dos velas a cada lado).
- Valide el fractal frente a la acción del precio circundante: el máximo debe ser el valor más grande, o el mínimo, el valor más pequeño, dentro de la ventana retrospectiva `BarWatch`. Esto refleja la confirmación de swing en ZigZag utilizada por la versión MetaTrader.
- Para una configuración breve, el valor principal MACD debe ser mayor que el valor de la señal en la barra desplazada. Para una configuración larga, la comparación opuesta debe ser cierta.
- Cuando aparece una señal, la estrategia utiliza una orden de mercado única cuyo volumen neutraliza cualquier posición opuesta antes de abrir la nueva operación.

## Gestión de trailing stop
- El bloque final se activa solo cuando `UseTrailingProfit` está habilitado y la ganancia flotante (convertida a través de `PriceStep` y `StepPrice`) excede `MinProfit`.
- La distancia de seguimiento se mide en puntos de precio. Cuando `AutoStopLevel` es `true`, el motor usa `StartStopLevelPoints`; de lo contrario utiliza `StopLevelPoints`.
- Para posiciones largas, la parada sigue a `ClosePrice - distance`, para posiciones cortas sigue a `ClosePrice + distance`. Si la vela atraviesa el nivel de stop, la estrategia cierra la operación con una orden de mercado.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Tamaño comercial base en lotes utilizados para cada entrada. |
| `BarWatch` | Número de barras utilizadas para validar que un fractal es también un extremo de oscilación en ZigZag. |
| `Shift` | Número de barras en el historial que se evalúan en busca de señales. Debería permanecer en 2 para los fractales clásicos. |
| `UseTrailingProfit` | Habilita la lógica del trailing stop. |
| `AutoStopLevel` | Cambia la distancia de seguimiento a `StartStopLevelPoints`. |
| `StartStopLevelPoints` | Distancia de seguimiento alternativa (puntos). |
| `StopLevelPoints` | Distancia de seguimiento primaria (puntos). |
| `MinProfit` | Se requiere un beneficio flotante mínimo (moneda de la cuenta) antes de aplicar el seguimiento. |
| `CandleType` | Marco de tiempo utilizado para velas y cálculos de indicadores. |
| `MacdFastLength` | Período EMA rápida para el filtro MACD. |
| `MacdSlowLength` | Período EMA lento para el filtro MACD. |
| `MacdSignalLength` | Periodo de señal EMA para el filtro MACD. |

## Notas
- La estrategia calcula fractales internamente (dos barras en cada lado) y reutiliza el resultado para la validación de ZigZag, coincidiendo estrechamente con los buffers a los que se accede en el código MQL.
- La confirmación del ZigZag se aproxima comprobando las velas `BarWatch` circundantes en lugar de volver a ejecutar el indicador MetaTrader completo, lo que mantiene el comportamiento determinista dentro de StockSharp.
- El beneficio del trailing-stop se deriva de los `PriceStep` y `StepPrice` del instrumento. Verifique estos valores para su instrumento antes de ejecutar la estrategia.
