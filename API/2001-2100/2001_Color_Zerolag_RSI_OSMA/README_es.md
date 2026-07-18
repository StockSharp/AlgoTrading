# Estrategia Color Zerolag RSI OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza un oscilador compuesto construido a partir de cinco cálculos RSI con diferentes períodos. La suma ponderada de los valores RSI se suaviza dos veces para producir una línea OSMA de cero rezago.

## Cómo Funciona

1. Calcular cinco valores RSI con períodos 8, 21, 34, 55 y 89.
2. Multiplicar cada RSI por su peso y sumar los resultados.
3. Aplicar dos pasos de suavizado a la suma para obtener el valor OSMA.
4. Si el OSMA gira hacia arriba (el valor anterior era más bajo que hace dos barras y el valor actual supera el anterior), la estrategia cierra posiciones cortas y opcionalmente abre una larga.
5. Si el OSMA gira hacia abajo (el valor anterior era más alto que hace dos barras y el valor actual cae por debajo del anterior), la estrategia cierra posiciones largas y opcionalmente abre una corta.

## Parámetros

- **Smoothing 1, Smoothing 2** – longitudes de las fases de suavizado.
- **Factor 1..5** – pesos para cada componente RSI.
- **RSI Period 1..5** – períodos de los indicadores RSI.
- **Allow Buy / Allow Sell** – habilitar apertura de posiciones largas o cortas.
- **Close Long / Close Short** – cerrar posiciones existentes ante señales opuestas.
- **Candle Type** – marco temporal de las velas procesadas (por defecto 4 horas).

## Notas

La estrategia opera únicamente sobre velas finalizadas. La protección de posición se inicia automáticamente cuando la estrategia comienza.
