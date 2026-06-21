# Estrategia de Ruptura de Swing PRO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura que opera cuando el precio cierra más allá del último swing alto o bajo confirmado. La distancia entre los últimos puntos de swing define los niveles de stop-loss y objetivo.

## Detalles

- **Largo**: cierre anterior por encima del último swing alto y máximo actual por encima del máximo anterior.
- **Corto**: cierre anterior por debajo del último swing bajo y mínimo actual por debajo del mínimo anterior.
- **Stops**: nivel de swing opuesto.
- **Objetivos**: rango entre el último swing alto y bajo.
- **Indicadores**: cálculo interno de pivotes.
