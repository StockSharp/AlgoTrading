# Estrategia de Pivotes de Orden Superior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Detecta máximos y mínimos pivote de primer, segundo y tercer orden utilizando definiciones de pivote de 3 o 5 barras. La estrategia es analítica y no coloca órdenes.

## Detalles

- **Criterios de entrada**:
  - Ninguno (solo análisis).
- **Criterios de salida**:
  - Ninguno.
- **Indicadores**:
  - Detector de pivotes de 3 o 5 barras.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `CandleType` = 5m
  - `UseThreeBar` = true
  - `DisplayFirstOrder` = true
  - `DisplaySecondOrder` = true
  - `DisplayThirdOrder` = true
- **Filtros**:
  - Marco temporal único
  - Indicadores: detector de pivotes
  - Stops: ninguno
  - Complejidad: Bajo
