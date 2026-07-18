# Estrategia de cuadrícula de bandas elásticas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del asesor experto MetaTrader 4 **RUBBERBANDS_2.mq4**.
- Ejecuta una cuadrícula simétrica alrededor del precio actual utilizando las mejores cotizaciones de oferta/demanda en lugar de velas.
- Mantiene libros de contabilidad separados para exposición larga y corta para que el comportamiento coincida con la implementación de MT4 con cobertura.
- Implementa controles de pérdidas y ganancias a nivel de sesión y un modo de reposo/parada manual idéntico a las entradas originales.

## Lógica de trading
1. La estrategia se suscribe a `SubscribeLevel1()` y reacciona a cada cambio de la mejor oferta y la mejor demanda.
2. Dos extremos flotantes (`_upperExtreme` / `_lowerExtreme`) capturan el precio de venta más alto y más bajo alcanzado desde el último reinicio. Se inicializan a partir de parámetros cuando `UseInitialValues` es verdadero; de lo contrario, se utiliza el primer precio de venta recibido.
3. Cuando no hay operaciones abiertas y el tiempo del servidor llega al primer tic de un minuto (el segundo es igual a cero), la estrategia solicita tanto una compra de mercado como una venta de mercado. Esto refleja el comportamiento de MT4, donde las banderas de compra/venta se activan cada minuto mientras el libro está vacío.
4. Cada vez que el precio de venta avanza `GridStepPoints` puntos por encima del máximo almacenado, se emite una nueva orden de venta. Cada caída de la misma distancia por debajo del mínimo almacenado desencadena una nueva orden de compra. Los extremos se actualizan a la demanda actual después de cada activación, de modo que la escalera se "estire" con el precio.
5. El número total de operaciones abiertas simultáneamente (suma de tramos largos y cortos) está limitado por `MaxTrades`.
6. El beneficio flotante se calcula a partir de la oferta/demanda actual: los beneficios a largo plazo utilizan la oferta menos el precio medio a largo plazo, los beneficios a corto plazo utilizan el precio medio a corto menos el precio de venta. El asistente `PriceToMoney` convierte las diferencias de precios en la moneda de la cuenta usando `PriceStep`/`StepPrice` cuando esté disponible.
7. Cuando el beneficio flotante alcanza `SessionTakeProfitPerLot * OrderVolume` y `UseSessionTakeProfit` está habilitado, toda la exposición se estabiliza. Del mismo modo, la pérdida flotante por debajo de `-SessionStopLossPerLot * OrderVolume` desencadena una salida completa cuando `UseSessionStopLoss` está habilitado.
8. Los indicadores manuales reproducen las opciones originales de EA: `CloseNow` impone un inicio plano, `QuiesceMode` mantiene la estrategia inactiva mientras está plana y `StopNow` detiene nuevas entradas sin interferir con las posiciones existentes.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Volumen para cada orden de mercado (MT4 `Lots`). |
| `MaxTrades` | Recuento máximo de operaciones abiertas simultáneamente (MT4 `maxcount`). |
| `GridStepPoints` | Distancia en puntos de precio entre capas de la cuadrícula (MT4 `pipstep`). |
| `QuiesceMode` | Si está habilitada, la estrategia espera una vez, idéntica a `quiescenow`. |
| `TriggerImmediateEntries` | Abre una compra y venta inicial tan pronto como la estrategia esté lista (`donow`). |
| `StopNow` | Detiene las entradas automáticas mientras mantiene vivas las posiciones actuales (`stopnow`). |
| `CloseNow` | Solicita un aplanamiento inmediato al inicio (`closenow`). |
| `UseSessionTakeProfit` & `SessionTakeProfitPerLot` | Objetivo de beneficio flotante a nivel de sesión por lote. |
| `UseSessionStopLoss` & `SessionStopLossPerLot` | Umbral de pérdida flotante a nivel de sesión por lote. |
| `UseInitialValues`, `InitialMax`, `InitialMin` | Soporte de reinicio opcional que reutiliza los extremos anteriores (`useinvalues`, `inmax`, `inmin`). |

## Notas de implementación
- Todo el estado interno tiene tabulaciones y se almacena en campos en lugar de colecciones para seguir las pautas del proyecto.
- Las órdenes de mercado se limitan mediante el seguimiento de `_activeBuyOrder` y `_activeSellOrder`, por lo que no se envían solicitudes duplicadas mientras la anterior está pendiente.
- La contabilidad de cobertura se realiza en `OnOwnTradeReceived`, donde los precios/volúmenes promedio a corto y largo plazo se actualizan de forma independiente y se convierten en ganancias flotantes para la lógica de parada.
- `TryCloseAll()` refleja la rutina MT4 `close1by1()` al enviar órdenes de mercado opuestas hasta que ambos libros contables estén planos y luego restablecer los extremos a la última solicitud.
- La estrategia se basa exclusivamente en llamadas API de alto nivel (`SubscribeLevel1()`, `BuyMarket`, `SellMarket`) y evita el acceso directo a los indicadores como lo exigen las reglas del repositorio.
