# Estrategia cruzada de precios MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia MA Price Cross es una conversión directa del MetaTrader 4 asesor experto "MA Price Cross" al API de alto nivel de StockSharp. Espera a que la media móvil seleccionada cruce el precio actual mientras se permite la negociación dentro de un período de tiempo configurable. Cuando el cruce se produce desde abajo, el algoritmo abre una posición larga; cuando el cruce ocurre desde arriba, abre una posición corta. Las distancias protectoras de stop-loss y take-profit se definen en MetaTrader puntos y se traducen automáticamente a compensaciones de precios absolutas utilizando el `PriceStep` del instrumento.

A diferencia de la implementación original MQL, que reacciona en cada tic, la versión StockSharp funciona con velas terminadas y utiliza la suscripción de alto nivel `SubscribeCandles`. Esto garantiza que las decisiones comerciales se ejecuten una vez por barra y sigan siendo compatibles con el proceso de vinculación del indicador. El promedio móvil se puede configurar para que coincida con los cuatro modos MetaTrader y acepta diferentes fuentes de precios (cierre, apertura, máximo, mínimo, mediana, típico, ponderado).

## Lógica comercial

1. Espere a que la hora actual caiga dentro de la ventana de negociación `[StartTime, StopTime)`. Las ventanas nocturnas se mantienen alrededor de la medianoche.
2. Procese solo velas completadas. Alimente la media móvil configurada con el precio aplicado elegido.
3. Almacene el valor de media móvil anterior para emular la lógica de cambio `iMA` utilizada en MetaTrader.
4. Cuando el promedio anterior esté por debajo del último precio y el nuevo promedio esté por encima del precio, abra (o invierta) una posición larga.
5. Cuando el promedio anterior esté por encima del último precio y el nuevo promedio esté por debajo del precio, abra (o invierta) una posición corta.
6. Antes de abrir una nueva posición en el lado opuesto, aplane cualquier exposición existente para reflejar la restricción `OrdersTotal() == 0` del código original.
7. Inicie un stop-loss y take-profit virtual con distancias expresadas en MetaTrader puntos multiplicados por el instrumento actual `PriceStep`.

## Parámetros predeterminados

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | `TimeFrame(1m)` | Serie de velas que impulsa todos los cálculos. |
| `MaPeriod` | `160` | Número de barras utilizadas por la media móvil. |
| `MaMethod` | `Simple` | Tipo de media móvil: simple, exponencial, suavizada o ponderada lineal. |
| `PriceType` | `Close` | Fuente del precio remitida a la media móvil (cierre/apertura/máximo/mínimo/mediana/típica/ponderada). |
| `StartTime` | `01:00` | Hora del día en que se activa la negociación. |
| `StopTime` | `22:00` | Hora del día en la que se detienen las nuevas entradas. |
| `StopLossPoints` | `200` | MetaTrader puntos convertidos en una distancia de parada de protección absoluta. |
| `TakeProfitPoints` | `600` | MetaTrader puntos convertidos en una distancia objetivo de beneficio absoluto. |
| `OrderVolume` | `0.1` | Volumen predeterminado presentado con órdenes de mercado. |

## Notas

- Si `StartTime` es igual a `StopTime`, el filtro de tiempo está deshabilitado y se permite operar durante todo el día.
- Cuando `StopLossPoints` o `TakeProfitPoints` es igual a cero, no se registra el nivel de protección correspondiente.
- El filtro de tiempo utiliza el tiempo de cierre de la vela (`candle.CloseTime.TimeOfDay`) para que se adapte a la zona horaria del intercambio proporcionada por MarketDataConnector.
- Si la seguridad no expone `PriceStep`, las distancias basadas en puntos se utilizan directamente sin escalar.

## Referencia de estrategia original

- Fuente: `MQL/44133/MA Price Cross.mq4`
- Autor: JBlanked (2023)
