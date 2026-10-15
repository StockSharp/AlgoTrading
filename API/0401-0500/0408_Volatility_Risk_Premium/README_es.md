# Estrategia de Prima de Riesgo por Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia vende opciones para capturar la prima de riesgo por volatilidad, esperando que la volatilidad implícita supere a la realizada en promedio. Las posiciones son delta-cubiertas para aislar la prima.

La exposición corta en opciones se gestiona con controles de riesgo estrictos y recobertura periódica.

## Detalles

- **Datos**: Volatilidad implícita de opciones y volatilidad realizada.
- **Entrada**: Vender opciones fuera del dinero cuando implícita > realizada.
- **Salida**: Recomprar al vencimiento o cuando la volatilidad se dispara.
- **Instrumentos**: Opciones sobre índices o FX.
- **Riesgo**: Cobertura delta y stop-loss sobre vega.

