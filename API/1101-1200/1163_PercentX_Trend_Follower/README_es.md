# Seguidor de Tendencia PercentX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia derivada de PercentX Trend Follower de Trendoscope.

La estrategia normaliza la distancia del precio desde una banda seleccionada (Keltner o Bollinger) y opera cuando este oscilador cruza rangos extremos dinámicos. El ATR se utiliza para la colocación de stops.

## Detalles

- **Criterios de entrada**: El oscilador cruza por encima del rango superior para largo, por debajo del rango inferior para corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop basado en ATR.
- **Stops**: Stop inicial por ATR.
- **Valores predeterminados**:
  - `BandType` = Keltner
  - `MaLength` = 40
  - `LoopbackPeriod` = 80
  - `OuterLoopback` = 80
  - `UseInitialStop` = true
  - `AtrLength` = 14
  - `TrendMultiplier` = 1
  - `ReverseMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: BollingerBands, KeltnerChannels, ATR, Highest, Lowest
  - Stops: ATR
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
