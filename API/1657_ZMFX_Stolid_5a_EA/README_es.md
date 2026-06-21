# Estrategia ZMFX Stolid 5a EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia multi-temporalidad que entra en retrocesos confirmados por lecturas de RSI y Stochastic.
El sistema identifica la tendencia principal a partir del Stochastic de 4 horas y medias móviles suavizadas de 1 hora.
Las posiciones se abren en reversiones de vela con condiciones de RSI en sobrecompra/sobreventa y se cierran con señales opuestas.

## Detalles

- **Criterios de entrada**:
  - Largo: `UpTrend && PreviousBarDown && PrevRSI < 30 && (RSI15 < 30 => double volume)`
  - Corto: `DownTrend && PreviousBarUp && PrevRSI > 70 && (RSI15 > 70 => double volume)`
- **Largo/Corto**: Ambos
- **Stops**: Sin stops explícitos; posiciones cerradas por condiciones de indicadores
- **Valores predeterminados**:
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: RSI, Stochastic, Smoothed Moving Average
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Multi-temporalidad
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
