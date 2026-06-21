# Estrategia de Canal BrakeExp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el indicador **BrakeExp**, que construye un canal exponencial alrededor de los movimientos del precio. El indicador alterna entre regímenes largo y corto y genera señales de compra o venta cuando el precio cruza los bordes dinámicos del canal.

## Cómo funciona

- El indicador mantiene una curva exponencial que sigue al precio.
- Cuando la curva está por debajo del precio (tendencia alcista), la estrategia busca señales de compra.
- Cuando la curva está por encima del precio (tendencia bajista), la estrategia busca señales de venta.
- Un cruce de un lado al otro genera una señal de entrada en la nueva dirección y cierra la posición opuesta.

## Parámetros

- `Candle Type` – período temporal de las velas procesadas.
- `Volume` – volumen de la orden para entradas de mercado.
- `A`, `B` – parámetros que definen la forma de la curva BrakeExp.
- `Buy Open` / `Sell Open` – permiso para abrir posiciones largas o cortas.
- `Buy Close` / `Sell Close` – permiso para cerrar posiciones cortas o largas.

## Notas

Esta implementación se centra en la lógica principal del indicador BrakeExp y no incluye gestión de stop-loss o take-profit. Se pueden añadir controles de riesgo adicionales si es necesario.
