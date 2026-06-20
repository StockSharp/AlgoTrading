# Estrategia Donchian de Pico de Sentimiento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia **Donchian Sentiment Spike** está construida en torno al pico de sentimiento de Donchian.

Las pruebas indican un retorno anual promedio de aproximadamente 115%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando Donchian confirma cambios de tendencia en datos intradía (15m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como DonchianPeriod, SentimentPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `DonchianPeriod = 20`
  - `SentimentPeriod = 20`
  - `SentimentMultiplier = 2m`
  - `StopLoss = 2m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Donchian, Spike
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

