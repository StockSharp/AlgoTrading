# Estrategia Multi-TF AI SuperTrend con ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina dos indicadores SuperTrend filtrados por una verificación de fuerza ADX. La dirección de la tendencia se confirma comparando las WMA del precio con las WMA del SuperTrend. Las operaciones largas se abren cuando ambos SuperTrend son alcistas y el ADX muestra fuerza positiva. Las operaciones cortas se abren bajo condiciones opuestas. El ATR del primer SuperTrend proporciona un stop trailing.

- **Largo**: Ambos SuperTrend alcistas, WMA del precio por encima de las WMA del SuperTrend, +DI > -DI y ADX por encima del umbral.
- **Corto**: Ambos SuperTrend bajistas, WMA del precio por debajo de las WMA del SuperTrend, -DI > +DI y ADX por encima del umbral.
- **Indicadores**: SuperTrend, WMA, ATR, ADX.
- **Stops**: Stop trailing basado en el ATR del primer SuperTrend.
