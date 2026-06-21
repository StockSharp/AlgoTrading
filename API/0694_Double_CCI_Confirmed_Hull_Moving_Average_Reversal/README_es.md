# Estrategia de Reversión con Hull MA Confirmada por Doble CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando el precio cruza por encima de la Hull Moving Average con confirmación de los indicadores CCI rápido y lento. Una EMA de seguimiento gestiona el beneficio tras una activación basada en ATR.

Las pruebas muestran un retorno anual moderado. Funciona mejor en mercados mixtos.

## Detalles
- **Criterios de entrada**:
  - **Largo**: El precio cruza por encima del HMA, cierre por encima del HMA, CCI rápido > 0, CCI lento > 0
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - **Largo**: Cierre por debajo de la EMA de seguimiento tras activación o mínimo toca el stop ATR
- **Stops**: Sí.
- **Valores predeterminados**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `HullMaLength` = 34
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Solo largos
  - Indicadores: CCI, HMA, EMA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
