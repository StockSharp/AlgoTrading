# ADX Estrategia cruzada DI del sistema
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia del sistema ADX es la StockSharp conversión del MetaTrader 4 expertos `ADX_System.mq4`. El EA original compara el
Índice direccional promedio (ADX) con sus componentes +DI y -DI en las dos velas completadas más recientes. Cuando la línea +DI
sube por encima del valor ADX el sistema quiere que sea largo; cuando la línea -DI supera el valor ADX, quiere ser corta. el
El puerto StockSharp reproduce este comportamiento almacenando los valores del indicador de las dos velas terminadas anteriores para que la lógica
refleja las llamadas `iADX(..., shift=1/2)` utilizadas en el código MetaTrader.

Sólo puede haber una posición abierta a la vez. La estrategia envía órdenes de mercado para entradas y salidas, igualando el billete único.
lógica de MetaTrader cuentas de compensación. La gestión de riesgos refleja el asesor experto original: toma de ganancias fija y stop-loss
Los niveles se expresan en puntos en relación con el precio de entrada promedio, y un trailing stop opcional puede asegurar ganancias una vez que el
la posición evoluciona favorablemente.

## Lógica comercial
1. Suscríbase al período de tiempo configurado (`CandleType`) y procese solo velas terminadas para evitar decisiones dentro de la barra.
2. Alimente un indicador `AverageDirectionalIndex` con los datos de la vela y espere hasta que el indicador proporcione sus ADX, +DI y -DI.
valores.
3. Almacene en caché las lecturas del indicador de las dos velas terminadas más recientes para que la estrategia pueda hacer referencia a las velas "actuales" y
valores "anteriores" exactamente iguales a la implementación MetaTrader.
4. **Entrada larga**: si el ADX (`shift = 2`) más antiguo está por debajo del ADX (`shift = 1`) más reciente, el +DI más antiguo está por debajo del más antiguo
ADX, y el +DI más reciente está por encima del ADX más reciente, envíe una orden de compra de mercado.
5. **Entrada breve**: si aparecen las mismas condiciones para el componente -DI (antiguo -DI debajo del antiguo ADX, nuevo -DI encima del nuevo ADX), envíe un
orden de venta de mercado.
6. **Salida larga**: cierra la posición larga cuando el ADX comienza a caer y +DI vuelve a cruzar por debajo de él, cuando el valor configurado
se alcanza la toma de ganancias o el stop-loss, o cuando se supera el trailing stop.
7. **Salida corta**: refleja la lógica de salida larga usando -DI junto con los controles de riesgo.
8. Actualice el historial del indicador en caché después de cada vela para que la siguiente señal utilice el último par `shift = 1/2`.

## Gestión de riesgos
- `TakeProfitPoints` y `StopLossPoints` describen distancias en puntos de estilo MetaTrader. Se convierten a unidades de precio reales.
usando `Security.PriceStep` cuando esté disponible; de lo contrario, el valor bruto se trata como un delta de precio absoluto.
- El trailing stop (`TrailingStopPoints`) se activa solo después de que la posición gana al menos la distancia configurada desde el
precio de entrada. Una vez activo, se mueve en dirección a la ganancia y cierra la posición cuando el precio cruza el nivel final.
- Todas las salidas (inversión de indicador, toma de ganancias, stop-loss, trailing stop) utilizan órdenes de mercado, por lo que la posición se aplana.
inmediatamente, imitando el comportamiento `OrderClose` de la fuente EA.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | marco de tiempo de 1 minuto | Plazo primario procesado por la estrategia. |
| `AdxPeriod` | `int` | `14` | Número de velas utilizadas para calcular los componentes ADX y DI. |
| `TradeVolume` | `decimal` | `1` | Tamaño de lote utilizado para cada orden de mercado. |
| `TakeProfitPoints` | `decimal` | `100` | Distancia de obtención de beneficios en puntos respecto al precio de entrada. |
| `StopLossPoints` | `decimal` | `30` | Distancia del stop-loss en puntos con respecto al precio de entrada. |
| `TrailingStopPoints` | `decimal` | `0` | Distancia de trailing-stop opcional en puntos. Establezca en cero para desactivar el seguimiento. |

## Diferencias con el experto MetaTrader original
- MetaTrader gestiona tickets individuales mientras que StockSharp trabaja con una única posición neta. Por lo tanto, la conversión cierra el
posición actual antes de emitir una nueva orden de entrada cuando la señal cambia.
- El EA original se basó en `Point` para convertir puntos en distancias de precios. El puerto StockSharp usa `Security.PriceStep` cuando
es conocido; de lo contrario, la distancia se trata como unidades de precio bruto, por lo que es posible que deba ajustar los valores predeterminados para los instrumentos con
pasos de precios no convencionales.
- MetaTrader aplica paradas dinámicas modificando el orden existente. StockSharp cierra la posición con una orden de mercado cuando el
Se viola el trailing stop, que es funcionalmente equivalente pero más simple dentro del modelo de compensación.

## Consejos de uso
- Asegúrese de que el volumen de la estrategia (`TradeVolume`) se alinee con el paso del lote del instrumento. El constructor también asigna este valor a
`Strategy.Volume`, por lo que los métodos auxiliares utilizan el tamaño comercial esperado.
- Aumente `TakeProfitPoints` y `StopLossPoints` si opera con instrumentos con rangos promedio más grandes o incrementos de precios más pequeños.
- Agregue la estrategia a un gráfico para visualizar las velas, el indicador ADX y las operaciones ejecutadas, lo que ayuda a verificar esas señales.
ocurre exactamente cuando +DI o -DI cruza por encima de la línea ADX.

## Indicadores
- `AverageDirectionalIndex` (proporciona ADX junto con los componentes +DI y -DI).
