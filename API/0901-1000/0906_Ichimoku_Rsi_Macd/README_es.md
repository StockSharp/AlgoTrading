# Estrategia Ichimoku RSI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que combina la Nube Ichimoku, RSI y señales de cruce del MACD.

## Detalles

- **Criterios de entrada**: Precio por encima/debajo de la nube Ichimoku con filtro RSI y cruce de la línea MACD sobre la línea de señal.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce MACD opuesto.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Ichimoku, RSI, MACD
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía (1h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
