# Robot ADX + 2 Estrategia MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Robot ADX + 2 MA es una adaptación StockSharp del MetaTrader experto `Robot_ADX+2MA`. El sistema combina una velocidad rápida y una lenta.
media móvil exponencial con los componentes +DI/-DI del índice direccional medio (ADX). Las órdenes sólo se abren cuando el
La vela anterior muestra una separación EMA suficientemente amplia y la vela actual confirma el impulso a través del índice direccional. el
La conversión mantiene el comportamiento original de abrir como máximo una posición de mercado a la vez y delegar las salidas a stop-loss y
protecciones para la toma de ganancias.

## Lógica comercial
1. Suscríbase a la serie de velas principal configurada a través de `CandleType` y procese solo velas terminadas.
2. Alimente dos promedios móviles exponenciales (períodos 5 y 12) con los precios de cierre de las velas. Sus valores de la vela anterior.
emular el lookback `shift = 1` usado en MetaTrader.
3. Alimente un indicador `AverageDirectionalIndex` (período 6) con las mismas velas. Almacena el +DI/-DI actual y el anterior.
lecturas para replicar los filtros EA.
4. Calcule la distancia absoluta EMA de la vela anterior y compárela con `DifferenceThreshold` convertida de puntos a
unidades de precio (`Point` en MetaTrader es igual a `Security.PriceStep` en StockSharp).
5. **Entrada alcista**: permitida sólo si no hay ninguna posición abierta y se cumplen las siguientes condiciones:
   - El rápido anterior EMA está por debajo del lento anterior EMA.
   - El +DI anterior está por debajo de 5, el +DI actual está por encima de 10 y +DI es más fuerte que -DI.
   - La distancia EMA está por encima del umbral configurado.
6. **Entrada bajista**: simétrica a las reglas largas, requiere que el EMA rápido anterior esté por encima del EMA lento, los filtros -DI deben ser
satisfecho, y -DI para dominar +DI.
7. Cuando se abre una operación, confíe en el módulo de riesgo iniciado por `StartProtection` para salir mediante toma de ganancias o parada de pérdidas. Sin manual
Se agregan reglas de salida, que coinciden con el experto original.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | plazo de 1 minuto | Serie de velas primarias procesadas por la estrategia. |
| `TakeProfitPoints` | `int` | `4700` | Distancia del objetivo de obtención de beneficios expresada en pasos de precio. Establezca en cero para desactivar. |
| `StopLossPoints` | `int` | `2400` | Distancia del objetivo de stop-loss en pasos de precio. Establezca en cero para desactivar. |
| `TradeVolume` | `decimal` | `0.1` | Volumen neto utilizado para cada orden de mercado. |
| `DifferenceThreshold` | `int` | `10` | Se requiere una distancia mínima de EMA (en pasos de precio) antes de que se acepte una señal. |

## Gestión de riesgos
- La versión StockSharp llama a `StartProtection` con `UnitTypes.Step`, por lo que las distancias de stop-loss y take-profit configuradas son
convertido al paso de precio del corredor automáticamente.
- Las órdenes de protección se generan como salidas del mercado (`useMarketOrders = true`), replicando el comportamiento de cierre inmediato del
MQL función auxiliar.

## Detalles de implementación
- Los enlaces de indicadores utilizan el `SubscribeCandles(...).Bind(...).BindEx(...)` API de alto nivel, por lo que no se requieren bucles de datos manuales.
- Los valores EMA de la vela anterior se almacenan en caché para reproducir las llamadas `iMA(..., shift = 1)` en el EA original.
- Los datos ADX se consumen a través de `AverageDirectionalIndexValue`, lo que brinda acceso directo a los componentes +DI y -DI sin necesidad de llamar
`GetValue` ayudantes prohibidos.
- Una protección por vela (`_lastProcessedTime`) garantiza que las señales se evalúen solo una vez, aunque se activen los enlaces EMA y ADX
devoluciones de llamada para la misma vela.

## Diferencias con el experto MetaTrader
- Se elimina la llamada directa redundante `OrderSend` presente en la rama de venta del código MQL; ambas direcciones utilizan un solo
`BuyMarket`/`SellMarket` ayudante.
- MetaTrader comprueba el margen libre antes de enviar pedidos. El puerto StockSharp delega controles de riesgo al entorno de alojamiento y
supone un equilibrio suficiente.
- La lógica de protección se implementa a través del administrador de riesgos de StockSharp en lugar de bucles personalizados que llaman repetidamente a `OrderSend`.

## Consejos de uso
- Ajuste `TradeVolume` para respetar el paso del lote del valor seleccionado antes de comenzar a operar en vivo.
- Si el mercado utiliza una escala de precios diferente, modifique `DifferenceThreshold` junto con las distancias de parada/objetivo para que el EMA
La separación es comparable a la configuración MetaTrader.
- El período de tiempo predeterminado es un minuto, pero el parámetro `CandleType` permite cambiar a cualquier otra serie respaldada por los datos.
fuente.

## Indicadores
- `ExponentialMovingAverage(5)` calculado sobre precios de cierre.
- `ExponentialMovingAverage(12)` calculado sobre precios de cierre.
- `AverageDirectionalIndex(6)` proporciona filtros de fuerza +DI/-DI y ADX.
