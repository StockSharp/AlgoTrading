# Estrategia de Crypto Scalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Crypto Scalper reproduce la lógica del experto de MetaTrader original con componentes de alto nivel de StockSharp. Vigila un cruce alcista o bajista de una media móvil ponderada lineal rápida en el marco temporal principal y confirma la configuración con filtros de tendencia calculados en un marco temporal superior. Una vez que las condiciones se alinean, la estrategia entra usando órdenes de mercado y gestiona las salidas a través de distancias de stop-loss y take-profit medidas en pips de MetaTrader.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Primary Candle` | Tipo de vela procesado en el marco temporal principal. | Marco temporal de 1 minuto |
| `Higher Candle` | Tipo de vela de marco temporal superior usado para confirmación. | Marco temporal de 15 minutos |
| `Fast LWMA` | Longitud de la media móvil ponderada lineal principal. | 8 |
| `Higher Fast MA` | Longitud de la LWMA rápida en el marco temporal de confirmación. | 6 |
| `Higher Slow MA` | Longitud de la LWMA lenta en el marco temporal de confirmación. | 85 |
| `Momentum Period` | Longitud del indicador de Momentum aplicado a las velas del marco temporal superior. | 14 |
| `Momentum Threshold` | Desviación mínima del momentum de referencia (línea de base MetaTrader 100) requerida para operar. | 0.3 |
| `Momentum Reference` | Nivel de referencia usado para emular el escalado de momentum de MetaTrader. | 100 |
| `Stop Loss (pips)` | Distancia de stop de protección en pips de MetaTrader. | 20 |
| `Take Profit (pips)` | Distancia de ganancia de protección en pips de MetaTrader. | 50 |
| `Volume` | Volumen de orden expresado en lotes. | 0.01 |
| `MACD Fast` | Período de la EMA rápida para la confirmación MACD. | 12 |
| `MACD Slow` | Período de la EMA lenta para la confirmación MACD. | 26 |
| `MACD Signal` | Período de la EMA de señal para la confirmación MACD. | 9 |

## Lógica de trading
1. Suscribirse al marco temporal principal y calcular una LWMA que reacciona rápidamente al precio.
2. Detectar una entrada cuando la vela anterior cruza la LWMA hacia arriba (largo) o hacia abajo (corto).
3. Confirmar el cruce usando los filtros del marco temporal superior:
   - La LWMA rápida superior debe mantenerse por encima de la LWMA lenta superior para entradas largas y por debajo para entradas cortas.
   - El histograma MACD (principal menos señal) debe ser positivo para largos y negativo para cortos.
   - El momentum debe desviarse del nivel de referencia al menos por `Momentum Threshold`.
4. Enviar una orden de mercado en la dirección detectada cuando no hay otras órdenes activas y la posición actual lo permite.
5. Monitorear las velas siguientes y cerrar la posición cuando se toca el precio de stop-loss o take-profit.

## Notas
- La estrategia usa suscripciones de alto nivel de StockSharp con `Bind`, evitando búferes de indicadores manuales.
- Los niveles de protección se recalculan en cada vela usando el paso de precio del valor. Se aplica un paso de reserva de `0.0001` si el instrumento no expone un paso de precio configurado.
- Solo se permite una posición a la vez. Las señales posteriores se ignoran hasta que la operación existente finalice.
- Todos los comentarios en línea dentro de la implementación C# están escritos en inglés según las directrices del repositorio.
