# Estrategia de Retroceso EMA WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que combina un filtro de tendencia EMA con extremos de Williams %R. Espera un retroceso en Williams %R antes de permitir otra operación y puede piramidear hasta un número establecido de posiciones.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Williams %R cae por debajo de -100 y luego ocurre un retroceso por encima de `WPR Retracement`. Tendencia alcista opcional confirmada por EMA.
  - **Corto**: Williams %R sube por encima de 0 y luego retrocede por debajo de `-WPR Retracement`. Tendencia bajista opcional confirmada por EMA.
- **Largo/Corto**: Ambas direcciones con piramidación.
- **Criterios de salida**:
  - Williams %R sale de la zona extrema.
  - Salida opcional después de `Max Unprofit Bars` sin ganancias.
  - Stop-loss, toma de ganancias y stop trailing opcional gestionados por módulo de protección.
- **Stops**: Stop-loss y toma de ganancias fijos con stop trailing opcional.
- **Valores predeterminados**:
  - `Use EMA Trend` = true
  - `Bars In Trend` = 1
  - `EMA Trend` = 144
  - `WPR Period` = 46
  - `WPR Retracement` = 30
  - `Use WPR Exit` = true
  - `Order Volume` = 0.1
  - `Max Trades` = 2
  - `Stop Loss` = 50
  - `Take Profit` = 200
  - `Use Trailing` = false
  - `Trailing Stop` = 10
  - `Use Unprofit Exit` = false
  - `Max Unprofit Bars` = 5
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, Williams %R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
