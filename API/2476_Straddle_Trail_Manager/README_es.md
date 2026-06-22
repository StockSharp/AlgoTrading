# Estrategia de Gestión de Trailing Straddle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de Gestión de Trailing Straddle** replica el comportamiento del Expert Advisor original de MetaTrader 5 "Straddle&Trail". La estrategia coloca un par de órdenes stop (un straddle) alrededor del precio actual antes de eventos de noticias programados o inmediatamente a demanda. Una vez que se activa una posición, el algoritmo gestiona las transiciones de break-even, trailing stops y comandos opcionales de cierre que cancelan órdenes pendientes o cierran posiciones abiertas.

Esta implementación está construida sobre la API de alto nivel de StockSharp. La colocación de órdenes, la gestión de posiciones y los controles de riesgo se implementan sin usar procesamiento de mensajes de bajo nivel.

## Lógica de trading

1. **Colocación del straddle**
   * Se crean dos órdenes stop (buy stop por encima y sell stop por debajo) una vez que se alcanza la ventana del evento programado o instantáneamente si `PlaceStraddleImmediately` está habilitado.
   * Los precios de las órdenes se desplazan del bid/ask actual por `DistanceFromPrice` (expresado en pips). El desplazamiento se convierte en unidades de precio usando el paso de precio del instrumento.
   * La estrategia evita recrear el straddle múltiples veces en el mismo día a menos que las órdenes sean ajustadas o canceladas explícitamente.

2. **Gestión de órdenes pre-evento**
   * Cuando `AdjustPendingOrders` está habilitado, las órdenes stop se cancelan y se recolocan cada minuto nuevo para que permanezcan alineadas con el precio actual.
   * Los ajustes se detienen `StopAdjustMinutes` antes del evento para evitar perseguir el precio cuando la volatilidad aumenta.
   * Si `RemoveOppositeOrder` está habilitado, la orden stop restante se cancela automáticamente una vez que un lado del straddle se activa y abre una posición.

3. **Gestión del riesgo**
   * Los niveles iniciales de stop-loss y take-profit se calculan a partir de `StopLossPips` y `TakeProfitPips` y se rastrean internamente.
   * Cuando el beneficio abierto alcanza `BreakevenTriggerPips`, el nivel de stop se mueve al precio de entrada más `BreakevenLockPips` (o el valor simétrico para operaciones cortas).
   * Si `TrailPips` es mayor que cero, un trailing stop sigue el precio. El trailing puede comenzar inmediatamente o solo después de la condición de break-even dependiendo de `TrailAfterBreakeven`.
   * Las salidas por take-profit y stop se ejecutan con órdenes de mercado por fiabilidad.

4. **Cierre manual**
   * Establecer `ShutdownNow` en `true` desencadena una limpieza inmediata según la opción `ShutdownMode`. Las acciones posibles incluyen cerrar posiciones largas/cortas y cancelar órdenes pendientes largas/cortas.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `ShutdownNow` | Activa el procedimiento de cierre en la próxima actualización de vela. Se reinicia automáticamente a `false` tras la ejecución. |
| `ShutdownMode` | Define qué se debe cancelar o cerrar (`All`, `LongPositions`, `ShortPositions`, `PendingLong`, `PendingShort`). |
| `DistanceFromPrice` | Distancia entre el precio actual y cada orden stop, medida en pips. |
| `StopLossPips` | Distancia inicial de stop-loss para posiciones activadas. Establecer en `0` para desactivar. |
| `TakeProfitPips` | Distancia inicial de take-profit. Establecer en `0` para desactivar. |
| `TrailPips` | Distancia del trailing stop. Establecer en `0` para desactivar el trailing. |
| `TrailAfterBreakeven` | Cuando es `true`, el trailing comienza solo después de que se satisface la condición de break-even. |
| `BreakevenLockPips` | Beneficio bloqueado cuando se activa el disparador de break-even. |
| `BreakevenTriggerPips` | Umbral de beneficio que activa la lógica de break-even. |
| `EventHour` / `EventMinute` | Hora del evento programado (hora del bróker/servidor). Establecer ambos en `0` para desactivar el programador de eventos. |
| `PreEventEntryMinutes` | Minutos antes del evento cuando se debe colocar el straddle. Se ignora cuando el evento está desactivado o cuando la colocación inmediata está habilitada. |
| `StopAdjustMinutes` | Número de minutos antes del evento cuando se detiene el ajuste automático de órdenes pendientes. |
| `RemoveOppositeOrder` | Cancela la orden stop no ejecutada cuando se activa la primera parte del straddle. |
| `AdjustPendingOrders` | Habilita el recentrado automático de órdenes pendientes mientras se espera el evento. |
| `PlaceStraddleImmediately` | Coloca el straddle justo después de que la estrategia comienza, omitiendo el programador de eventos. |
| `CandleType` | Suscripción de velas utilizada para el seguimiento del tiempo. Por defecto velas de 1 minuto. |

> **Volumen** – la propiedad `Volume` de StockSharp controla el tamaño de la orden. Está establecido en `1` por defecto y puede modificarse antes de iniciar la estrategia.

## Suscripciones de datos

La estrategia se suscribe a:

* La serie de velas configurada (por defecto 1 minuto) para ejecutar el programador, la lógica de trailing y las comprobaciones de cierre.
* El libro de órdenes para mantener un seguimiento de los últimos precios bid/ask para la alineación precisa de órdenes stop.

## Notas y limitaciones

* La gestión de stop-loss y take-profit se ejecuta mediante órdenes de mercado en lugar de modificar órdenes de protección del lado del bróker. Esto refleja el comportamiento original manteniendo la implementación simple.
* La estrategia usa el `PriceStep` del instrumento para aproximar el tamaño del pip. Para instrumentos exóticos, ajuste los parámetros en consecuencia.
* El comando de cierre se evalúa solo cuando llegan nuevos datos de velas. Para una acción inmediata, reduzca el marco temporal de las velas.
* La implementación en Python se omite intencionalmente según lo solicitado.

## Notas de conversión

* La lógica de break-even y trailing se porta línea por línea de la versión MQL. La versión StockSharp mantiene las mismas relaciones numéricas pero opera con precios decimales y usa salidas de mercado.
* El manejo de trades manuales (magic number `0` en MQL) no se reproduce porque las estrategias de StockSharp gestionan sus propias posiciones. Toda la lógica de protección se aplica solo a los trades generados por la estrategia.
* La función `CalcMagic` es innecesaria en StockSharp y por lo tanto fue eliminada. El estado de la estrategia es rastreado internamente por el framework.

