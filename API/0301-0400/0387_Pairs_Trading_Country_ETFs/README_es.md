# Estrategia de Trading en Pares de ETFs de Países
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de reversión a la media opera un par de ETFs de países basándose en la puntuación z de su ratio de precios. Cuando el ratio se desvía más allá de un umbral, el sistema entra en una posición largo/corto esperando que el diferencial revierta hacia su media.

El ratio de precios se rastrea con una ventana deslizante y las posiciones se cierran cuando la puntuación z cruza el nivel de salida.

## Detalles

- **Universo**: exactamente dos ETFs de países.
- **Señal**: puntuación z del ratio de precios deslizante que supera `EntryZ`.
- **Salida**: cerrar cuando la puntuación z revierte a `ExitZ`.
- **Datos**: velas diarias, ventana de 60 días por defecto.
- **Control de riesgo**: órdenes omitidas si el valor de la operación está por debajo de `MinTradeUsd`.
