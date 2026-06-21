# Estrategia MACD vs Signal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el cruce de la línea MACD con la línea de señal.

Entra largo cuando la línea MACD cruza hacia arriba de la línea de señal.
Entra corto cuando la línea MACD cruza hacia abajo de la línea de señal.
Opcionalmente aplica stop-loss, take-profit y stop de seguimiento.

## Detalles

- **Criterios de entrada**:
  - Largo: `MACD cruza por encima de Signal`
  - Corto: `MACD cruza por debajo de Signal`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cruce de MACD opuesto
  - Reglas de gestión del riesgo (stop-loss, stop de seguimiento, take-profit)
- **Stops**: Stop-loss, take-profit, stop de seguimiento (opcional)
- **Valores predeterminados**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLoss` = 50 puntos
  - `TakeProfit` = 999 puntos
  - `TrailingStop` = 0 puntos (desactivado)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Stop-loss / Take-profit / Trailing
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
