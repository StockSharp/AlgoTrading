# Estrategia de gomas 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una versión StockSharp del asesor experto MetaTrader 4 **RUBBERBANDS_3**. Mantiene dos precios extremos, abre posiciones adicionales cada vez que el precio se expande en una distancia configurable y liquida toda la secuencia una vez que se produce un contramovimiento de un tamaño determinado. Después de un retroceso, la estrategia opcionalmente gira en la dirección opuesta mientras monitorea un objetivo de pérdidas y ganancias a nivel de sesión.

> **Nota:** StockSharp opera en posiciones neteadas. El script MT4 original puede mantener órdenes largas y cortas simultáneamente, pero el puerto cierra la secuencia activa antes de cambiar de dirección. Se preserva el comportamiento general de escalar hacia las tendencias y revertir los retrocesos.

## Lógica de trading

1. Registre el precio de cierre actual como máximo y mínimo (o reutilice los valores guardados al reiniciar).
2. Cuando el precio suba `PipStep` puntos por encima del máximo actual, envíe una orden de compra de mercado de tamaño `OrderVolume` y actualice el máximo al nuevo precio.
3. Cuando el precio caiga `PipStep` puntos por debajo del mínimo actual, envíe una orden de venta de mercado de tamaño `OrderVolume` y actualice el mínimo.
4. Si el mercado retrocede `BackStep` puntos en contra de la dirección activa, cierre todas las posiciones en esa dirección y establezca una reversión. El lado opuesto se abre una vez liquidada por completo la secuencia anterior.
5. Supervise el resultado acumulado de la sesión. Si el beneficio realizado más abierto alcanza `SessionTakeProfit` × `OrderVolume`, cierre la sesión. Cuando la reducción durante la reversión exceda `SessionStopLoss` × `OrderVolume`, cierre todo también.
6. La palanca `QuiesceNow` evita nuevas operaciones cuando la estrategia es plana. El indicador `StopNow` detiene toda la lógica y `CloseNow` solicita un aplanamiento inmediato de la cartera.

Los pedidos se generan a partir de velas terminadas del `CandleType` configurado. El período de tiempo predeterminado es un minuto, que coincide con el tiempo del EA original que activó las comprobaciones al comienzo de cada minuto.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Tamaño base de cada orden de mercado. | `0.02` |
| `MaxOrders` | Número máximo de posiciones concurrentes en una sola dirección. Las entradas adicionales se bloquean cuando se alcanza el límite. | `10` |
| `PipStep` | Distancia de expansión en puntos que suma un nuevo comercio. | `100` |
| `BackStep` | Contramovimiento en puntos que fuerza una salida y prepara una reversión. | `20` |
| `QuiesceNow` | Cuando `true`, la estrategia permanece inactiva mientras no haya posiciones abiertas. | `false` |
| `DoNow` | Abre la primera secuencia larga inmediatamente después de que comienza la estrategia. | `false` |
| `StopNow` | Bandera de parada dura que impide cualquier procesamiento posterior. Las posiciones existentes permanecen intactas. | `false` |
| `CloseNow` | Solicita una posición plana inmediata, provocando cierres secuenciales. | `false` |
| `UseSessionTakeProfit` | Habilita la toma de ganancias acumulada de la sesión. | `true` |
| `SessionTakeProfit` | Beneficio objetivo en la moneda de la cuenta por lote utilizado para cerrar la sesión. | `2000` |
| `UseSessionStopLoss` | Habilita el stop-loss acumulado de la sesión. | `true` |
| `SessionStopLoss` | Pérdida máxima tolerada por lote al revertir antes del cierre de la sesión. | `4000` |
| `UseInitialValues` | Al reiniciar, reutilice los `InitialMax` y `InitialMin` proporcionados manualmente en lugar del último precio de cierre. | `false` |
| `InitialMax` | El extremo superior almacenado se reutiliza cuando `UseInitialValues` está habilitado. | `0` |
| `InitialMin` | El extremo inferior almacenado se reutiliza cuando `UseInitialValues` está habilitado. | `0` |
| `CandleType` | Serie de velas procesadas por la estrategia. El valor predeterminado es velas de un minuto. | `TimeFrame(1m)` |

## Gestión de sesiones

- **Agregación de ganancias:** las ganancias realizadas se acumulan después de cada cierre completo, mientras que las ganancias no realizadas se recalculan a partir de los precios de entrada promedio ponderados de todas las posiciones abiertas.
- **Obtención de beneficios de la sesión:** una vez que se alcanza `SessionTakeProfit`, la estrategia cierra todas las operaciones y restablece los extremos almacenados.
- **Stop-loss de sesión:** durante una secuencia de reversión (`BackStep` activada), la estrategia rastrea la pérdida flotante. Si la reducción excede `SessionStopLoss`, todas las posiciones se liquidan y la sesión se reinicia con las estadísticas borradas.

## Notas de uso

- El paso de precio utilizado para convertir puntos en precios se toma de `Security.PriceStep`. Configure los metadatos del instrumento en consecuencia; de lo contrario, se aplica una reserva de `0.0001`.
- Debido a que las órdenes se compensan, la estrategia ejecuta operaciones de cierre antes de abrir en la dirección opuesta. Al migrar datos heredados, tenga en cuenta que el historial de pedidos puede diferir del de las plataformas cubiertas.
- La bandera `DoNow` solo abre la primera posición larga. Las entradas adicionales siguen las condiciones de ruptura habituales.
- Utilice `QuiesceNow` cuando desee dejar la estrategia cargada pero inactiva después de que aplana el libro.
