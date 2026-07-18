# Estrategia de Retroceso y Rebote
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Retroceso y Rebote es una conversión C# del asesor experto MQL5 "TST (barabashkakvn's edition)". Monitorea un único instrumento en el marco temporal especificado por el parámetro `CandleType` y busca movimientos fuertes que retrocedan de vuelta dentro del rango de la barra. Cuando una barra alcista se desvanece desde su máximo más del umbral de retroceso, la estrategia compra, mientras que un retroceso bajista equivalente desencadena una venta. La implementación usa la API de suscripción de velas de alto nivel de StockSharp y gestiona todas las órdenes de protección en unidades de pip que se convierten en desplazamientos de precio absolutos.

Las distancias en pip se calculan desde el `PriceStep` del instrumento. Para los símbolos que cotizan con tres o cinco decimales, la estrategia automáticamente multiplica el paso por diez para coincidir con la definición de pip de MetaTrader. Todo el dimensionamiento de posición se toma de la propiedad base `Volume` de la estrategia.

## Lógica de entrada
- Procesar solo velas terminadas de la serie `CandleType` configurada.
- Con `ReverseSignal = false` (predeterminado):
  - **Configuración larga:** la vela cierra por debajo de su apertura y la diferencia entre el máximo de la vela y el cierre excede `RollbackRatePips` (convertido a precio). Esto indica que el precio se expandió hacia arriba y luego retrocedió lo suficiente para calificar para una entrada contraria larga.
  - **Configuración corta:** la vela cierra por encima de su apertura y la diferencia entre el cierre y el mínimo de la vela excede `RollbackRatePips`. Esto refleja la lógica larga en el lado bajista.
- Cuando `ReverseSignal = true`, los roles de las condiciones larga y corta se intercambian, permitiendo al trader cambiar la dirección sin cambiar los otros parámetros.
- Las nuevas entradas solo se colocan cuando la posición actual está plana o en la dirección opuesta. El volumen ejecutado es igual a `Volume + |Position|` para que una posición opuesta se cierre antes de establecer el nuevo trade.

## Lógica de salida
- En la entrada, la estrategia almacena los niveles de stop-loss y take-profit basados en los desplazamientos de pip configurados. Cuando el rango de la vela toca un nivel, la posición se cierra con una orden de mercado.
- `StopLossPips = 0` o `TakeProfitPips = 0` deshabilita el nivel de protección correspondiente.
- La lógica de trailing se activa una vez que la ganancia flotante supera `TrailingStopPips + TrailingStepPips` (en términos de precio).
  - Para trades largos, el stop se mueve escalonadamente a `precio más alto - TrailingStopPips` siempre que el nuevo nivel esté al menos `TrailingStepPips` por encima del stop anterior.
  - Para trades cortos, el stop se mueve escalonadamente a `precio más bajo + TrailingStopPips` cuando el nuevo nivel está al menos `TrailingStepPips` por debajo del stop anterior.
  - Si el mercado se revierte y cruza el trailing stop, la posición se cierra inmediatamente.
- Cuando no hay posición abierta, todas las variables de estado internas se borran para evitar datos obsoletos.

## Parámetros
| Parámetro | Descripción | Valor predeterminado |
| --- | --- | --- |
| `CandleType` | Serie de velas utilizada para el cálculo de señales. | Marco temporal de 15 minutos |
| `StopLossPips` | Distancia del stop de protección en pips. Establecer en cero para deshabilitar. | 30 |
| `TakeProfitPips` | Distancia del take-profit en pips. Establecer en cero para deshabilitar. | 90 |
| `TrailingStopPips` | Offset del trailing stop en pips. Establecer en cero para deshabilitar el trailing. | 1 |
| `TrailingStepPips` | Ganancia extra (en pips) requerida antes de que el trailing stop pueda moverse de nuevo. Debe ser positivo cuando el trailing está habilitado. | 15 |
| `RollbackRatePips` | Retroceso mínimo desde el extremo de la barra que valida una señal. | 15 |
| `ReverseSignal` | Invierte la dirección de entrada (las señales largas se convierten en cortas y viceversa). | false |

## Notas de uso
- Establecer la propiedad `Volume` antes de iniciar la estrategia; define la cantidad operada para cada orden.
- El trailing requiere `TrailingStopPips > 0` y `TrailingStepPips > 0`. La estrategia lanza un error al inicio si esta relación se viola.
- Debido a que el experto original evaluaba ticks dentro de la barra activa, el porto C# usa el máximo/mínimo/cierre de la vela terminada para aproximar el mismo comportamiento. La diferencia es insignificante para la mayoría de los backtests y mantiene la implementación alineada con la API de alto nivel de StockSharp.
- La estrategia funciona con un solo instrumento. Para operar múltiples instrumentos, crear instancias de estrategia separadas.
