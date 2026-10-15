# Scalp ChopFlow ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

ChopFlow ATR Scalp entra cuando el mercado sale de condiciones de movimiento lateral y el OBV cruza su EMA. Las salidas usan stops y objetivos simétricos basados en ATR.

El objetivo es capturar movimientos rápidos durante la formación temprana de tendencias.

## Detalles

- **Criterios de entrada**: `Choppiness < ChopThreshold` y OBV por encima/debajo de su EMA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop o distancia de take-profit basada en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `ChopLength` = 14
  - `ChopThreshold` = 60
  - `ObvEmaLength` = 10
  - `SessionInput` = "1700-1600"
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Scalping
  - Dirección: Ambos
  - Indicadores: ATR, Choppiness, OBV
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
