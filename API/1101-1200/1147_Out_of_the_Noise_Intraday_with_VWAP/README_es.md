# Estrategia Intradía "Out of the Noise" con VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa el enfoque de rompimiento intradía "Out of the Noise". La estrategia construye límites dinámicos superiores e inferiores alrededor de la apertura de la sesión utilizando movimientos absolutos promedio durante los últimos *Period* días.

Las posiciones largas se abren cuando el precio rompe por encima del límite superior, mientras que las posiciones cortas se abren por debajo del límite inferior. Las posiciones existentes salen en un cruce de VWAP o al tocar el límite opuesto. El tamaño de la posición puede escalar opcionalmente a un objetivo de volatilidad derivado de la desviación estándar diaria.
