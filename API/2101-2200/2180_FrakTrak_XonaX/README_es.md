# Estrategia FrakTrak XonaX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

FrakTrak XonaX es una estrategia de ruptura basada en niveles de fractales calculados en un marco temporal superior. Cuando el precio se mueve más allá del fractal más reciente por un pequeño offset, la estrategia entra en la dirección de la ruptura. Un take profit fijo y un trailing stop gestionan la posición abierta.

## Parámetros
- **Volume** – tamaño de la orden.
- **Take Profit** – distancia en puntos para el nivel de take-profit.
- **Trailing Stop** – distancia en puntos utilizada para el trailing del stop-loss.
- **Trailing Correction** – distancia adicional añadida al trailing stop.
- **Candle Type** – marco temporal utilizado para construir velas y fractales.

## Reglas de trading
1. Calcular fractales superiores e inferiores usando los últimos candles completados.
2. Comprar cuando el precio de cierre supera el fractal superior más 15 puntos y no existe posición larga. El stop-loss se coloca en el último fractal inferior y el take-profit se configura usando *Take Profit*.
3. Vender cuando el precio de cierre cae por debajo del fractal inferior menos 15 puntos y no existe posición corta. El stop-loss se coloca en el último fractal superior y el take-profit se configura usando *Take Profit*.
4. Cuando una posición se vuelve rentable en más de *Trailing Stop* puntos, el stop-loss sigue al precio con un offset adicional de *Trailing Correction*.
