# Estrategia Sophia 1_1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sophia 1_1 es una estrategia de trading de cuadrícula basada en el principio de martingala.
La estrategia abre una posición después de cuatro velas consecutivas moviéndose en la misma dirección:
- Cuatro velas ascendentes activan una entrada corta.
- Cuatro velas descendentes activan una entrada larga.

Una vez en el mercado, el algoritmo añade posiciones cada vez que el precio se mueve contra la posición actual en un número fijo de pasos de precio (`Pip Step`).
El volumen de cada operación adicional se multiplica por `Lot Exponent`, formando una cuadrícula de martingala clásica.

La gestión del riesgo se maneja a través de `Take Profit`, `Stop Loss` y un trailing stop opcional.
El mecanismo de trailing se activa cuando el beneficio alcanza `Trail Start` y sigue el nivel de stop en `Trail Stop` pasos de precio.

## Parámetros
- **Volume** – volumen base para la primera operación.
- **Pip Step** – distancia en pasos de precio antes de añadir una nueva posición.
- **Lot Exponent** – multiplicador para el volumen de cada operación adicional.
- **Max Trades** – número máximo de posiciones en la cuadrícula.
- **Take Profit** – objetivo de beneficio en pasos de precio desde el precio de entrada promedio.
- **Stop Loss** – umbral de pérdida en pasos de precio desde el precio de entrada promedio.
- **Use Trailing** – habilitar o deshabilitar el trailing stop.
- **Trail Start** – beneficio requerido antes de que el trailing stop se active.
- **Trail Stop** – distancia del trailing stop en pasos de precio.
- **Candle Type** – marco temporal de las velas usadas para los cálculos.
