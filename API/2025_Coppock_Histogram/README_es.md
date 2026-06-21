# Estrategia de Histograma Coppock
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera reversiones del Coppock Histogram. El indicador suma dos valores de Rate of Change y suaviza el resultado con una media móvil. Cuando el momentum gira hacia arriba, la estrategia abre posiciones largas y cierra las cortas. Un giro hacia abajo cierra las largas y entra en cortas. Las señales se evalúan únicamente en velas completadas.

## Detalles

- **Criterios de entrada**: El histograma Coppock con pendiente ascendente para compras o descendente para ventas.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: La señal opuesta cierra las posiciones abiertas.
- **Stops**: Sin stop-loss ni take-profit explícitos.
- **Valores predeterminados**:
  - `Roc1Period` = 14
  - `Roc2Period` = 11
  - `SmoothPeriod` = 3
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromHours(8)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RateOfChange, SimpleMovingAverage
  - Stops: Ninguno
  - Complejidad: Básico
  - Marco temporal: 8H
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
