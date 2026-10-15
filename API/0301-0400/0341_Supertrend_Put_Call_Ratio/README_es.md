# Estrategia Supertrend de Relación Put/Call
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia **Supertrend Put Call Ratio** está construida en torno al Supertrend y la relación put/call.

Las pruebas indican un retorno anual promedio de aproximadamente 112%. Funciona mejor en el mercado de divisas.

Las señales se activan cuando sus indicadores confirman cambios de tendencia en datos intradía (15m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como Period, Multiplier. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `Period = 10`
  - `Multiplier = 3m`
  - `PCRPeriod = 20`
  - `PCRMultiplier = 2m`
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

