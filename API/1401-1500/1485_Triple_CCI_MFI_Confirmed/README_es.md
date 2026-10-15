# Estrategia Triple CCI MFI Confirmada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando el CCI rápido cruza por encima de cero mientras el CCI medio y el lento permanecen positivos, el precio está por encima de la EMA y el MFI supera 50. El beneficio es seguido por la EMA tras una activación basada en ATR.

Las pruebas muestran un rendimiento moderado; funciona mejor durante mercados en tendencia.

## Detalles
- **Criterios de entrada**:
  - **Largo**: CCI rápido cruza por encima de 0, CCI medio > 0, CCI lento > 0, MFI > 50, cierre por encima de EMA
- **Largo/Corto**: Solo largo.
- **Criterios de salida**:
  - **Largo**: Cierre por debajo de la EMA de seguimiento tras la activación o el mínimo toca el stop ATR
- **Stops**: Sí.
- **Valores predeterminados**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 14
  - `MiddleCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `MfiLength` = 14
  - `EmaLength` = 50
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: CCI, MFI, EMA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
