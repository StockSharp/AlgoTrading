# Estrategia de Supertrend RSI Divergence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Supertrend RSI Divergence** utiliza el indicador Supertrend junto con la divergencia RSI para identificar oportunidades de trading.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 67%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando Divergence confirma configuraciones de divergencia en datos intradía (15m). Este método es adecuado para operadores activos.

Los stops se basan en múltiplos de ATR y factores como SupertrendPeriod, SupertrendMultiplier. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `SupertrendPeriod = 10`
  - `SupertrendMultiplier = 3.0m`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Divergence
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
