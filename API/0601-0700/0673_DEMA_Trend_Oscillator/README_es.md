# Estrategia de Oscilador de Tendencia DEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia normaliza la Media Móvil Exponencial Doble (DEMA) con una media móvil y desviación estándar. Entra largo cuando el valor normalizado supera el umbral largo y el precio permanece por encima de la banda superior; entra corto cuando está por debajo del umbral corto y el precio está bajo la banda inferior. Utiliza stop trailing basado en ATR, stop-loss de banda y take profit de riesgo-recompensa.

## Detalles

- **Criterios de entrada**:
  - Largo: valor normalizado > `LongThreshold` y mínimo > banda superior
  - Corto: valor normalizado < `ShortThreshold` y máximo < banda inferior
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: el precio alcanza el take profit, stop-loss de banda o stop trailing
  - Corto: el precio alcanza el take profit, stop-loss de banda o stop trailing
- **Stops**: Stop-loss de banda, trailing ATR, take profit de riesgo-recompensa
- **Valores predeterminados**:
  - `DemaPeriod` = 40
  - `BaseLength` = 20
  - `LongThreshold` = 55m
  - `ShortThreshold` = 45m
  - `RiskReward` = 1.5m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: DEMA, SMA, StandardDeviation, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
