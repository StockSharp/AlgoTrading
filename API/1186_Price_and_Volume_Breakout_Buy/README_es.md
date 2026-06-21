# Estrategia de Compra por Ruptura de Precio y Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra cuando precio y volumen rompen simultáneamente por encima de sus respectivos máximos del período de lookback mientras el precio se mantiene sobre la SMA de tendencia. Las operaciones cortas se activan cuando el precio cae por debajo del mínimo del lookback bajo la misma condición de volumen y filtro SMA. Las posiciones se cierran tras cinco cierres consecutivos en el lado opuesto de la SMA.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Close > máximo más alto anterior && Volume > volumen más alto anterior && Close > SMA
  - **Corto**: Close < mínimo más bajo anterior && Volume > volumen más alto anterior && Close < SMA
- **Largo/Corto**: Configurable
- **Criterios de salida**:
  - **Tendencia**: Cinco cierres más allá de la SMA
- **Stops**: No
- **Valores predeterminados**:
  - `PriceBreakoutPeriod` = 60
  - `VolumeBreakoutPeriod` = 60
  - `TrendlineLength` = 200
  - `OrderDirection` = "Long"
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Configurable
  - Indicadores: Highest, SMA, Volume
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
