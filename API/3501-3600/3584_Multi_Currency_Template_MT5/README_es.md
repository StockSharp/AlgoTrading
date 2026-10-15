# Plantilla Multimoneda Estrategia MT5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia MT5 de plantilla multimoneda** replica el comportamiento del asesor experto MetaTrader con el mismo nombre. Opera con un patrón simple de dos velas en el período de tiempo diario y al mismo tiempo permite al usuario operar una canasta de instrumentos simultáneamente. La estrategia abre una posición inicial sólo cuando la vela diaria anterior es lo suficientemente alcista o bajista como para activar el patrón, luego gestiona la operación en un marco de tiempo de control más rápido. Un bloque de promedio de martingala agrega boletos adicionales cuando el precio se mueve contra la posición en un número configurable de MetaTrader puntos, mientras que la lógica de salida combina toma de ganancias fija, promedio de equilibrio y un trailing stop opcional.

El puerto StockSharp mantiene la gestión de múltiples símbolos al permitir al usuario definir una lista de valores separados por comas. Cada símbolo se maneja de forma independiente con su propio contexto de seguimiento, cesta de posición y valores de gestión de dinero. Cuando el parámetro `TradeMultipair` está deshabilitado, la estrategia intercambia el `Security` principal adjunto a la instancia de la estrategia.

## Generación de señal

* La estrategia se suscribe al `SignalCandleType` (diario por defecto) y almacena dos velas terminadas consecutivas.
* Se detecta una configuración **larga** cuando el último cierre está por debajo de la apertura anterior y la vela anterior cerró por encima de su apertura.
* Se detecta una configuración **corta** cuando el último cierre está por encima de la apertura anterior y la vela anterior cerró por debajo de su apertura.
* Sólo una dirección puede estar activa en cualquier momento. Las nuevas operaciones se ignoran hasta que la cesta actual esté completamente cerrada.

## Ejecución de órdenes

* Las inscripciones se envían en el mercado con el volumen definido por `Lots`.
* Cuando `NewBarTrade` está habilitado, la estrategia espera a que finalice una vela el `TradeCandleType` antes de armar una nueva entrada. La bandera se consume en la primera decisión comercial para replicar el comportamiento MetaTrader de "comerciar solo en una barra nueva".
* Los objetivos de stop-loss y take-profit se inicializan usando MetaTrader pips (multiplicados por el tamaño del pip detectado) para que la distancia coincida con el experto original.
* Si `EnableMartingale` es verdadero, la estrategia agrega boletos promedio cada vez que el precio se aleja `StepPoints` de la mejor entrada de la cesta actual. Los volúmenes se escalan en `NextLotMultiplier` elevado al número de tickets ya abiertos en ese lado.

## Gestión comercial

* El comportamiento de obtención de beneficios depende de `EnableTakeProfitAverage`:
  * Cuando está deshabilitada, la toma de ganancias permanece a la distancia inicial definida por `TakeProfitPips` del mejor precio de la cesta.
  * Cuando está habilitado y la cesta contiene al menos dos boletos, el objetivo se desplaza al precio de equilibrio más `TakeProfitOffsetPoints`.
* Los niveles de stop-loss se recalculan después de cada ejecución para que reflejen el peor precio de la cesta.
* Un trailing stop actúa cuando solo hay un ticket abierto. Reproduce la lógica MetaTrader saltando primero al punto de equilibrio más `TrailingStopPoints` una vez que el movimiento excede `TrailingStopPoints + TrailingStepPoints`, luego siguiendo el precio con la misma distancia una vez que la operación sigue avanzando.
* Las salidas de riesgo desencadenan una orden de mercado que cierra la cesta completa en una transacción por lado.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `Lots` | Volumen base de operaciones para el primer billete de cada cesta. |
| `StopLossPips` | Distancia inicial de stop-loss expresada en MetaTrader pips. |
| `TakeProfitPips` | Distancia inicial de obtención de beneficios en MetaTrader pips. |
| `TrailingStopPoints` | Distancia de seguimiento (MetaTrader puntos) cuando solo hay un ticket activo. |
| `TrailingStepPoints` | Se requiere un buffer adicional (puntos) antes de que el trailing stop se mueva nuevamente. |
| `SlippagePoints` | Reservado para análisis para imitar la entrada de deslizamiento MetaTrader (no se utiliza para la ejecución). |
| `NewBarTrade` | Habilita el filtro de intercambio en barra nueva basado en las velas `TradeCandleType`. |
| `TradeCandleType` | Cronograma de latidos que impulsa la detección de nuevas barras y la administración del dinero. |
| `TradeMultipair` | Cuando es verdadero, activa el modo multisímbolo. |
| `PairsToTrade` | Lista separada por comas de identificadores de seguridad adicionales resueltos mediante `GetSecurity`. |
| `Commentary` | Comentario del pedido conservado como referencia. |
| `EnableMartingale` | Activa el bloque promediador que agrega tickets en movimientos adversos. |
| `NextLotMultiplier` | Multiplicador aplicado al volumen de tickets anterior cuando se realiza una nueva orden promedio. |
| `StepPoints` | Distancia en MetaTrader puntos que activa la siguiente orden promedio. |
| `EnableTakeProfitAverage` | Habilita el objetivo de equilibrio + compensación para cestas con múltiples boletos. |
| `TakeProfitOffsetPoints` | MetaTrader puntos agregados por encima (largo) o por debajo (corto) del precio de equilibrio cuando el promedio está activo. |
| `SignalCandleType` | Periodo de tiempo utilizado para construir el patrón de dos velas (diario por defecto). |

## Notas

* La estrategia se basa en órdenes de mercado tanto para entradas como para salidas; Las órdenes de protección del corredor de MetaTrader se emula internamente.
* `PairsToTrade` debe contener identificadores que el conector conectado pueda resolver. Los símbolos desconocidos se omiten en silencio.
* La martingala y los bloques finales operan por contexto de símbolo, por lo tanto, cada valor mantiene una canasta independiente.
* `SlippagePoints` se conserva para que esté completo, pero no afecta la ejecución en StockSharp.
