# Estrategia de Scalping 15m EMA MACD RSI ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de scalping que combina un filtro de tendencia EMA de 50 períodos, el momentum del histograma MACD y los niveles del RSI. La gestión del riesgo utiliza stop loss y take profit basados en ATR.

La estrategia compra cuando el precio está por encima de la EMA, el histograma MACD es positivo y el RSI se sitúa entre 50 y el nivel de sobrecompra. Las posiciones cortas se abren cuando el precio está por debajo de la EMA, el histograma es negativo y el RSI está entre el nivel de sobreventa y 50. Los stops y los objetivos siguen al precio por múltiplos del ATR desde el cierre.

## Detalles

- **Criterios de entrada**: Precio relativo a la EMA, signo del histograma MACD, nivel del RSI.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss o take profit basados en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaPeriod` = 50
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `AtrPeriod` = 14
  - `SlAtrMultiplier` = 1m
  - `TpAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Scalping
  - Dirección: Ambos
  - Indicadores: EMA, MACD, RSI, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
