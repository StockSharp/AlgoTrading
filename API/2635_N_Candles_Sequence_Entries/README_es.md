# Estrategia N Candles con Entradas en Secuencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Concepto
La estrategia N Candles escanea el mercado en busca de velas consecutivas que cierran todas en la misma dirección. Una vez que ha aparecido un número configurable de velas alcistas o bajistas, la estrategia entra en la dirección de la secuencia. La implementación es una conversión directa del asesor experto de MetaTrader "N Candles v4" y preserva sus controles de riesgo, configuración basada en pips y comportamiento opcional de trailing stop dentro de la API de alto nivel de StockSharp.

## Criterios de entrada
- Cada vela terminada se evalúa una vez.
- Las velas que cierran al alza se cuentan como alcistas, las que cierran a la baja como bajistas, y las velas doji reinician la secuencia.
- Cuando aparecen `ConsecutiveCandles` velas alcistas (o bajistas) en una fila, la estrategia envía una orden de mercado en la dirección del movimiento.
- Se aplican límites de apilamiento estilo cobertura o límites de exposición estilo neteado dependiendo del `AccountingMode` seleccionado.

## Gestión de salidas
- `StopLossPips` y `TakeProfitPips` definen niveles de salida estáticos medidos en pips desde el precio de entrada promedio de la posición activa.
- Si `TrailingStopPips` es mayor que cero, el nivel del stop sigue al precio más favorable:
  - Cuando no existe un stop fijo (por ejemplo cuando `StopLossPips` es cero), la estrategia espera hasta que el precio se mueva `TrailingStopPips` a favor de la operación antes de colocar un stop de equilibrio.
  - Una vez que se ha establecido un stop, se mueve hacia el mercado cuando la distancia entre el precio y el stop supera `TrailingStopPips + TrailingStepPips`.
- Los niveles de protección se recalculan cuando cambia el tamaño de la posición y se comprueban contra cada vela terminada, garantizando que cualquier evento de stop-loss o take-profit cierre la operación inmediatamente.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `ConsecutiveCandles` | Número de velas idénticas requeridas para disparar una entrada. | 3 |
| `TakeProfitPips` | Distancia del take-profit en pips. Usar cero para deshabilitar el objetivo. | 50 |
| `StopLossPips` | Distancia del stop-loss en pips. Usar cero para deshabilitar el stop. | 50 |
| `TrailingStopPips` | Distancia del trailing stop en pips. Cero deshabilita el trailing. | 10 |
| `TrailingStepPips` | Movimiento adicional requerido antes de que el trailing stop avance. | 4 |
| `MaxPositionsPerDirection` | Número máximo de entradas apiladas por dirección en cobertura. | 2 |
| `MaxNetVolume` | Tamaño máximo de posición neta absoluta al operar en modo neteado. | 2 |
| `AccountingMode` | Cambiar entre `Netting` (límite de volumen) y `Hedging` (límite de recuento de entradas). | Netting |
| `CandleType` | Agregación de velas usada para la detección de patrones. | Velas de 1 minuto |

Todos los parámetros basados en pips se convierten a offsets de precio usando el tamaño del tick del instrumento. Si el instrumento tiene 3 o 5 decimales, el tamaño del pip se escala por un factor de diez para reflejar la definición de MetaTrader.

## Notas de implementación
- La estrategia depende de la suscripción de velas de alto nivel de StockSharp (`SubscribeCandles`) y evita buffers de historial manuales.
- La lógica de protección hace un seguimiento del precio más alto (para largos) o más bajo (para cortos) visto después de la entrada para emular el comportamiento de trailing original.
- Los límites de posición se adaptan automáticamente al `Volume` base de la estrategia. Aumentar `Volume` expande los tamaños de órdenes de stop y take-profit proporcionalmente.
- Se emiten mensajes de registro cada vez que una salida de protección (stop o take-profit) cierra una posición, proporcionando claridad durante los backtests.

## Consejos de uso
- Elegir el modo `Hedging` cuando se simulan plataformas que permiten múltiples tickets por dirección, o quedarse con `Netting` para reflejar cuentas de posición única.
- Establecer `TrailingStepPips` en cero para un trailing stop clásico que se mueve cada vez que el mercado avanza `TrailingStopPips`.
- Debido a que las salidas se evalúan en velas completadas, considerar un intervalo de velas más corto si la precisión intrabar es crítica.
