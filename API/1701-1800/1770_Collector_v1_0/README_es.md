# Estrategia Collector v1.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia abre órdenes de mercado cuando el precio alcanza niveles dinámicos de compra o venta separados por una distancia fija. El volumen aumenta después de un número especificado de operaciones. Todas las posiciones se cierran una vez que el beneficio acumulado supera un umbral.

## Detalles

- **Criterios de entrada**:
  - Largo: precio de cierre >= nivel de compra
  - Corto: precio de cierre <= nivel de venta
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cerrar todo cuando el beneficio total >= ProfitClose
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `Distance` = 10m
  - `InitialVolume` = 0.01m
  - `VolumeStep` = 0.01m
  - `IncreaseTrade` = 3
  - `MaxTrades` = 200
  - `ProfitClose` = 500000m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Cuadrícula
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
