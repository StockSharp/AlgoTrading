# Estrategia Fibonacci TP SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa niveles de retroceso Fibonacci para generar entradas con stop-loss basado en ATR y take-profit porcentual. El trading está limitado por un intervalo mínimo de barras entre operaciones y un límite de ganancia semanal.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close <= Fib 38.2%` && `Close >= Fib 78.6%` && `Min bars since last trade`
  - **Corto**: `Close <= Fib 23.6%` && `Close >= Fib 61.8%` && `Min bars since last trade`
- **Largo/Corto**: Ambos lados
- **Criterios de salida**:
  - `ATR stop-loss` o `Take-profit`
- **Stops**: Sí
- **Valores predeterminados**:
  - `Take Profit %` = 4
  - `Min Bars Between Trades` = 10
  - `Lookback` = 100
  - `ATR Period` = 14
  - `ATR Multiplier` = 1.5
  - `Max Weekly Return` = 0.15

- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Highest, Lowest, ATR
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
