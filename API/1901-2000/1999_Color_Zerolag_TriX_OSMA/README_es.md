# Estrategia Color Zerolag TriX OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

Esta estrategia utiliza un oscilador TRIX OSMA de cero rezago construido a partir de cinco períodos TRIX diferentes. Cada componente TRIX es ponderado y suavizado para formar un único oscilador que reacciona a los cambios de tendencia con un rezago mínimo. Se abre una posición larga cuando el oscilador gira hacia arriba y una posición corta cuando gira hacia abajo.

## Cómo Funciona

1. Calcular cinco valores TRIX usando medias móviles exponenciales triples y la tasa de cambio.
2. Combinar los valores TRIX con sus pesos para formar un valor de tendencia rápida.
3. Suavizar la tendencia rápida dos veces para crear un oscilador OSMA de cero rezago.
4. Detectar reversiones de tendencia comparando los últimos dos valores del oscilador.
5. Entrar largo en un giro hacia arriba y corto en un giro hacia abajo; las posiciones opuestas existentes se cierran antes de abrir una nueva.

## Parámetros

- `Smoothing1` – factor de suavizado para la tendencia lenta.
- `Smoothing2` – factor de suavizado para la línea OSMA.
- `Factor1..Factor5` – pesos aplicados a cada componente TRIX.
- `Period1..Period5` – períodos para los cinco cálculos TRIX.
- `CandleType` – serie de velas utilizada para los cálculos.

## Indicadores

- TripleExponentialMovingAverage
- RateOfChange
- Combinación personalizada de TRIX OSMA de cero rezago

## Notas

La estrategia requiere que los cinco indicadores TRIX estén formados antes de generar señales. La protección de stops y objetivos se activa mediante `StartProtection`.
