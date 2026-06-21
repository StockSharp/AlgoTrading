# Estrategia de Stop Loss y Take Profit en Dinero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra largo cuando una SMA a corto plazo cruza por encima de una SMA a largo plazo y corto en el cruce opuesto. Las posiciones se cierran cuando el beneficio o la pérdida alcanza cantidades de dinero predefinidas.

## Detalles

- **Criterios de entrada**: SMA(14) cruza SMA(28)
- **Largo/Corto**: Ambos
- **Criterios de salida**: El beneficio o la pérdida en dinero alcanza el objetivo
- **Stops**: Sí
- **Valores predeterminados**:
  - `FastLength` = 14
  - `SlowLength` = 28
  - `TakeProfitMoney` = 200
  - `StopLossMoney` = 100
