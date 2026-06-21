# Bandas de Bollinger con Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entra en largo cuando el precio cierra por encima de la banda superior de Bollinger.
Sale cuando el precio cae por debajo de la banda inferior o se activa un stop trailing basado en ATR.

## Detalles

- **Criterios de entrada**: Cierre por encima de la banda superior.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cierre por debajo de la banda inferior o activación del stop trailing.
- **Stops**: Stop trailing.
- **Valores predeterminados**:
  - `BbLength` = 20
  - `BbDeviation` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: Bollinger Bands, ATR
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
