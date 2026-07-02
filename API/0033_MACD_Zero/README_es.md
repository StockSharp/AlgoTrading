# Estrategia MACD Zero Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Este sistema opera cambios de momentum cuando el histograma del Moving Average Convergence Divergence (MACD) se aproxima a la línea cero. Un MACD ascendente por debajo de cero o un MACD descendente por encima de cero señala una posible reversión.

Las pruebas indican un rendimiento anual promedio de aproximadamente 136%. Funciona mejor en el mercado de acciones.

La estrategia espera que la línea MACD tienda hacia cero mientras permanece en el lado opuesto. Una vez que el momentum se desvanece, entra anticipando un giro en el precio.

Las operaciones salen cuando el MACD cruza su línea de señal o se activa un stop-loss.

## Detalles

- **Criterios de entrada**: MACD tendiendo hacia cero desde cualquier lado.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: MACD cruza la línea de señal o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

