# Estrategia de Tendencia EMA WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina un filtro de tendencia EMA con señales de Williams %R. Compra en niveles de sobreventa y vende en niveles de sobrecompra. Un umbral de retroceso previene entradas consecutivas. Las salidas opcionales cierran las operaciones en extremos opuestos de Williams %R o después de varios barrotes no rentables.

## Detalles

- **Criterios de entrada**:
  - Largo: Williams %R <= -100 y tendencia EMA alcista
  - Corto: Williams %R >= 0 y tendencia EMA bajista
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Williams %R cruza el extremo opuesto cuando `UseWprExit` está habilitado
  - La posición permanece no rentable durante `MaxUnprofitBars` barras cuando `UseUnprofitExit` está habilitado
- **Stops**: No
- **Valores predeterminados**:
  - `WprPeriod` = 46
  - `WprRetracement` = 30
  - `EmaPeriod` = 144
  - `BarsInTrend` = 1
  - `MaxUnprofitBars` = 5
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: EMA, Williams %R
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
