# PZ Parabolic SAR EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto *PZ Parabolic SAR*. Emplea dos indicadores Parabolic SAR con diferentes configuraciones de paso y aceleración máxima. El SAR de "operación" detecta la dirección de la tendencia para las entradas, mientras que el SAR de "stop" sigue el precio más de cerca y activa las salidas cuando la tendencia se revierte.

El control del riesgo se gestiona mediante el Average True Range (ATR). Se establece un stop inicial basado en ATR cuando se abre una posición. Opcionalmente, un trailing stop basado en ATR puede ajustar el stop a medida que el precio se mueve a favor de la operación. La estrategia también admite cierre parcial: una vez que el beneficio supera la distancia del stop inicial, se cierra la mitad de la posición y el stop se mueve a break-even.

La estrategia opera en dirección larga y corta y solo funciona con velas completadas. Utiliza órdenes de mercado sin colocar órdenes de stop reales.

## Detalles

- **Criterios de entrada**: Precio por encima/debajo del SAR de operación y del SAR de stop en la misma dirección.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: SAR de stop cruzando el precio o trailing stop ATR alcanzado.
- **Stops**: Stop basado en ATR con trailing y break-even opcionales.
- **Valores predeterminados**:
  - `TradeStep` = 0.002
  - `TradeMax` = 0.2
  - `StopStep` = 0.004
  - `StopMax` = 0.4
  - `AtrPeriod` = 30
  - `AtrMultiplier` = 2.5
  - `UseTrailing` = false
  - `TrailingAtrPeriod` = 30
  - `TrailingAtrMultiplier` = 1.75
  - `PartialClosing` = true
  - `PercentageToClose` = 0.5
  - `BreakEven` = true
  - `LotSize` = 0.1
  - `CandleType` = TimeFrame(5m)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR, ATR
  - Stops: ATR, Trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
