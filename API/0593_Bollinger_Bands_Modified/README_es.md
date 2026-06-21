# Estrategia de Bollinger Bands Modificada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera rupturas de Bollinger Bands con un filtro de tendencia EMA opcional. Entra en largo cuando el precio cruza por encima de la banda superior y en corto cuando cruza por debajo de la banda inferior.

El stop loss se coloca en el máximo o mínimo reciente y el take profit es un múltiplo del riesgo.

## Detalles

- **Criterios de entrada**:
  - Largo: el precio cruza por encima de la banda superior de Bollinger
  - Corto: el precio cruza por debajo de la banda inferior de Bollinger
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: stop en el mínimo reciente, objetivo en riesgo * factor
  - Corto: stop en el máximo reciente, objetivo en riesgo * factor
- **Stops**: Máximo/mínimo de las últimas N velas
- **Valores predeterminados**:
  - `BollingerLength` = 20
  - `BollingerDeviation` = 0.38m
  - `EmaLength` = 80
  - `HighestLength` = 7
  - `LowestLength` = 7
  - `TargetFactor` = 1.6m
  - `EmaTrend` = true
  - `CrossoverCheck` = false
  - `CrossunderCheck` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, EMA, Highest, Lowest
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
