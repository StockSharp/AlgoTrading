# Estrategia Bober XM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Bober XM utiliza un enfoque de doble canal basado en un cálculo personalizado de Keltner. Las entradas por ruptura son confirmadas por una Media Móvil Ponderada y la fuerza general de la tendencia mediante ADX. Las salidas dependen del On-Balance Volume cruzando su media móvil mientras el ADX permanece fuerte.

Diseñada para traders que buscan confirmación de momentum con salidas basadas en volumen.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close > UpperBand && Close > WMA && ADX > Threshold`
  - **Corto**: `Close < LowerBand && Close < WMA && ADX > Threshold`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - **Largo**: `OBV < OBV_MA && ADX > Threshold`
  - **Corto**: `OBV > OBV_MA && ADX > Threshold`
- **Stops**: Stop-loss porcentual mediante `StopLossPercent`
- **Valores predeterminados**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `KeltnerMultiplier` = 1.5m
  - `WmaPeriod` = 15
  - `ObvMaPeriod` = 22
  - `AdxPeriod` = 60
  - `AdxThreshold` = 35m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Keltner Channel, WMA, OBV, ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
