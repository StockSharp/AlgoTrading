# Estrategia de Scalping MartinGale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El cruce de SMA(3) con SMA(8) activa las entradas con piramidación estilo martingala. Se añaden órdenes adicionales en cada barra hasta que se alcanza el stop o el take-profit.

## Detalles

- **Criterios de entrada**: `SMA3` por encima de `SMA8` para largos, por debajo para cortos; se añaden nuevas entradas mientras persiste la señal.
- **Largo/Corto**: Configurable mediante `TradingMode`.
- **Criterios de salida**: El precio alcanza `TakeProfit` o `StopLoss` y relación SMA opuesta.
- **Stops**: Sí, basados en el valor de la SMA lenta.
- **Valores predeterminados**:
  - `FastLength` = 3
  - `SlowLength` = 8
  - `TakeProfit` = 1.03
  - `StopLoss` = 0.95
  - `TradingMode` = Long
  - `CandleType` = 5 minutes
  - `MaxPyramids` = 5
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Configurable
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
