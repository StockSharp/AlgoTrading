# Estrategia Heikin-Ashi Optimizada con Opciones de Compra/Venta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Las velas Heikin-Ashi suavizan los datos de precios y destacan la dirección de la tendencia. Esta estrategia opera en una sola dirección a la vez: ya sea largos en velas verdes o cortos en velas rojas dentro de un rango de fechas definido por el usuario. Niveles opcionales de stop-loss y take-profit proporcionan control del riesgo.

## Detalles

- **Criterios de entrada**: Cambio de color de la vela Heikin-Ashi.
- **Largo/Corto**: Configurable.
- **Criterios de salida**: Señal opuesta o niveles de stop.
- **Stops**: Opcional, basado en porcentaje.
- **Valores predeterminados**:
  - `CandleType` = 1 day
  - `StartDate` = 2023-01-01
  - `EndDate` = 2024-01-01
  - `TradeType` = BuyOnly
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Configurable
  - Indicadores: Heikin-Ashi
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: Rango de fechas
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

