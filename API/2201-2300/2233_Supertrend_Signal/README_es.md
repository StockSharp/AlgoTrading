# Estrategia de Señal Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre posiciones cuando el precio de cierre cruza la línea SuperTrend. Se coloca una operación larga cuando el precio sube por encima de la línea, y se abre una operación corta cuando el precio cae por debajo de ella. Las señales opuestas cierran e invierten las posiciones existentes.

El indicador SuperTrend utiliza el Rango Verdadero Promedio (ATR) para seguir el precio y definir la tendencia predominante. Los parámetros permiten configurar el período ATR, el multiplicador y el marco temporal de las velas.

## Detalles

- **Criterios de entrada**:
  - Largo: El precio de cierre cruza por encima de SuperTrend
  - Corto: El precio de cierre cruza por debajo de SuperTrend
- **Largo/Corto**: Largo y Corto
- **Criterios de salida**:
  - Cruce opuesto de SuperTrend
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `AtrPeriod` = 5
  - `Multiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend (basado en ATR)
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Medio plazo
  - Estacionalidad: Ninguno
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
