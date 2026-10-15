# Estrategia CCI + EMA con TP/SL Porcentual o ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el Índice de Canal de Materias Primas (CCI) con un filtro de tendencia EMA opcional y confirmación RSI.
Las posiciones se abren cuando el CCI sale de zonas extremas y los filtros opcionales permiten operar.
El take profit y el stop loss pueden calcularse como porcentajes del precio de entrada o usando niveles basados en ATR con una relación riesgo-recompensa.

## Detalles

- **Condiciones de entrada:**
  - **Largo:** El CCI cruza por encima del nivel de sobreventa, precio sobre la EMA (si está activada), RSI por debajo de sobreventa (si está activado).
  - **Corto:** El CCI cruza por debajo del nivel de sobrecompra, precio bajo la EMA (si está activada), RSI por encima de sobrecompra (si está activado).
- **Condiciones de salida:**
  - Niveles de take-profit o stop-loss alcanzados.
  - Las posiciones largas se cierran cuando el CCI cruza por encima del nivel de sobrecompra.
  - Las posiciones cortas se cierran cuando el CCI cruza por debajo del nivel de sobreventa.

Los parámetros predeterminados siguen el script original.
