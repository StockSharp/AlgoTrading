# Estrategia de Diferencia EMA Reflejada RED
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia refleja la distancia entre dos Hull Moving Averages y sigue un valor suavizado. Cuando el reflejo suavizado se revierte en un porcentaje especificado, entra en posiciones largas o cortas según corresponda.

## Detalles

- **Criterios de entrada**:
  - Largo: el reflejo suavizado sube por encima de su límite de retroceso.
  - Corto: el reflejo suavizado cae por debajo de su límite de retroceso.
- **Largo/Corto**: Ambos
- **Valores predeterminados**:
  - `Smoothing Period` = 2
  - `Change Percent` = 0.04
