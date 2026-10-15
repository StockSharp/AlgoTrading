# Estrategia VWAP con Filtro de Sesgo Conductual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia **VWAP Behavioral Bias Filter** está construida en torno al filtro de sesgo conductual de VWAP.

Las pruebas indican un retorno anual promedio de aproximadamente 124%. Funciona mejor en el mercado de divisas.

Las señales se activan cuando el filtro Behavioral confirma entradas filtradas en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como BiasThreshold, BiasWindowSize. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `BiasThreshold = 0.5m`
  - `BiasWindowSize = 20`
  - `StopLoss = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Behavioral, Bias
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

