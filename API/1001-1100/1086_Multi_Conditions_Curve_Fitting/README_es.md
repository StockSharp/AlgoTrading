# Estrategia de Ajuste de Curva con Múltiples Condiciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina cruce de EMA, RSI y oscilador estocástico para operar cuando múltiples señales se alinean.

## Detalles

- **Criterios de entrada**:
  - Largo: `FastEMA > SlowEMA` y `RSI < RsiOversold` y `StochK < 20`
  - Corto: `FastEMA < SlowEMA` y `RSI > RsiOverbought` y `StochK > 80`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: `FastEMA < SlowEMA` o `RSI > RsiOverbought` o `StochK > StochD`
  - Corto: `FastEMA > SlowEMA` o `RSI < RsiOversold` o `StochK < StochD`
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 25
  - `RsiLength` = 14
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `StochLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, RSI, Stochastic
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
