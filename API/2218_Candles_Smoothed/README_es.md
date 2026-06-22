# Estrategia de Velas Suavizadas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el color de las velas suavizadas. Para cada vela completada, la diferencia entre el precio de cierre y apertura se pasa a través de una media móvil. Cuando esta diferencia suavizada cambia de signo, el "color" de la vela cambia y la estrategia invierte su posición.

## Lógica

1. Suscribirse a una serie de velas configurable.
2. Calcular `diff = close - open` para cada vela completada.
3. Suavizar el `diff` utilizando la media móvil seleccionada.
4. Determinar el color de la vela:
   - **Color 0** si `smoothed diff > 0` (cierre por encima de la apertura).
   - **Color 1** en caso contrario.
5. Generar señales:
   - **Comprar** cuando el color cambia de 0 a 1.
   - **Vender** cuando el color cambia de 1 a 0.
6. La posición actual se cierra antes de abrir una nueva.

## Parámetros

- `CandleType` – marco temporal de las velas procesadas. Por defecto 1 hora.
- `MaLength` – longitud de la media móvil de suavizado. Por defecto 30.
- `MaMethods` – algoritmo de media móvil: `Simple`, `Exponential`, `Smma` o `Weighted`. Por defecto `Weighted`.

## Notas

- La estrategia usa órdenes de mercado a través de `BuyMarket` y `SellMarket`.
- Se usa la API de alto nivel para la suscripción a velas y la visualización en gráficos.
- Los valores del indicador se acceden mediante `TryGetValue` para evitar llamadas directas al buffer.
