# Estrategia Ima Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera en función de la velocidad relativa del precio respecto a su media móvil.
El ratio `Close / SMA - 1` se compara entre dos velas consecutivas. Un fuerte incremento abre una posición larga, mientras que una fuerte caída abre una posición corta.

## Detalles

- **Criterios de entrada**:
  - Largo: `(IMA_now - IMA_prev) / abs(IMA_prev) >= SignalLevel`
  - Corto: `(IMA_now - IMA_prev) / abs(IMA_prev) <= -SignalLevel`
- **Criterios de salida**: Señal opuesta
- **Tamaño de posición**: `RiskLevel` y `StopLossTicks` definen el volumen de la operación, limitado por `MaxVolume`
- **Largo/Corto**: Ambos
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `SmaPeriod` = 5
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 1000
  - `SignalLevel` = 0.5
  - `RiskLevel` = 0.01
  - `MaxVolume` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
