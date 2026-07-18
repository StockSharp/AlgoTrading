# Monitor de Reglas FTMO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que monitorea las reglas del desafío FTMO y gestiona las operaciones basándose en el riesgo ATR.

La estrategia dimensiona las posiciones usando ATR y se detiene cuando se cumplen los objetivos del desafío. Monitorea la pérdida diaria máxima, la pérdida total, el objetivo de ganancia y los días mínimos de trading.

## Detalles

- **Criterios de entrada**: Vela alcista abre largo, vela bajista abre corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Desafío completado o señal opuesta.
- **Stops**: Basados en ATR.
- **Valores predeterminados**:
  - `AccountSize` = 10000m
  - `RiskPercent` = 1m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Gestión de riesgo
  - Dirección: Ambos
  - Indicadores: ATR
  - Stops: ATR
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
