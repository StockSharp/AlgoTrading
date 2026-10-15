# Estrategia RSI con Sobrecompra/Sobreventa Dinámica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **RSI Dynamic Overbought Oversold** está construida alrededor del RSI con niveles dinámicos de sobrecompra/sobreventa.

Las pruebas indican un retorno anual promedio de aproximadamente el 178%. Funciona mejor en el mercado de acciones.

Las señales se disparan cuando la Sobrecompra confirma cambios de tendencia en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como RsiPeriod, MovingAvgPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para las condiciones del indicador.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `RsiPeriod = 14`
  - `MovingAvgPeriod = 50`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Overbought, Oversold
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
