# Estrategia de 10 Pips
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de cobertura abre posiciones largas y cortas al mismo tiempo. Cada posición utiliza niveles fijos de take-profit y stop-loss medidos en unidades de precio y puede protegerse con un trailing stop. Cuando un lado se cierra, la estrategia abre inmediatamente una nueva posición en la misma dirección para mantener ambos lados activos.

## Parámetros
- `TakeProfitBuy` – distancia de take-profit para posiciones largas.
- `StopLossBuy` – distancia de stop-loss para posiciones largas.
- `TrailingStopBuy` – distancia de trailing stop para posiciones largas.
- `TakeProfitSell` – distancia de take-profit para posiciones cortas.
- `StopLossSell` – distancia de stop-loss para posiciones cortas.
- `TrailingStopSell` – distancia de trailing stop para posiciones cortas.
- `Volume` – tamaño de la orden usado para todas las operaciones.

## Notas
- Las posiciones se abren con órdenes de mercado.
- Las órdenes de protección se registran para cada lado por separado.
- Los trailing stops se actualizan cuando el mercado se mueve en una dirección favorable.
