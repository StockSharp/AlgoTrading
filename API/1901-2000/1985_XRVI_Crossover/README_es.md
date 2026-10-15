# Estrategia de Cruce XRVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Cruce XRVI se basa en el Extended Relative Vigor Index (XRVI).
El XRVI se calcula suavizando el Relative Vigor Index y luego aplicando una segunda media móvil para producir una línea de señal.
La estrategia entra en largo cuando el XRVI cruza por encima de la línea de señal y entra en corto cuando cruza por debajo.
Las posiciones existentes se invierten en señales opuestas.

## Detalles

- **Criterios de entrada**: Cruce del XRVI con su línea de señal
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruce opuesto
- **Stops**: No
- **Valores predeterminados**:
  - `RviLength` = 10
  - `SignalLength` = 5
  - `CandleType` = Marco temporal H4
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Relative Vigor Index, Simple Moving Average
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
