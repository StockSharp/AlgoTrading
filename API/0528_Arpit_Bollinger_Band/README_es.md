# Estrategia Arpit Bollinger Band
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura de Bollinger Band que espera un cierre fuera de las bandas hace dos velas y entra cuando el precio rompe el extremo de esa barra.

- **Indicadores**: Bollinger Bands (EMA 20, desviación 1.5)
- **Entrada**: Largo cuando el precio cerró por debajo de la banda inferior hace dos barras y el máximo actual supera el máximo de esa barra. Corto cuando el precio cerró por encima de la banda superior hace dos barras y el mínimo actual cae por debajo del mínimo de esa barra.
- **Stops**: Stop colocado más allá del rango de la vela actual con un buffer del 5% y take profit basado en una relación riesgo‑beneficio.

