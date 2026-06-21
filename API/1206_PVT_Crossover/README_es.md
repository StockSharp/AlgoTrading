# Estrategia de Cruce de PVT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el cruce del indicador Price Volume Trend (PVT) y su media móvil exponencial (EMA). Se abre una posición larga cuando el PVT cruza por encima de su EMA, y una posición corta cuando cruza por debajo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: PVT cruza por encima de su EMA.
  - **Corto**: PVT cruza por debajo de su EMA.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Revertir posición ante señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `EmaLength` = 20.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: PVT, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
