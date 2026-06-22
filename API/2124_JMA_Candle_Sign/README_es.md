# Estrategia de Señal de Vela JMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza dos medias móviles Jurik (JMA) calculadas sobre los precios de apertura y cierre de cada vela. Una señal alcista ocurre cuando la JMA del precio de apertura cruza por debajo de la JMA del precio de cierre, lo que genera una entrada larga. Una señal bajista ocurre cuando la JMA del precio de apertura cruza por encima de la JMA del precio de cierre, lo que genera una entrada corta.

El marco temporal predeterminado son velas de cuatro horas con un período JMA de siete. Los niveles de stop loss y take profit se definen en puntos y se aplican a través de la gestión de riesgos integrada. La estrategia actúa únicamente sobre velas completadas y mantiene como máximo una posición abierta.

## Parámetros
- **JMA Length** – período para ambas JMA.
- **Candle Type** – marco temporal de las velas procesadas.
- **Take Profit** – objetivo de beneficio en puntos.
- **Stop Loss** – pérdida máxima en puntos.
