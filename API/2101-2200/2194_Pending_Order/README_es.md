# Estrategia de Orden Pendiente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que coloca cuatro órdenes pendientes alrededor del bid y ask actuales durante horas especificadas. Mantiene continuamente órdenes buy limit, sell limit, buy stop y sell stop a una distancia configurable del precio de mercado. Cada orden pendiente utiliza offsets fijos de stop-loss y take-profit.

## Detalles

- **Criterios de entrada**: Colocar órdenes pendientes a `Distance` ticks del bid/ask actual dentro de las horas permitidas.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Take-profit o stop-loss relativo al precio de entrada.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `StartHour` = 6
  - `EndHour` = 20
  - `TakeProfit` = 20
  - `StopLoss` = 100
  - `Distance` = 15
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Rango
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
