# Sistema de Confirmación de Volumen para una Tendencia (Estrategia)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el Indicador de Impulso de Tendencia (TTI), el Indicador de Confirmación de Precio por Volumen (VPCI) y el ADX para confirmar tendencias alcistas.

## Detalles
- **Criterios de entrada**:
  - **Largo**: ADX > 30, TTI > señal, VPCI > 0.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - VPCI < 0.
- **Stops**: No.
- **Valores predeterminados**:
  - `ADX Length` = 14
  - `ADX Smoothing` = 14
  - `TTI Fast Average` = 13
  - `TTI Slow Average` = 26
  - `TTI Signal Length` = 9
  - `VPCI Short Avg` = 5
  - `VPCI Long Avg` = 25
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: ADX, TTI, VPCI
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Medio plazo
