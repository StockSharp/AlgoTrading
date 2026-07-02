# Estrategia de ruptura SMC Hilo MaxMin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el comportamiento del experto MetaTrader *SMC MaxMin en 1200*. A la hora terminal especificada coloca un
orden de parada de compra por encima del máximo de la vela anterior y una orden de parada de venta por debajo del mínimo de la vela anterior. Se rellenan los pedidos pendientes
por la distancia mínima de parada del corredor, convertida de pips a unidades de precio del instrumento. Una vez que se produce una ruptura, el orden opuesto
se cancela y la posición abierta se gestiona a través de paradas fijas, objetivos de ganancias y un trailing stop opcional.

Diferencias clave con respecto al código MQL4 original:

- Las primitivas de orden StockSharp (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) reemplazan las llamadas directas `OrderSend`.
- Las entradas mínimas de distancia de parada, parada de pérdidas y toma de ganancias se expresan en pips y se convierten mediante `Security.PriceStep` a
respete el tamaño real del tick del instrumento.
- La gestión del trailing stop mueve la orden de stop sólo cuando se alcanza una distancia rentable mayor que el trailing buffer.
- Toda la lógica está impulsada por la suscripción de vela de alto nivel API, por lo que no se utilizan escaneos directos del historial ni buffers de indicadores manuales.

## Reglas de trading
1. **Hora de configuración**: cuando la hora terminal sea `SetHour`, utilice la vela completada anteriormente como referencia.
2. **Entrada larga**: coloque una parada de compra en `previous_high + min_stop_distance + price_step`.
3. **Entrada corta**: coloque un límite de venta en `previous_low - min_stop_distance - price_step`.
4. **Exclusividad mutua**: si se completa cualquiera de las paradas, la orden pendiente opuesta se cancela inmediatamente.
5. **Stop-loss**: el stop largo es `previous_low - StopLossPips`, el stop corto es `previous_high + StopLossPips` (ambos convertidos
a unidades de precio).
6. **Take-profit**: las posiciones largas utilizan un límite de venta en `entry + TakeProfitPips`; Las posiciones cortas utilizan un límite de compra en
`entry - TakeProfitPips`.
7. **Trailing stop**: cuando una posición tiene ganancias por más de `TrailingStopPips`, el stop se arrastra para mantener el mismo pip.
distancia desde la oferta/demanda actual.
8. **Tiempo de espera del pedido**: dos horas después de la configuración (`SetHour + 2`), se cancelan todas las paradas pendientes no completadas.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `Volume` | Volumen de orden utilizado para ambas órdenes de entrada. | `0.1` |
| `SetHour` | Hora terminal (0–23) cuando se crea la ruptura a horcajadas. | `15` |
| `TakeProfitPips` | Distancia objetivo de ganancias en pips. Establezca en `0` para deshabilitar las órdenes de obtención de ganancias. | `500` |
| `StopLossPips` | Distancia de parada de protección en pips. Establezca en `0` para desactivar la parada inicial. | `30` |
| `TrailingStopPips` | Distancia del trailing stop en pips. Establezca en `0` para mantener una parada estática. | `30` |
| `MinStopDistancePips` | Distancia de parada mínima del corredor utilizada para rellenar los precios de entrada. | `0` |
| `CandleType` | El tipo de vela que define la sesión por horas, por defecto es un período de tiempo de 1 hora. | `1h` |

## Notas de uso
- La estrategia requiere datos de nivel 1 para gestionar los trailingstops y mantener los precios de oferta y demanda más recientes para los cálculos de distancia.
- Si el instrumento subyacente tiene tamaños de tick no estándar (por ejemplo, el JPY se cruza con 0,01 pips), ajuste `TakeProfitPips`,
`StopLossPips` y `TrailingStopPips` en consecuencia.
- Cuando `TakeProfitPips` o `StopLossPips` es cero, las órdenes respectivas no se envían, pero las paradas dinámicas aún pueden activarse si
el parámetro final es positivo.
- Asegúrese de que el `SetHour` configurado coincida con la hora del servidor del agente de la fuente de datos entrante.
