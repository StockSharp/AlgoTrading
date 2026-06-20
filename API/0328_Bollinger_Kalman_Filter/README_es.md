# Estrategia de Bollinger Kalman Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Bollinger Kalman Filter** se basa en el Bollinger Kalman Filter.

Las señales se activan cuando Bollinger confirma entradas filtradas en datos intradía (5m). Este método es adecuado para operadores activos.

Los stops se basan en múltiplos de ATR y factores como BollingerLength, BollingerDeviation. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `BollingerLength = 20`
  - `BollingerDeviation = 2.0m`
  - `KalmanQ = 0.01m`
  - `KalmanR = 0.1m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
