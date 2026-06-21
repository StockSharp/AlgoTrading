# Estrategia de Sistema de Trading Simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el Simple Trading System de MetaTrader. Utiliza una media móvil desplazada varios barras y compara el cierre actual con cierres anteriores para detectar reversiones de tendencia a corto plazo. Una señal de compra ocurre cuando la media móvil está por debajo de su valor `MaShift` barras atrás y el cierre está entre los cierres de `MaShift` y `MaPeriod + MaShift` barras atrás mientras la vela es bajista. Una señal de venta es el opuesto espejo. Dependiendo de los parámetros, la estrategia puede abrir y/o cerrar posiciones largas o cortas cuando aparecen señales. Se pueden configurar niveles opcionales de stop-loss y take-profit.

## Detalles

- **Criterios de entrada:**
  - **Largo**: `MA(t) <= MA(t+MaShift)` && `Close(t) >= Close(t+MaShift)` && `Close(t) <= Close(t+MaPeriod+MaShift)` && `Close(t) < Open(t)`
  - **Corto**: `MA(t) >= MA(t+MaShift)` && `Close(t) <= Close(t+MaShift)` && `Close(t) >= Close(t+MaPeriod+MaShift)` && `Close(t) > Open(t)`
- **Largo/Corto**: Ambos lados dependiendo de `BuyPositionOpen` y `SellPositionOpen`.
- **Criterios de salida**: La señal opuesta activa el cierre si `BuyPositionClose` o `SellPositionClose` está habilitado.
- **Stops**: Opcional. `StopLoss` y `TakeProfit` en unidades absolutas de precio a través de `StartProtection`.
- **Valores predeterminados:**
  - `MaType` = EMA
  - `MaPeriod` = 2
  - `MaShift` = 4
  - `PriceType` = Close
  - `CandleType` = velas de 6 horas
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `Volume` = 1
- **Filtros:**
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Media Móvil
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
