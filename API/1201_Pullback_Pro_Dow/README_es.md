# Estrategia Pullback Pro Dow
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza los pivotes de la Teoría Dow para definir la dirección de la tendencia y entra en retrocesos de EMA cuando la fuerza de la tendencia está confirmada por el ADX. El sistema escala la salida en dos objetivos de riesgo-recompensa.

Los backtests muestran un comportamiento estable en futuros de índices como el US30.

## Detalles

- **Criterios de entrada**:
  - Largo: máximos y mínimos más altos, mínimo cruza por debajo de la EMA, ADX por encima del umbral
  - Corto: máximos y mínimos más bajos, máximo cruza por encima de la EMA, ADX por encima del umbral
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop en el último pivote, toma de beneficios en dos objetivos R:R
- **Stops**: Basado en pivotes
- **Valores predeterminados**:
  - `PivotLookback` = 10
  - `EmaLength` = 21
  - `RiskReward1` = 1.5m
  - `Tp1Percent` = 50
  - `RiskReward2` = 3m
  - `UseAdxFilter` = true
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, Average Directional Index
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
