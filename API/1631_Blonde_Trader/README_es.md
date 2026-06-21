# Estrategia Blonde Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Blonde Trader es una estrategia de trading en cuadrícula convertida desde MQL. Busca movimientos de precio alejándose de extremos recientes y abre posiciones con una cuadrícula de órdenes pendientes.

## Concepto

- Calcular el máximo más alto y el mínimo más bajo durante las últimas **Period X** velas.
- Si el precio actual está por debajo del máximo reciente en más de **Limit** ticks, abrir una posición larga y colocar una serie de órdenes buy limit formando una cuadrícula.
- Si el precio actual está por encima del mínimo reciente en más de **Limit** ticks, abrir una posición corta y colocar una serie de órdenes sell limit formando una cuadrícula.
- Cerrar todas las posiciones cuando el beneficio acumulado alcance **Amount**.
- Opcionalmente, cuando el precio se mueva **LockDown** ticks en beneficio, se coloca una orden stop en el nivel de punto de equilibrio para proteger la posición.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `PeriodX` | Período de retroceso para el máximo más alto y el mínimo más bajo. |
| `Limit` | Distancia mínima en ticks desde el precio actual hasta un extremo. |
| `Grid` | Paso en ticks entre las órdenes pendientes de la cuadrícula. |
| `Amount` | Objetivo de beneficio en la moneda de la cuenta. |
| `LockDown` | Distancia en ticks para mover el stop al punto de equilibrio. |
| `CandleType` | Tipo de velas utilizadas para el análisis. |

## Indicadores

- `Highest` – rastrea el máximo más alto durante el período de retroceso.
- `Lowest` – rastrea el mínimo más bajo durante el período de retroceso.

## Lógica de Órdenes

1. Cuando aparece una configuración larga:
   - Comprar a mercado con el volumen predeterminado de la estrategia.
   - Colocar cuatro órdenes buy limit adicionales por debajo de la entrada, cada una separada por **Grid** ticks y doblando el volumen.
2. Cuando aparece una configuración corta:
   - Vender a mercado con el volumen predeterminado de la estrategia.
   - Colocar cuatro órdenes sell limit adicionales por encima de la entrada con las mismas reglas de cuadrícula y duplicación de volumen.
3. Si `PnL` alcanza **Amount**, todas las posiciones abiertas y las órdenes pendientes se cierran.
4. Si `LockDown` es mayor que cero y el precio se ha movido el número especificado de ticks a favor de la posición, se coloca una orden stop protectora un tick más allá del precio de entrada.

## Notas

Esta estrategia demuestra la lógica básica de trading en cuadrícula. Utiliza únicamente características de la API de alto nivel: `SubscribeCandles`, vinculación de indicadores y ayudantes de órdenes simples como `BuyMarket`, `SellLimit` y `SellStop`.
