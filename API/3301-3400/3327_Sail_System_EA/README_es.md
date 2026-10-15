# Estrategia Sail System EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Sail System EA es un scalper con cobertura que mantiene exposición larga/corta simétrica mientras comprueba continuamente requisitos del broker como spread máximo, nivel mínimo de stop y límites de sesión. El port StockSharp recrea el comportamiento original con la API `Strategy` de alto nivel: el motor se suscribe a cotizaciones level-1, abre o rearma ambos lados de la cobertura y gestiona niveles virtuales de stop-loss/take-profit sin llamadas de conector de bajo nivel.

La implementación mantiene dos objetos internos `PositionState` (largo y corto). Para cada lado la estrategia sigue precio de entrada, volumen restante, niveles virtuales de protección y órdenes pendientes. Esto refleja el experto MQL que mantenía contadores de tickets separados para órdenes de mercado y pendientes.

## Lógica de negociación
1. **Filtro de sesión.** El trading puede restringirse a una ventana configurable. Cuando la hora actual cae fuera de la sesión, la estrategia mantiene, cancela o cierra exposición existente según `ManageExistingOrders`.
2. **Vigilancia de spread.** Las actualizaciones bid/ask se recopilan mediante `SubscribeLevel1()`. La estrategia comprueba el spread instantáneo o una media móvil (hasta 100 muestras) y compara el valor con `MaxSpread` más la comisión configurada. Si el spread es demasiado amplio, el sistema puede cerrar posiciones abiertas y la distancia de entrada puede multiplicarse por `MultiplierIncrease` para esperar condiciones más tranquilas.
3. **Motor de entrada.** Cuando se permite operar, la estrategia abre ambos lados con órdenes de mercado o mantiene órdenes limit emparejadas, según `UsePendingOrders`. El precio limit de nuevas órdenes se deriva del mejor bid/ask actual más `DistancePending` (en pips) y un multiplicador de seguridad opcional.
4. **Protección virtual.** Cada ejecución establece niveles virtuales de stop-loss y take-profit opcional usando `OrdersStopLoss` / `OrdersTakeProfit`. Los niveles se recalculan después de `DelayModifyOrders` actualizaciones de cotización, pero solo cuando la mejora supera `StepModifyOrders`. El mecanismo reproduce los ajustes graduales de stop de MQL sin llamar a `OrderModify`.
5. **Gestión de salida.** Cuando el bid (para largos) o ask (para cortos) alcanza el stop virtual o objetivo, la estrategia envía la orden de mercado opuesta para cerrar la posición. Las salidas se etiquetan por motivo (stop loss, take-profit, fin de sesión o violación de spread) para que el log resultante coincida con el asesor experto.
6. **Gestión de reentrada.** Si las órdenes pendientes se alejan del mercado más de `PipsReplaceOrders` multiplicado por `SafeMultiplier`, se cancelan y recrean con precios nuevos. Esto reemplaza la lógica de reubicación por temporizador del script MQL.
7. **Tamaño de lote.** Se usa `ManualLotSize` fijo o el volumen se deriva del patrimonio de cartera y `RiskFactor`, imitando el cálculo de auto-lote del código original.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` / `ManualLotSize` | Volumen base por orden cuando el tamaño automático está desactivado. |
| `AutoLotSize`, `RiskFactor` | Activa el tamaño de lote basado en patrimonio. |
| `UseVirtualLevels` | Mantiene la lógica de stop-loss/take-profit en el lado de la estrategia. |
| `OrdersStopLoss`, `OrdersTakeProfit`, `PutTakeProfit` | Distancias de protección en pips. |
| `DelayModifyOrders`, `StepModifyOrders` | Controlan la rapidez con que se refrescan los niveles virtuales. |
| `PipsReplaceOrders`, `SafeMultiplier` | Fuerzan reentrada cuando órdenes pendientes están demasiado lejos del mercado. |
| `UsePendingOrders`, `DistancePending` | Cambia entre entradas limit y de mercado. |
| `UseTimeFilter`, `TimeStartTrade`, `TimeStopTrade`, `ManageExistingOrders` | Configuración de ventana de trading. |
| `MaxSpread`, `TypeOfSpreadUse`, `HighSpreadAction`, `MultiplierIncrease`, `CloseOnHighSpread` | Filtro de spread y reacción. |
| `CommissionInPip`, `CountAvgSpread`, `TimesForAverage` | Controles de promediado de spread. |
| `AcceptStopLevel`, `Slippage`, `OrdersId` | Nivel mínimo de stop del broker, slippage de ejecución y equivalente de magic number. |

Todos los parámetros se exponen mediante `StrategyParam<T>`, por lo que están disponibles en la UI del Designer y son compatibles con ejecuciones de optimización.

## Diferencias frente a MQL
- StockSharp usa un modelo de posición neta; por ello la estrategia cancela la orden pendiente opuesta cuando se ejecuta un lado para evitar aplanar la posición neta. Esto conserva el comportamiento de cobertura alternante del EA original.
- La bandera `UseVirtualLevels` mantiene la gestión de stop-loss/objetivo dentro de la estrategia. El experto MQL usaba objetos de gráfico para visualización; este port registra cada actualización en lugar de dibujar líneas.
- El promediado de spread se implementa como media incremental, reemplazando el acumulador basado en arrays de MQL mientras respeta el mismo límite de periodo de promedio.

## Uso de la API de alto nivel
- `SubscribeLevel1().Bind(ProcessLevel1)` impulsa todo el motor de decisión a partir de actualizaciones del mejor bid/ask.
- Las órdenes de entrada y salida se crean mediante helpers de estilo `RegisterOrder`, `BuyMarket`, `SellMarket`, tal como recomiendan las directrices de conversión.
- `StartProtection()` se invoca una vez durante `OnStarted`, siguiendo la práctica recomendada del framework para activar soporte de órdenes protectoras.
