# Backtest de Estrategia LANZ 4.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Backtest de la Estrategia LANZ 4.0 es una estrategia de ruptura que utiliza pivotes de swing para detectar cambios de tendencia. Cuando el precio rompe por encima del último máximo de pivote, entra en largo; cuando el precio rompe por debajo del último mínimo de pivote, entra en corto. El tamaño de posición se calcula a partir del porcentaje de riesgo y el valor en pips, con stop-loss por debajo/encima del último swing más un buffer y take-profit por ratio riesgo-recompensa.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: El precio cruza por encima del último máximo de pivote.
  - **Corto**: El precio cruza por debajo del último mínimo de pivote.
- **Criterios de salida**: Stop-loss o take-profit.
- **Stops**: Máximo/mínimo de swing reciente con buffer.
- **Valores predeterminados**:
  - `SwingLength` = 180
  - `SlBufferPoints` = 50
  - `RiskReward` = 1
  - `RiskPercent` = 1
  - `PipValueUsd` = 10
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo y corto
  - Indicadores: Highest, Lowest
  - Complejidad: Moderado
  - Nivel de riesgo: Medio
