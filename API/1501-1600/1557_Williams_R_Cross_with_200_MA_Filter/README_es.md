# Estrategia de Cruce de Williams %R con Filtro de 200 MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera cruces de Williams %R alrededor del nivel -50 con un filtro de tendencia basado en SMA de 200 períodos.
Las posiciones se cierran con distancias fijas de objetivo de beneficio y stop.

## Detalles

- **Criterios de entrada**: %R cruza los umbrales con precio relativo a SMA 200
- **Largo/Corto**: Ambos
- **Criterios de salida**: objetivo de beneficio o stop
- **Stops**: Sí
- **Valores predeterminados**:
  - `WrLength` = 14
  - `CrossThreshold` = 10
  - `TakeProfit` = 30
  - `StopLoss` = 20
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: WilliamsR, SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
