# Estrategia iTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un gestor manual de ventas convertido desde el asesor experto de MetaTrader **iTrade**. Recrea el flujo de botones del gráfico del EA original: cada vez que el usuario solicita una venta, se abre una posición martingala. Luego la estrategia observa la ganancia flotante de todas las operaciones cortas y liquida los tickets más y menos rentables cuando se alcanzan objetivos de ganancia predefinidos.

## Lógica central

- Las órdenes se abren solo por solicitudes explícitas del usuario. Llame a `QueueSellRequest()` para simular la pulsación del botón de MetaTrader.
- La primera posición usa el **volumen inicial** configurado. Después de cada operación perdedora, el tamaño de la siguiente orden se multiplica por el **multiplicador martingala**. Las operaciones rentables reinician la secuencia al volumen base.
- La ganancia flotante se mide usando el mejor precio ask actual. Cuando la ganancia media por operación abierta alcanza el **objetivo de ganancia media**, la estrategia cierra las operaciones más rentable y menos rentable del lote activo (hasta **conteo base de operaciones**).
- Si hay más de **conteo base de operaciones** posiciones abiertas, se aplica el **objetivo de ganancia extendido** más estricto antes de cerrar dos operaciones.
- Los cálculos de ganancia dependen de los valores `PriceStep` y `StepPrice` del valor. La estrategia lanza una excepción durante el arranque cuando faltan.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `InitialVolume` | Tamaño de lote base usado para la primera orden martingala. |
| `MartingaleMultiplier` | Multiplicador aplicado después de cada operación perdedora. |
| `AverageProfitTarget` | Ganancia flotante media (en divisa) requerida para cerrar operaciones dentro del primer lote. |
| `ExtendedAverageProfitTarget` | Umbral de ganancia flotante media cuando está activo más que el lote base. |
| `BaseTradeCount` | Número de operaciones consideradas parte del lote inicial. |
| `ControlInterval` | Frecuencia de comprobaciones internas (intervalo del temporizador). |

## Notas de uso

1. Configure `Security`, `Portfolio` y cualquier parámetro deseado antes de iniciar la estrategia.
2. Llame a `QueueSellRequest()` cada vez que deba abrirse una nueva venta. La estrategia dimensionará la orden según las reglas martingala y enviará una venta de mercado.
3. El algoritmo almacena un historial de resultados de operaciones cerradas (hasta 200 entradas) para reproducir el comportamiento martingala original.
4. Las órdenes de cierre se envían como compras de mercado por el volumen exacto de las operaciones objetivo.

## Diferencias con la versión MetaTrader

- La versión MetaTrader dependía de botones en el gráfico; aquí el usuario dispara ventas programáticamente mediante `QueueSellRequest()`.
- La ejecución de órdenes se gestiona mediante órdenes de mercado de StockSharp. Las ejecuciones parciales se agregan automáticamente por la estrategia.
- Los umbrales de ganancia operan sobre valores decimales de divisa usando `StepPrice`, mientras que el EA original usaba funciones de ganancia de tickets de MetaTrader.
