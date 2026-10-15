# Estrategia de Variación Color J
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que replica el Asesor Experto ColorJVariation utilizando la Media Móvil Jurik. Rastrea la pendiente de la JMA y entra cuando cambia la dirección. La estrategia soporta niveles absolutos de stop loss y take profit.

## Detalles

- **Criterios de entrada**:
  - Largo: `PrevSlopeDown && JMA turns up`
  - Corto: `PrevSlopeUp && JMA turns down`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Señal de reversión opuesta
- **Stops**: Absolutos mediante `StopLoss` y `TakeProfit`
- **Valores predeterminados**:
  - `JmaPeriod` = 12
  - `JmaPhase` = 100
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoría: Reversión de tendencia
  - Dirección: Ambos
  - Indicadores: Jurik Moving Average
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
