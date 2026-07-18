# Estrategia de Surf 3.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia de C# es una fiel adaptación del experto MetaTrader 4 **Surfing 3.0**. Recrea la lógica de ruptura que observa una envolvente de promedio móvil exponencial (EMA) construida a partir de máximos y mínimos de velas. Siempre que la barra anterior se cierra dentro de la banda y la última barra cerrada la atraviesa, el sistema reacciona con un comercio direccional. La traducción se basa en el alto nivel API de StockSharp, suscripciones de velas e indicadores integrados en lugar de buffers escritos a mano.

El algoritmo funciona exclusivamente con velas terminadas de una agregación configurable. Mantiene solo la cantidad mínima de estado necesaria para emular las retrospectivas `iMA` y `iClose` utilizadas por el código original. Cada decisión se toma una vez por barra cerrada, coincidiendo con el estilo de evaluación de "barra cerrada" de la implementación MQL.

## Indicadores

- **Alto EMA / Mínimo EMA**: dos promedios móviles exponenciales calculados sobre máximos y mínimos de velas. Forman una envolvente dinámica que define los niveles de ruptura para entradas largas y cortas.
- **Índice de fuerza relativa (RSI)**: actúa como filtro de tendencias. Las posiciones largas requieren que RSI esté por encima de `LongRsiThreshold`, mientras que las posiciones cortas solo se permiten cuando está por debajo de `ShortRsiThreshold`.

## Lógica de trading

1. Suscríbase a velas de tipo `CandleType` y actualice los indicadores EMA y RSI para cada barra terminada.
2. Almacene los valores de la barra cerrada anterior del precio de cierre y los máximos/mínimos de EMA. Estos representan `PriceClose_2`, `PriceHigh_2` y `PriceLow_2` del experto original.
3. Cuando la última barra cerrada (`PriceClose_1`) cruza **por encima** del máximo EMA mientras que el cierre anterior estaba por debajo o igual a él y el filtro RSI confirma:
   - Cierre cualquier posición corta abierta.
   - Abra una orden de mercado larga con volumen `OrderVolume`.
   - Calcule las compensaciones de stop loss y takeprofit en puntos del instrumento.
4. Cuando la última barra cerrada cruza **por debajo** del mínimo EMA mientras que el cierre anterior estuvo por encima o igual a él y el RSI está por debajo del umbral corto:
   - Cierre cualquier posición larga abierta.
   - Abra una orden de mercado corta con volumen `OrderVolume`.
   - Aplique los niveles de protección utilizando las mismas distancias basadas en puntos.
5. Sólo puede haber una posición neta activa. Las señales de reversión siempre aplanan la exposición existente antes de entrar en la dirección opuesta.
6. Fuera de la ventana de negociación `[TradeStartHour, TradeEndHour)`, no se inician nuevas transacciones. Una vez que el reloj llega a `TradeEndHour`, la estrategia cierra cualquier posición restante y restablece su historial interno, imitando la llamada `closeAllPos()` en la versión MQL.

## Gestión del riesgo

- **Stop Loss / Take Profit**: expresado en puntos del instrumento y convertido utilizando el paso del precio del valor. Ambos son opcionales; establecer una distancia de `0` desactiva el nivel respectivo.
- **Sesión plana**: al final de la ventana de negociación permitida, todas las posiciones abiertas se cierran en el mercado y se borra el seguimiento de stop/takeprofit. Esto evita que las posiciones se desvíen de la noche a la mañana, exactamente como lo hizo cumplir el experto original con `startHour` / `endHour`.

## Parámetros

| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `OrderVolume` | Volumen comercial utilizado para cada orden de mercado. | `1` |
| `TakeProfitPoints` | Distancia de toma de ganancias expresada en puntos del instrumento. | `80` |
| `StopLossPoints` | Distancia de stop loss expresada en puntos del instrumento. | `50` |
| `MaPeriod` | Longitud del EMA aplicada a máximos y mínimos. | `50` |
| `RsiPeriod` | Periodo del filtro RSI. | `10` |
| `LongRsiThreshold` | Valor mínimo RSI requerido para permitir entradas largas. | `40` |
| `ShortRsiThreshold` | Valor máximo RSI permitido para ingresar posiciones cortas. | `65` |
| `TradeStartHour` | Hora (hora de cambio) a partir de la cual se permiten nuevas operaciones. | `8` |
| `TradeEndHour` | Hora (exclusiva) después de la cual se cierran las posiciones y no se inician nuevas operaciones. | `18` |
| `CandleType` | Agregación de velas utilizada para todos los cálculos (predeterminado: velas de 15 minutos). | `15m` |

## Notas

- Las señales se evalúan estrictamente en velas terminadas; las fluctuaciones intrabar se ignoran como en MetaTrader.
- La estrategia restablece su historial EMA cuando finaliza la sesión de negociación para evitar mezclar datos de diferentes días.
- La traducción de Python se omite intencionalmente de acuerdo con las pautas del proyecto.
