# Estrategia Mejorada de Nube Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia Ichimoku solo larga con filtro EMA de 171 días. La estrategia compra cuando el span A está por encima del span B, el precio rompe el máximo de hace 25 barras, Tenkan-sen está por encima de Kijun-sen y el cierre está por encima de la EMA. La posición se cierra cuando Tenkan cae por debajo de Kijun.

## Detalles

- **Criterios de entrada**: spanA > spanB, close > high[25], Tenkan > Kijun, close > EMA.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Tenkan < Kijun.
- **Stops**: No.
- **Valores predeterminados**:
  - `ConversionPeriods` = 7
  - `BasePeriods` = 211
  - `LaggingSpan2Periods` = 120
  - `Displacement` = 41
  - `EmaPeriod` = 171
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: Ichimoku, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
