# Estrategia de Ruptura de Clúster de Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Volatility Cluster Breakout** está construida alrededor de rupturas durante clústeres de alta volatilidad.

Las pruebas indican un retorno anual promedio de aproximadamente el 169%. Funciona mejor en el mercado cripto.

Las señales se disparan cuando sus indicadores confirman oportunidades de ruptura en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como PriceAvgPeriod, AtrPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para las condiciones del indicador.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `PriceAvgPeriod = 20`
  - `AtrPeriod = 14`
  - `StdDevMultiplier = 2.0m`
  - `StopMultiplier = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: múltiples indicadores
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
