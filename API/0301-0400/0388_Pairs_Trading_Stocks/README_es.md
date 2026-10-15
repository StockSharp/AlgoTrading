# Estrategia de Trading en Pares de Acciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia simplificada de trading en pares opera en múltiples pares de acciones. Para cada par, el ratio de precios se rastrea en una ventana deslizante y se calcula su puntuación z. Cuando la puntuación z supera un umbral de entrada se abre una operación largo/corto; las posiciones se cierran cuando la puntuación z revierte.

El algoritmo soporta el trading de múltiples pares independientes de forma simultánea.

## Detalles

- **Universo**: lista de pares de acciones.
- **Señal**: puntuación z del ratio de precios que cruza `EntryZ`.
- **Salida**: cerrar cuando la puntuación z alcanza `ExitZ`.
- **Datos**: velas diarias con retrospectiva de 60 días por defecto.
- **Control de riesgo**: operaciones omitidas cuando el valor de la orden está por debajo de `MinTradeUsd`.
