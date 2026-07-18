# Estrategia de Ejemplo de Orden
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura convertida desde el ejemplo MQL5 `OrderExample.mq5`.
Entra en operaciones cuando el precio rompe por encima de máximos recientes o por debajo de mínimos recientes.

La estrategia utiliza los indicadores `Highest` y `Lowest` para rastrear los niveles de ruptura en una ventana configurable.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close` rompe por encima del máximo más alto de `Lookback` velas
  - Corto: `Close` rompe por debajo del mínimo más bajo de `Lookback` velas
- **Largo/Corto**: Ambos
- **Criterios de salida**: Ruptura opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `Lookback` = 26
  - `CandleType` = `TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
