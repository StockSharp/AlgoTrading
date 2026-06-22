# Estrategia de Apertura y Cierre a Tiempo v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia basada en tiempo que abre operaciones a una hora específica y las cierra más tarde en el día. La dirección de la operación se confirma comparando una media móvil exponencial rápida y una lenta. Los niveles de stop-loss y take-profit se expresan en ticks.

## Detalles

- **Criterios de Entrada**: En `OpenTime`, ir largo si la EMA rápida está por encima de la EMA lenta, ir corto si está por debajo. La dirección depende de `TradeMode`.
- **Largo/Corto**: Configurable (comprar, vender, o ambos).
- **Criterios de Salida**: Las posiciones se cierran en `CloseTime` o por stops de protección.
- **Stops**: Sí, tanto stop-loss como take-profit en ticks.
- **Valores predeterminados**:
  - `OpenTime` = 05:00
  - `CloseTime` = 21:01
  - `SlowPeriod` = 200
  - `FastPeriod` = 50
  - `StopLossTicks` = 30
  - `TakeProfitTicks` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Basado en tiempo
  - Dirección: Configurable
  - Indicadores: EMA
  - Stops: Fijo
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
