# Estrategia de escaleras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de escaleras** reproduce el comportamiento del experto MetaTrader original. Comienza colocando órdenes stop simétricas alrededor del precio de venta actual y luego reconstruye continuamente la cuadrícula alrededor del llenado más reciente. Las ganancias se acumulan en incrementos de precio (pips) sin ponderación por volumen, exactamente como en el script original. Cuando se alcanza un objetivo de ganancias, la estrategia liquida todas las posiciones por orden de mercado, elimina las paradas pendientes y restablece la red.

## Lógica comercial

1. Cuando no haya posiciones abiertas, coloque un stop de compra y un stop de venta a una distancia de `ChannelSteps / 2` pasos de precio por encima y por debajo del precio de venta actual.
2. Después de que se complete la primera orden de parada, vuelva a armar la cuadrícula alrededor del precio ejecutado:
   - Si hay menos de dos órdenes stop activas, cancele las obsoletas.
   - Siempre que el precio de oferta actual se mantenga dentro de la mitad de la distancia del canal desde la última entrada, coloque un nuevo stop de compra y de venta a `ChannelSteps` del llenado más reciente.
   - Cuando `AddLots` esté habilitado, aumente el volumen de pedidos pendientes en el lote base después de cada ejecución.
3. Mantenga dos listas actualizadas con todas las entradas largas y cortas para reproducir la cesta cubierta utilizada por la versión MT4.
4. Calcule el beneficio no realizado de la cesta en cada vela terminada utilizando la mejor oferta para posiciones largas y la mejor demanda para posiciones cortas. Las distancias se normalizan según el paso del precio del instrumento, reflejando el cálculo de puntos original.
5. Activar una liquidación total cuando se supere cualquiera de los umbrales:
   - `ProfitSteps` – beneficio producido únicamente por el símbolo actual.
   - `CommonProfitSteps` – beneficio en toda la cesta.
6. La liquidación envía órdenes de mercado para cerrar cada exposición larga y corta por separado. Las órdenes stop pendientes se cancelan una vez que la cesta está plana.

> **Nota**: El experto original adjuntó niveles de stop-loss al registrar órdenes pendientes. StockSharp no admite niveles de protección por orden a través del nivel alto API, por lo tanto, el puerto cierra operaciones exclusivamente a través de la lógica basada en ganancias descrita anteriormente.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `ChannelSteps` | Distancia (en pasos de precio mínimo) entre las órdenes stop simétricas. | `1000` |
| `ProfitSteps` | Umbral de ganancia (en pasos) requerido para cerrar la canasta local. | `1500` |
| `CommonProfitSteps` | Umbral de beneficio global (en pasos) que obliga a una liquidación total. | `1000` |
| `AddLots` | Cuando esté habilitado, aumente el siguiente volumen de pedido pendiente en el lote base después de cada ejecución. | `true` |
| `BaseVolume` | Volumen utilizado para el primer par de órdenes stop. | `0.1m` |
| `CandleType` | Plazo utilizado para las suscripciones de velas y la gestión comercial. | `1 minute` |

## Notas de implementación

- Utiliza la API de alto nivel de StockSharp con `SubscribeCandles()` y `Bind()` para procesar velas terminadas únicamente.
- Realiza un seguimiento de las entradas individuales dentro de `OnOwnTradeReceived` para que el cálculo de ganancias pueda imitar la lógica de cobertura de la versión MQL.
- Los umbrales de ganancias operan en distancias puras de precio-escalón, sin multiplicarse por el volumen ejecutado, coincidiendo con la forma en que el experto de MT4 sumó los pips.
- Todas las órdenes stop se crean a través de `BuyStop` y `SellStop`, mientras que las salidas se ejecutan con órdenes de mercado para mantener la lógica portátil entre los proveedores de datos.
