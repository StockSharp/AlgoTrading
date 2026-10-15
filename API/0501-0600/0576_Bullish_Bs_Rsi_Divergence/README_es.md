# Divergencia RSI Alcista de B's
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza RSI para detectar divergencias alcistas regulares y ocultas con puntos pivote. Abre operaciones largas en la divergencia y las cierra en señales bajistas, objetivo de RSI o stop trailing.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Divergencia RSI alcista regular u oculta.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Divergencia bajista, RSI cruzando por encima del objetivo o stop trailing.
- **Stops**: Stop trailing opcional basado en ATR o porcentaje.
- **Valores predeterminados**:
  - `RsiPeriod` = 9
  - `PivotLookbackRight` = 3
  - `PivotLookbackLeft` = 1
  - `TakeProfitRsiLevel` = 80
  - `RangeUpper` = 60
  - `RangeLower` = 5
  - `StopType` = None
  - `StopLoss` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 3.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Divergencia
  - Dirección: Largo
  - Indicadores: RSI, ATR
  - Stops: Stop trailing opcional
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
