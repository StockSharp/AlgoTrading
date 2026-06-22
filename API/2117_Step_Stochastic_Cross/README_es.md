# Estrategia Step Stochastic Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia utiliza el indicador Step Stochastic (un oscilador personalizado basado en ATR) para generar señales de reversión. Se suscribe a un marco temporal de velas seleccionado por el usuario y calcula líneas rápida y lenta del Step Stochastic escaladas de 0 a 100.

## Reglas de entrada y salida
- **Entrada larga:** La línea lenta está por encima de 50 y la línea rápida cruza de arriba hacia abajo la línea lenta.
- **Entrada corta:** La línea lenta está por debajo de 50 y la línea rápida cruza de abajo hacia arriba la línea lenta.
- **Salida larga:** La línea lenta está por debajo de 50 y se permite el cierre de posiciones largas.
- **Salida corta:** La línea lenta está por encima de 50 y se permite el cierre de posiciones cortas.

## Parámetros
- `KFast` – multiplicador para el canal rápido.
- `KSlow` – multiplicador para el canal lento.
- `CandleType` – marco temporal de las velas.
- `AllowBuyOpen`, `AllowSellOpen`, `AllowBuyClose`, `AllowSellClose` – permisos para acciones de trading.
- `StopLoss`, `TakeProfit` – niveles de protección opcionales en unidades de precio.

La estrategia llama a `StartProtection` para aplicar stop-loss y take-profit cuando se especifican.

El `StepStochasticIndicator` es un port en C# del indicador MQL5 original y produce valores `Fast` y `Slow` para cada vela completada.
