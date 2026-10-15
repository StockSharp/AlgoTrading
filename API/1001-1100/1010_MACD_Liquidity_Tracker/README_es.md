# Estrategia de Seguimiento de Liquidez MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

MACD Liquidity Tracker utiliza los estados de color del MACD para generar señales de trading. Cuatro modos (Fast, Normal, Safe, Crossover) ajustan la sensibilidad de las señales. Se admiten stop loss y take profit opcionales.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Depende de `SystemType` (por defecto `Normal` usa MACD por encima de la señal).
  - **Corto**: Depende de `SystemType` (por defecto `Normal` usa MACD por debajo de la señal).
- **Criterios de salida**: Señal opuesta.
- **Stops**: Stop loss y take profit opcionales.
- **Valores predeterminados**:
  - `FastLength` = 25
  - `SlowLength` = 60
  - `SignalLength` = 220
  - `AllowShortTrades` = false
  - `SystemType` = Normal
  - `UseStopLoss` = false
  - `StopLossPercent` = 3
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 6
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = tf(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo/Corto
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
