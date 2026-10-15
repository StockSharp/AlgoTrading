# Estrategia MACD con Filtro de Sentimiento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia **MACD Sentiment Filter** se basa en MACD con filtro de sentimiento.

Las señales se activan cuando los indicadores confirman entradas filtradas en datos intradía (15m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como MacdFast, MacdSlow. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: consulte la implementación para conocer las condiciones de los indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, utilizando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `MacdFast = 12`
  - `MacdSlow = 26`
  - `MacdSignal = 9`
  - `Threshold = 0.5m`
  - `StopLoss = 2m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples indicadores
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
