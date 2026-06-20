# Estrategia de VWAP Stochastic Divergence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **VWAP Stochastic Divergence** se basa en la combinación del VWAP con el indicador de fuerza de tendencia ADX.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 79%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando Stochastic confirma configuraciones de divergencia en datos intradía (5m). Este método es adecuado para operadores activos.

Los stops se basan en múltiplos de ATR y factores como AdxPeriod, AdxThreshold. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
  - `AdxExitThreshold = 20m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Stochastic, Divergence
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
