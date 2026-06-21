# Estrategia Supertrend AT v1.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia basada en Supertrend que abre una posición larga cuando el Supertrend cambia de bajista a alcista y una posición corta cuando cambia de alcista a bajista. El tamaño de la posición se calcula a partir del riesgo por operación, y las salidas utilizan niveles de stop-loss y take-profit derivados del Supertrend anterior.

## Detalles

- **Criterios de entrada**: Cambio de dirección del Supertrend.
- **Largo/Corto**: Largo y Corto.
- **Criterios de salida**: Objetivo o stop alcanzado.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3m
  - `RiskPerTrade` = 2m
  - `RewardRatio` = 3m
  - `CommissionPercent` = 0.05m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: Supertrend
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
