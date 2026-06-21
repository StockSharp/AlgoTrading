# Estrategia Asimmetric Stoch NR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en líneas asimétricas del oscilador estocástico. La estrategia reacciona a los cruces de %K y %D y admite protección opcional de posición.

El método alterna periodos para el cálculo de %K para adaptarse al ruido del mercado. El stop-loss y el take-profit se aplican en unidades de precio absolutas.

## Detalles

- **Criterios de entrada**:
  - Largo: `%K` cruza por encima de `%D`
  - Corto: `%K` cruza por debajo de `%D`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: `%K` cruza por debajo de `%D`
  - Corto: `%K` cruza por encima de `%D`
- **Stops**: absolutos en `StopLoss` y `TakeProfit`
- **Valores predeterminados**:
  - `KPeriodShort` = 5
  - `KPeriodLong` = 12
  - `DPeriod` = 7
  - `Slowing` = 3
  - `Overbought` = 80m
  - `Oversold` = 20m
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Largo plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
