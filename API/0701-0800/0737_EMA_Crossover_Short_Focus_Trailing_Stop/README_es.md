# Estrategia de Cruce EMA con Enfoque Corto y Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia va largo cuando la EMA de 13 está por encima de la EMA de 33 y no existe una posición corta. Va corto cuando la EMA de 13 está por debajo de la EMA de 33 y no hay una posición larga abierta. Las posiciones salen cuando la EMA de 13 cruza la EMA opuesta y un stop trailing sigue los extremos recientes.

## Detalles
- **Criterios de entrada:**
  - **Largo:** EMA de 13 ≥ EMA de 33 y posición ≤ 0.
  - **Corto:** EMA de 13 ≤ EMA de 33 y posición ≥ 0.
- **Largo/Corto:** ambos.
- **Criterios de salida:** el largo sale cuando EMA de 13 < EMA de 33; el corto sale cuando EMA de 13 > EMA de 25.
- **Stops:** stop trailing con distancia `TrailDistance` y desplazamiento `TrailOffset`.
- **Valores predeterminados:** short EMA = 13, mid EMA = 25, long EMA = 33, trail distance = 10, trail offset = 2.
