# Estrategia Z-Score RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Z-Score RSI calcula el RSI sobre el z-score del precio y usa una EMA del RSI para generar señales. Se abre una posición larga cuando el RSI cruza por encima de su EMA y una posición corta cuando cruza por debajo.

## Detalles

- **Criterios de entrada**: El RSI del z-score cruza su EMA
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruce contrario
- **Stops**: No
- **Valores predeterminados**:
  - `ZScoreLength` = 20
  - `RsiLength` = 9
  - `SmoothingLength` = 15
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: SMA, StandardDeviation, RSI, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
