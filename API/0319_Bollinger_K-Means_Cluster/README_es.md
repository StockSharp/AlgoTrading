# Estrategia de Bollinger K-Means Cluster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Bollinger K-Means Cluster** se basa en el Bollinger K-Means Cluster.

Las señales se activan cuando Bollinger confirma cambios de tendencia en datos intradía (5m). Este método es adecuado para operadores activos.

Los stops se basan en múltiplos de ATR y factores como BollingerLength, BollingerDeviation. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `BollingerLength = 20`
  - `BollingerDeviation = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `KMeansHistoryLength = 50`
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
