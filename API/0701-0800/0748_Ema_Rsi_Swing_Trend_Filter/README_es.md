# Filtro de Tendencia EMA RSI Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera el cruce de EMA20 y EMA50 en la dirección del filtro de tendencia EMA200.
Un filtro RSI opcional limita las entradas largas cuando el RSI está sobrecomprado y los cortos cuando está sobrevendido.

## Detalles

- **Criterios de entrada**: EMA20 cruza EMA50 con el precio relativo a EMA200 y filtro RSI opcional.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Salida opcional en cruce EMA opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `EmaFastPeriod` = 20
  - `EmaSlowPeriod` = 50
  - `EmaTrendPeriod` = 200
  - `RsiLength` = 14
  - `UseRsiFilter` = true
  - `RsiMaxLong` = 70
  - `RsiMinShort` = 30
  - `RequireCloseConfirm` = true
  - `ExitOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
