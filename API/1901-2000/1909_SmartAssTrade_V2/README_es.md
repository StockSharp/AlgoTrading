# Estrategia SmartAssTrade V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia SmartAssTrade V2 utiliza el histograma MACD y medias móviles de 20 períodos en múltiples marcos temporales (1m, 5m, 15m, 30m, 60m), combinados con filtros de Williams %R y RSI para capturar el impulso de tendencia. Un trailing stop opcional protege las ganancias.

## Detalles

- **Criterios de entrada**: la mayoría de los marcos temporales muestran histograma MACD y MA en ascenso con confirmación de WPR/RSI
- **Largo/Corto**: Ambos
- **Criterios de salida**: el precio alcanza el take profit o el stop loss; trailing stop opcional
- **Stops**: Stop loss y take profit absolutos con trailing opcional
- **Valores predeterminados**:
  - `Volume` = 1
  - `TakeProfit` = 35
  - `StopLoss` = 62
  - `UseTrailingStop` = false
  - `TrailingStop` = 30
  - `TrailingStopStep` = 1
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD, SMA, Williams %R, RSI
  - Stops: Opcional
  - Complejidad: Intermedio
  - Marco temporal: Multi-marco temporal (1m,5m,15m,30m,60m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
