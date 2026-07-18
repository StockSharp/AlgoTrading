# Estrategia Fibo iSAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina indicadores Parabolic SAR rápido y lento con niveles de retroceso de Fibonacci. Cuando el SAR rápido se sitúa por encima del SAR lento y por debajo del precio, la estrategia anticipa una tendencia alcista y coloca una orden Buy Limit en el retroceso del 50% de Fibonacci del rango reciente. El stop loss se coloca por debajo del mínimo del swing y el take profit en la extensión del 161%. Para una tendencia bajista, la lógica se refleja con órdenes Sell Limit.

## Detalles

- **Criterios de entrada**: Dirección de tendencia por SAR rápido/lento; entrada en el retroceso de Fibonacci del 50%.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Niveles de stop loss o take profit.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `StepFast` = 0.02
  - `MaximumFast` = 0.2
  - `StepSlow` = 0.01
  - `MaximumSlow` = 0.1
  - `CountBarSearch` = 3
  - `IndentStopLoss` = 30
  - `FiboEntranceLevel` = 50
  - `FiboProfitLevel` = 161
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR, Fibonacci
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
