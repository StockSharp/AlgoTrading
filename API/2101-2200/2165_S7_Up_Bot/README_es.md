# Estrategia S7 Up Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de ruptura que busca máximos o mínimos casi iguales seguidos de un movimiento brusco de precio.
Cuando dos mínimos consecutivos son casi iguales y el precio sube `Span Price`, el bot entra largo.
Entra corto cuando dos máximos se alinean y el precio cae `Span Price`.
Las posiciones están protegidas con funciones opcionales de take-profit, stop-loss, trailing stop y salida anticipada.

## Detalles

- **Criterios de entrada:**
  - **Compra:** La diferencia entre el mínimo actual y el anterior es menor que `HL Divergence` y el precio está `Span Price` por encima del mínimo.
  - **Venta:** La diferencia entre el máximo actual y el anterior es menor que `HL Divergence` y el precio está `Span Price` por debajo del máximo.
- **Largo/Corto:** Ambos.
- **Criterios de salida:**
  - Take-profit o stop-loss.
  - Trailing stop o ajuste de trailing a cero.
  - Salida anticipada si el precio cruza el máximo/mínimo anterior (`Exit At Extremum`) o se acerca al nivel de reversión (`Exit At Reversal`).
- **Stops:** Take-profit y stop-loss absolutos con trailing opcional.
- **Filtros:** Ninguno.

## Parámetros

- `Take Profit` – objetivo de ganancia en unidades de precio.
- `Stop Loss` – límite de pérdida en unidades de precio, 0 para stop automático basado en extremos.
- `HL Divergence` – diferencia máxima permitida entre dos máximos o mínimos consecutivos.
- `Span Price` – distancia desde el extremo al precio requerida para la entrada.
- `Max Trades` – número máximo de operaciones simultáneas.
- `Use Trailing Stop` – habilitar el mecanismo de trailing stop.
- `Trail Stop` – distancia del trailing stop.
- `Zero Trailing` – mover el stop hacia el precio una vez que la posición sea rentable.
- `Step Trailing` – paso mínimo para ajustar el trailing a cero.
- `Exit At Extremum` – cerrar si el precio cruza el máximo/mínimo anterior.
- `Exit At Reversal` – cerrar si el precio se acerca al extremo opuesto.
- `Span To Revers` – distancia desde el extremo para activar la salida por reversión.
- `Candle Type` – marco temporal utilizado para el análisis.
- `Order Volume` – cantidad por operación.
