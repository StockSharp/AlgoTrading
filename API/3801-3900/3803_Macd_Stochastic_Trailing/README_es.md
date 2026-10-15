# Macd Stochastic Estrategia final
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

# MACD Stochastic Estrategia de seguimiento

## Descripción general
- Convertido del MetaTrader 4 asesor experto `MQL/7637/3_lccfpgubwykd__www_forex-instruments_info.mq4`.
- Utiliza un flujo de trabajo de **tres períodos de tiempo**: las velas horarias impulsan ambos filtros MACD, las velas de 15 minutos suministran osciladores Stochastic y las velas de 1 minuto confirman las rupturas de precios y gestionan las salidas finales.
- Implementa una estrategia StockSharp de alto nivel utilizando `SubscribeCandles(...).Bind(...)`/`BindEx(...)` sin sondeo de datos manual.
- Las posiciones se abren con órdenes de mercado y se gestionan completamente dentro de la estrategia (no se requirieron cambios de pruebas externas).

## Indicadores y parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| ---- | ---- | ------- | ----------- |
| `LongStopLoss` | `decimal` | `17` | Distancia de parada inicial para operaciones largas, expresada en puntos del instrumento. |
| `ShortStopLoss` | `decimal` | `40` | Distancia de parada inicial para operaciones cortas (puntos). |
| `LongTrailingStop` | `decimal` | `88` | Distancia de seguimiento para posiciones largas. |
| `ShortTrailingStop` | `decimal` | `76` | Distancia de seguimiento para posiciones cortas. |
| `OrderVolume` | `decimal` | `0.1` | Volumen comercial base (lotes) reflejado desde la entrada MQL. |
| `MacdCandleType` | `DataType` | `H1` | Marco temporal para los filtros alcistas y bajistas MACD (`22/27/9` y `19/77/9`). |
| `StochasticCandleType` | `DataType` | `M15` | Marco de tiempo utilizado para los osciladores Stochastic (`5/3/11` y `9/3/19`). |
| `EntryCandleType` | `DataType` | `M1` | Marco de tiempo que proporciona confirmación de ruptura y lógica de seguimiento. |

Todas las configuraciones basadas en puntos se convierten a precios absolutos a través del instrumento `PriceStep`, reproduciendo fielmente el multiplicador MetaTrader `Point`.

## Reglas de trading
### Entrada larga
1. La línea principal horaria MACD(22,27,9) cruza por encima de su valor anterior pero permanece por debajo de cero.
2. M15 Stochastic(%K=5,%D=3,slowing=11) está por debajo de 26 y aumenta con respecto a su valor anterior.
3. El cierre actual de M1 perfora el máximo de M1 anterior.
4. Cuando todas las condiciones se alinean y no hay ninguna posición abierta, la estrategia compra `OrderVolume` más cualquier cantidad necesaria para invertir una posición corta existente.

### Entrada corta
1. La línea principal horaria MACD(19,77,9) cae por debajo de su valor anterior, con el valor anterior por encima de cero.
2. M15 Stochastic(%K=9,%D=3,desaceleración=19) está por encima de 70.
3. El cierre actual de M1 cae por debajo del mínimo anterior de M1.
4. Se abre un corto con la misma lógica de cambio de posición que el EA original.

### Salir y seguir
- Las paradas iniciales reflejan las distancias MQL `StopLoss` en puntos.
- Los trailingstops se activan una vez que el precio se mueve más que la distancia de seguimiento especificada a favor de la posición y se recalculan en cada vela M1 terminada.
- Si el precio toca el nivel de stop activo (inicial o arrastrado), la posición se cierra con una orden de mercado.

## Notas de implementación
- Las suscripciones a velas se dividen por período de tiempo, por lo que las actualizaciones de los indicadores siguen siendo independientes y coinciden exactamente con el comportamiento de múltiples períodos de tiempo de EA.
- Las comparaciones finales MQL `Bid`/`Ask` se aproximan con máximos/mínimos de velas M1 terminadas, que es la representación más cercana dentro del nivel alto basado en velas API.
- El código sigue las pautas del repositorio: sangría de tabulación, espacio de nombres `StockSharp.Samples.Strategies`, comentarios en inglés y declaraciones de parámetros dentro del constructor a través de `Param(...)`.
