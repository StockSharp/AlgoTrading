# Estrategia de Reversión de Tendencia Renko V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Reversión de Tendencia Renko V2 opera cuando la apertura Renko cruza el cierre Renko. Utiliza bloques Renko basados en ATR y aplica niveles de stop-loss y take-profit. Los cortos pueden desactivarse.

## Detalles

- **Criterios de entrada**: cruce de apertura/cierre Renko con ventana de tiempo
- **Largo/Corto**: Ambos (cortos opcionales)
- **Criterios de salida**: stop loss o take profit
- **Stops**: Sí
- **Valores predeterminados**:
  - `RenkoAtrLength` = 10
  - `StopLossPct` = 3
  - `TakeProfitPct` = 20
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Renko
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
