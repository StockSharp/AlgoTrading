# Estrategia de Índice de Fuerza Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en base a un Force Index suavizado que cruza niveles definidos por el usuario. Cuando el indicador sube por encima del nivel alto o cae por debajo del nivel bajo, la estrategia abre o cierra posiciones dependiendo del modo de operación seleccionado. El Force Index se calcula a partir del cambio de precio y el volumen, y se suaviza con una EMA.

## Detalles

- **Criterios de entrada**
  - *Modo directo*:
    - **Largo**: el indicador cruza por encima de `HighLevel`.
    - **Corto**: el indicador cruza por debajo de `LowLevel`.
  - *Modo contra tendencia*:
    - **Largo**: el indicador cruza por debajo de `LowLevel`.
    - **Corto**: el indicador cruza por encima de `HighLevel`.
- **Criterios de salida**
  - *Modo directo*:
    - **Largo**: cruce por debajo de `LowLevel`.
    - **Corto**: cruce por encima de `HighLevel`.
  - *Modo contra tendencia*:
    - **Largo**: cruce por encima de `HighLevel`.
    - **Corto**: cruce por debajo de `LowLevel`.
- **Stops**: No.
- **Valores predeterminados**:
  - `Period` = 30
  - `HighLevel` = 0
  - `LowLevel` = 0
  - `Candle Type` = 4-hour
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Force Index
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
