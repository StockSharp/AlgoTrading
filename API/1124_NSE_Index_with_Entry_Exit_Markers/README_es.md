# Estrategia de Índice NSE con Marcadores de Entrada y Salida
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre largos cuando el precio está por encima de una SMA de tendencia y el RSI cruza al alza por encima del nivel de sobreventa. Un stop loss y take profit basados en ATR gestionan la posición.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio está por encima de la SMA y el RSI cruza hacia arriba por encima del nivel de sobreventa.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - cerrar la posición larga cuando el precio alcanza el stop basado en ATR o el take profit.
- **Stops**: Stop loss y take profit basados en ATR.
- **Valores predeterminados**:
  - `SmaPeriod` = 200.
  - `RsiPeriod` = 14.
  - `RsiOversold` = 40.
  - `AtrPeriod` = 14.
  - `AtrMultiplier` = 1.5.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: SMA, RSI, ATR
  - Stops: Basado en ATR
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
