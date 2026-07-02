# Adaptive Grid MT4 (puerto StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia recrea el asesor experto "Adaptive Grid Mt4" para el nivel alto de StockSharp API. Deja caer una rejilla simétrica de
comprar órdenes stop y vender órdenes stop alrededor del cierre de la vela actual. Las distancias de la cuadrícula se derivan del rango verdadero promedio (ATR) y
Por lo tanto, se adaptan a la volatilidad del mercado. Cada orden pendiente vence después de un número configurable de velas, manteniendo la
Libro de pedidos ordenado en mercados laterales.

Cuando se ejecuta una orden de entrada, la estrategia registra inmediatamente las órdenes coincidentes de toma de ganancias y de limitación de pérdidas a los precios calculados.
de la instantánea ATR que produjo la cuadrícula. Las órdenes de protección son uno a uno con la entrada completa y persisten hasta que se ejecutan.
o cancelado manualmente.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `GridLevels` | Número de órdenes stop por encima y por debajo del mercado. Equivalente a la entrada `nGrid` del EA. |
| `TimerBars` | Número de velas terminadas tras las cuales se cancela cualquier entrada pendiente (MT4 `nBars`). |
| `PriceOffsetMultiplier` | multiplicador ATR aplicado a la compensación inicial del precio actual (`Poffset`). |
| `GridStepMultiplier` | multiplicador ATR utilizado para el espacio entre niveles de cuadrícula consecutivos (`Pstep`). |
| `StopLossMultiplier` | multiplicador ATR que define la distancia del stop-loss adjunto a cada orden (`StopLoss`). |
| `TakeProfitMultiplier` | multiplicador ATR que define la distancia de la toma de ganancias (`TakeProfit`). |
| `AtrPeriod` | periodo ATR promedio. Refleja el valor codificado de 14 del script. |
| `OrderVolume` | Volumen utilizado para todas las órdenes pendientes (MT4 `Lot`). |
| `CandleType` | Periodo de tiempo que impulsa el recálculo de la cuadrícula (`Wtf`). |

## Lógica de trading

1. Suscríbete a las velas del `CandleType` configurado y alimenta un ATR(14).
2. En cada vela terminada:
   - Avance la barra de la barra interna y cancele los pedidos de cuadrícula pendientes que excedieron `TimerBars`.
   - Omita el procesamiento adicional si el ATR no está formado, alguna orden de cuadrícula aún está activa o la estrategia ya mantiene una posición.
   - Calcule el desplazamiento de ruptura, el espaciado de la cuadrícula, las distancias de parada de pérdidas y toma de ganancias como valores `ATR * multiplier`.
   - Coloque `GridLevels` pares de órdenes stop de compra y de venta alrededor del cierre de la vela, normalizando los precios con
`Security.ShrinkPrice` para respetar el tamaño del tick del instrumento.
3. Cuando se complete una entrada, elimínela de la lista de cuadrícula rastreada y genere las órdenes de protección correspondientes:
   - Las entradas largas reciben un límite de pérdidas `SellStop` y una toma de ganancias `SellLimit`.
   - Las entradas cortas reciben un límite de pérdidas `BuyStop` y una toma de ganancias `BuyLimit`.
4. Las órdenes de protección se monitorean a través de `OnOrderChanged` para que las entradas completadas o canceladas se eliminen del seguimiento.
listas.

## Notas

- La cuadrícula solo se reconstruye cuando no hay posiciones abiertas y todas las órdenes de la cuadrícula existentes expiraron, coincidiendo con la lógica `What()` de
el EA original.
- Los precios se calculan a partir del cierre de la vela en lugar del tick sin procesar `Bid/Ask`. Esto mantiene la implementación impulsada por velas.
mientras produce el mismo diseño simétrico en todo el mercado.
- La instantánea ATR utilizada para la cuadrícula también se utiliza para que las órdenes de protección imiten la parada por boleto y la toma de ganancias de MetaTrader
valores.
- Aún no existe ninguna traducción de Python que coincida con la solicitud.
