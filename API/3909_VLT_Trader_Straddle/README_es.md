# Estrategia mixta de comerciante VLT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de VLT Trader es una StockSharp conversión del MetaTrader 4 asesor experto "VLT_TRADER". La idea original busca un período de volatilidad extremadamente baja y luego prepara una ruptura alrededor de la vela más reciente. Cuando la última vela completada tiene el rango más pequeño en comparación con un número configurable de velas anteriores, la estrategia coloca órdenes de parada por encima y por debajo de esa vela en anticipación de una expansión de la volatilidad.

## Lógica comercial
- Suscríbase a la serie de velas configuradas y calcule el rango (máximo menos mínimo) para cada barra.
- Realice un seguimiento del rango mínimo entre las barras `LookbackCandles` anteriores utilizando el indicador `Lowest`.
- Una vez que la vela finalizada más reciente tenga un rango menor que este mínimo histórico, prepare las órdenes de ruptura para la siguiente sesión.
- Coloque un stop de compra por encima del máximo anterior más `EntryOffsetPoints` y un stop de venta por debajo del mínimo anterior menos el mismo desplazamiento.
- Adjunte paradas y objetivos de distancia fija a cada orden pendiente (`StopLossPoints` y `TakeProfitPoints`).
- Deje activas ambas órdenes pendientes. Cualquiera que sea el lado que se active primero se convierte en una posición de mercado, mientras que el stop opuesto permanece en el libro y puede activarse más tarde si el mercado se revierte.
- Cuando se completa o cancela una orden pendiente, la referencia correspondiente se borra para que se puedan crear nuevas opciones después de que se cierren todas las posiciones y órdenes.

## Gestión de riesgos
- El tamaño de la operación se controla a través de `OrderVolume` y se redondea al paso y los límites de volumen del instrumento.
- Las distancias de parada de pérdidas y toma de ganancias se expresan en pasos de precio (puntos) y se convierten a precios reales utilizando el `PriceStep` del instrumento.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Tamaño de lote utilizado al crear las órdenes pendientes. |
| `EntryOffsetPoints` | Se agregaron puntos adicionales al máximo/mínimo anterior al realizar entradas de stop. |
| `TakeProfitPoints` | Tomar distancia de ganancia adjunta a cada orden. |
| `StopLossPoints` | Distancia de stop loss adjunta a cada orden. |
| `LookbackCandles` | Número de velas anteriores utilizadas para medir el rango histórico mínimo. |
| `CandleType` | Cronograma de la serie de velas que alimenta la estrategia. |

## Notas
- La estrategia requiere un `PriceStep` válido en el instrumento; de lo contrario no se realizan pedidos.
- Debido a que los niveles de parada y toma de ganancias se transmiten junto con las órdenes pendientes, los precios de ejecución en StockSharp pueden diferir ligeramente de MetaTrader dependiendo de las reglas de ejecución del corredor.
- La implementación se basa exclusivamente en API de alto nivel (`SubscribeCandles` + `Bind`) y el indicador estándar `Lowest` para reflejar la verificación de volatilidad del EA original.
