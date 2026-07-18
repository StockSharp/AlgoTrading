# Estrategia Terminator V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el oscilador de Convergencia/Divergencia de Medias Móviles (MACD) para operar en ambas direcciones. Se abre una posición larga cuando la línea MACD cruza por encima de su línea de señal. Se abre una posición corta cuando la línea MACD cruza por debajo de su línea de señal. Las posiciones están protegidas por niveles fijos de stop-loss y take-profit, mientras que un trailing stop opcional puede asegurar ganancias durante tendencias fuertes.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `MACD` cruza por encima de la línea de señal.
  - **Corto**: `MACD` cruza por debajo de la línea de señal.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Se alcanza el nivel de stop-loss o take-profit.
  - Se activa el trailing stop.
- **Stops**: Sí, incluye stop-loss, take-profit y trailing stop opcional.
- **Valores predeterminados**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 26
  - `SignalPeriod` = 1
  - `TakeProfit` = 500 puntos
  - `StopLoss` = 2500 puntos
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
