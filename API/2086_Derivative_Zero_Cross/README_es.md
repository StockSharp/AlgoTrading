# Estrategia de Cruce Cero de Derivada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el cambio de signo de la derivada del precio. La derivada se calcula como el momentum del precio dividido por el período y multiplicado por 100. Cuando la derivada cruza la línea cero, la posición actual se cierra y se abre la posición opuesta.

## Parámetros

- `DerivativePeriod` - período de suavizado para el cálculo de la derivada.
- `PriceType` - precio fuente utilizado para la derivada.
- `BuyEntry` - permitir la apertura de posiciones largas.
- `SellEntry` - permitir la apertura de posiciones cortas.
- `BuyExit` - permitir el cierre de posiciones largas.
- `SellExit` - permitir el cierre de posiciones cortas.
- `StopLoss` - stop loss en puntos.
- `TakeProfit` - take profit en puntos.
- `CandleType` - marco temporal de velas.

## Lógica

1. Suscribirse a velas y calcular el momentum del precio seleccionado.
2. La derivada se obtiene dividiendo el momentum por el período y escalando por 100.
3. Cuando la derivada pasa de positiva a no positiva, se abre una posición larga y se cierra la corta.
4. Cuando la derivada pasa de negativa a no negativa, se abre una posición corta y se cierra la larga.
5. Se aplica protección mediante stop loss y take profit para gestionar el riesgo.
