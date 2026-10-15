# Estrategia universal de inversores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación directa del asesor experto **Universal Investor** MetaTrader 4. Combina una media móvil exponencial (EMA) y una media móvil ponderada lineal (LWMA) para confirmar la dirección de la tendencia a corto plazo y realiza operaciones de una posición con un tamaño de posición adaptable.

## Lógica comercial

1. Suscríbase al `CandleType` configurado y calcule tanto EMA como LWMA con el período definido por `MovingPeriod`.
2. Almacene los dos valores más recientes de cada promedio móvil para que la lógica imite las llamadas `iMA(..., shift = 1/2)` del EA original.
3. Genere una señal de **compra** cuando el LWMA anterior esté por encima del EMA anterior, ambos promedios estuvieran subiendo y no haya una señal opuesta en la misma vela.
4. Genere una señal de **venta** cuando el LWMA anterior esté por debajo del EMA anterior, ambos promedios estuvieran cayendo y no haya una señal opuesta en la misma vela.
5. Cierre una posición larga abierta tan pronto como la LWMA caiga por debajo del EMA (lógica espejo para cortos).
6. Calcule el volumen de operaciones a partir del parámetro de la estrategia `Volume`, increméntelo para satisfacer el requisito `MaximumRisk` cuando el valor de la cartera sea lo suficientemente grande y redúzcalo después de operaciones perdedoras consecutivas de acuerdo con `DecreaseFactor`.
7. Envíe órdenes de mercado con `BuyMarket`/`SellMarket` y realice un seguimiento del precio de entrada para detectar salidas ganadoras o perdedoras.

La estrategia mantiene solo una posición abierta a la vez e inmediatamente se revierte solo después de un cierre completo, reproduciendo el comportamiento del script original MetaTrader.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `CandleType` | Serie de velas utilizadas para los cálculos. |
| `MovingPeriod` | Período tanto para EMA como para LWMA. |
| `MaximumRisk` | Fracción del capital (0,05 = 5%) utilizada para calcular el volumen mínimo de la posición. |
| `DecreaseFactor` | Reduce el volumen después de operaciones perdedoras consecutivas (0 desactiva la función). |
| `Volume` | Volumen de contrato base transferido a `BuyMarket`/`SellMarket`. |

## Indicadores

- `ExponentialMovingAverage`
- `LinearWeightedMovingAverage`

## Notas

- Los pedidos se realizan únicamente en velas cerradas, que coinciden con el EA que depende de los cheques `Time[0]`.
- La lógica del tamaño de la posición refleja la función MetaTrader `LotsOptimized`, incluido el componente basado en el riesgo y el multiplicador de la racha de pérdidas.
