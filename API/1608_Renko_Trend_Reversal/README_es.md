# Estrategia de Reversión de Tendencia Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Reversión de Tendencia Renko opera cuando la apertura Renko cruza el cierre Renko. El stop-loss y el take-profit pueden activarse. Usa bloques Renko basados en ATR.

## Detalles

- **Criterios de entrada**: cruce de apertura/cierre Renko con ventana de tiempo
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop loss o take profit opcionales
- **Stops**: Opcional
- **Valores predeterminados**:
  - `RenkoAtrLength` = 10
  - `StopLossPct` = 10
  - `TakeProfitPct` = 50
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Renko
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
