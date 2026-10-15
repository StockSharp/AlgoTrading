# Estrategia de Regresión Múltiple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera cuando el precio cruza una línea de regresión y gestiona el riesgo con límites basados en volatilidad. Los niveles opcionales de stop loss y take profit se derivan de una medida de riesgo seleccionada.

## Detalles

- **Criterios de entrada**: El precio cruza por encima o por debajo del valor de regresión.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o cuando el precio alcanza los límites seleccionados.
- **Stops**: Opcional, basado en `UseStopLoss` y `UseTakeProfit`.
- **Valores predeterminados**:
  - `Length` = 90
  - `RiskMeasure` = Atr
  - `RiskMultiplier` = 1
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: LinearRegression, ATR/StdDev/Bollinger/Keltner
  - Stops: Opcional
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
