# Simple MA ADX EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina una EMA con el Índice de Movimiento Direccional Promedio para confirmar la fuerza de la tendencia.

Compra cuando la EMA está subiendo, el cierre anterior está por encima de la EMA, ADX supera un umbral y +DI es mayor que -DI. Vende cuando aparecen las condiciones opuestas. Los niveles de stop-loss y take-profit gestionan el riesgo.

## Detalles

- **Criterios de entrada**: Dirección de la EMA, precio vs EMA, ADX, +DI/-DI.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta u órdenes de protección.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AdxPeriod` = 8
  - `MaPeriod` = 8
  - `AdxThreshold` = 22m
  - `StopLoss` = 30m
  - `TakeProfit` = 100m
  - `Volume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, ADX
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
