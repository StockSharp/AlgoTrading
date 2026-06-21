# Estrategia de Trading con Doble AI Super Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza dos indicadores SuperTrend combinados con medias móviles ponderadas para confirmar la dirección de la tendencia. Las operaciones largas se abren cuando ambos SuperTrends son alcistas y las WMA de precio permanecen por encima de las WMA de SuperTrend correspondientes. Las operaciones cortas se producen en las condiciones opuestas. Las posiciones se gestionan con un stop trailing basado en ATR del primer SuperTrend.

- **Largo**: Ambos SuperTrends alcistas y WMA de precio por encima de las WMA de SuperTrend.
- **Corto**: Ambos SuperTrends bajistas y WMA de precio por debajo de las WMA de SuperTrend.
- **Indicadores**: SuperTrend, WMA, ATR.
- **Stops**: Stop trailing basado en el primer SuperTrend.
