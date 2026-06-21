# Estrategia Back to the Future
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de momentum compara el precio de cierre actual con el precio de hace un número especificado de minutos. Cuando el precio avanza más allá de un umbral definido en relación con el precio histórico, el sistema abre una posición larga. Por el contrario, cuando el precio cae por debajo del umbral negativo, abre una posición corta. El enfoque asume que movimientos fuertes alejados del precio pasado indican tendencias emergentes.

La estrategia opera sobre velas completadas y funciona con cualquier instrumento y marco temporal compatible con StockSharp. Los niveles integrados de take-profit y stop-loss gestionan el riesgo una vez que se abre una posición. Una cola de precios pasados mantiene un historial deslizante para evaluar la diferencia de precio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close(t) - Close(t-Δ) > BarSize`.
  - **Corto**: `Close(t) - Close(t-Δ) < -BarSize`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: `Close >= Entry + TakeProfit` o `Close <= Entry - StopLoss`.
  - **Corto**: `Close <= Entry - TakeProfit` o `Close >= Entry + StopLoss`.
- **Stops**: Sí, take-profit y stop-loss fijos en unidades de precio.
- **Valores predeterminados**:
  - `BarSize = 0.25`
  - `HistoryMinutes = 60`
  - `TakeProfit = 10`
  - `StopLoss = 5000`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
