# Estrategia de Índice de Probabilidad de ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Probability of ATR Index.

## Detalles

- **Criterios de entrada**: La probabilidad cruza por encima o por debajo de su media móvil.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `AtrDistance` = 1.5m
  - `Bars` = 8
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: ATR, SMA, StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
