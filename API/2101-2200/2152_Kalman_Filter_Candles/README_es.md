# Estrategia de Velas con Filtro Kalman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica el Filtro Kalman a los precios de apertura y cierre de cada vela. Las velas suavizadas resultantes se clasifican como alcistas o bajistas dependiendo de si el cierre suavizado está por encima o por debajo de la apertura suavizada. Las posiciones se abren cuando el color de la vela cambia:

- **Alcista (rosa)** &rarr; abre una posición larga y cierra cualquier posición corta.
- **Bajista (azul)** &rarr; abre una posición corta y cierra cualquier posición larga.

## Parámetros

- `Process Noise` &ndash; factor de suavizado para el Filtro Kalman.
- `Candle Type` &ndash; marco temporal de las velas utilizadas en la estrategia.

## Cómo Funciona

1. Para cada vela terminada, los precios de apertura y cierre se suavizan individualmente utilizando Filtros Kalman separados.
2. Se genera una señal alcista cuando el cierre suavizado supera la apertura suavizada. Se produce una señal bajista cuando el cierre suavizado está por debajo de la apertura suavizada.
3. La estrategia entra en una posición larga con una señal alcista y en una posición corta con una señal bajista. Las posiciones opuestas se cierran automáticamente.

La estrategia está pensada como ejemplo de combinación de múltiples Filtros Kalman para formar un sistema simple de seguimiento de tendencia.
