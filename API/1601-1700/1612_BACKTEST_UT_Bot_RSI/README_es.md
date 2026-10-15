# Estrategia Backtest UT Bot + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina un detector de tendencia UT Bot con niveles de RSI. Entra largo en una reversión alcista del UT Bot cuando el RSI está sobrevendido, y corto en una reversión bajista cuando el RSI está sobrecomprado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: UT Bot gira hacia arriba y RSI < `RSI Oversold`.
  - **Corto**: UT Bot gira hacia abajo y RSI > `RSI Overbought`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Porcentajes de take profit o stop loss.
- **Stops**: Take Profit y Stop Loss.
- **Valores predeterminados**:
  - `RSI Length` = 14
  - `RSI Overbought` = 60
  - `RSI Oversold` = 40
  - `ATR Length` = 10
  - `UT Bot Factor` = 1.0
  - `Take Profit %` = 3.0
  - `Stop Loss %` = 1.5
- **Filtros**:
  - Categoría: Trend Following
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
