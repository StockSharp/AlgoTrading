# Estrategia de Envolventes de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia basada en el indicador TrendEnvelopes. Combina una EMA con bandas basadas en ATR para detectar rupturas.
Las posiciones largas se abren cuando el precio rompe por encima de la banda superior y aparece una señal de compra. Las posiciones cortas se abren en rupturas por debajo de la banda inferior con una señal de venta. Las bandas opuestas activan el cierre de posiciones.

## Detalles

- **Criterios de entrada**:
  - Largo: el precio cierra por encima del envolvente superior y genera una señal de compra
  - Corto: el precio cierra por debajo del envolvente inferior y genera una señal de venta
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal de tendencia opuesta
- **Stops**: Sí (take profit y stop loss)
- **Valores predeterminados**:
  - `MaPeriod` = 14
  - `Deviation` = 0.2m
  - `AtrPeriod` = 15
  - `AtrSensitivity` = 0.5m
  - `TakeProfit` = 2000 puntos
  - `StopLoss` = 1000 puntos
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: 4h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
