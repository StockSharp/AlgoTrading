# Reversión de Tendencia RSI y ATR con SL TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza RSI y ATR para detectar reversiones de tendencia con niveles dinámicos de stop-loss y take-profit.

## Detalles

- **Criterios de entrada**: Precio cruzando el umbral adaptativo RSI/ATR.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: Integrados a través del umbral dinámico.
- **Valores predeterminados**:
  - `RsiLength` = 8
  - `RsiMultiplier` = 1.5
  - `Lookback` = 1
  - `MinDifference` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: RSI, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
