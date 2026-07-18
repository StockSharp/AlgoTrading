# Estrategia de Puntuación EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia evalúa la dirección del mercado usando tres líneas EMA y opera cuando se supera un umbral de puntuación.

## Detalles
- **Criterios de entrada**:
  - **Largo**: La puntuación cruza por encima del umbral.
  - **Corto**: La puntuación cruza por debajo del umbral negativo.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal inversa.
- **Stops**: No.
- **Valores predeterminados**:
  - `Short EMA Period` = 21
  - `Medium EMA Period` = 50
  - `Long EMA Period` = 100
  - `Score Threshold` = 4
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Medio plazo
