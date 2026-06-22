# Estrategia CrossMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera un cruce de medias móviles simples con un stop loss basado en ATR. Se abre una posición larga cuando la SMA rápida cruza por encima de la SMA lenta. Se abre una posición corta cuando la SMA rápida cruza por debajo de la SMA lenta. Tras entrar en una posición, se coloca un stop loss a una distancia de un ATR del precio de entrada y se verifica en cada nueva vela.

## Parámetros
- Tipo de vela
- Período de SMA rápida
- Período de SMA lenta
- Período de ATR
- Volumen
