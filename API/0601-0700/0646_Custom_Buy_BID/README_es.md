# Estrategia de Compra Personalizada BID
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Custom Buy BID utiliza el indicador Supertrend para identificar reversiones alcistas. Abre una posición larga cuando el precio cruza por encima de la línea Supertrend y aplica objetivos de ganancia y pérdida configurables para la gestión del riesgo.

## Detalles

- **Criterios de entrada**: El precio cruza por encima del Supertrend.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Take Profit o Stop Loss.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `TakeProfitPercent` = 5m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StartDate` = 2018-09-01
  - `EndDate` = 9999-01-01
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: Supertrend
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
