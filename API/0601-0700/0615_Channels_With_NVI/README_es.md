# Estrategia de Canales con NVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza Bandas de Bollinger o Canales de Keltner combinados con el Índice de Volumen Negativo (NVI). Se abre una posición larga cuando el precio cierra por debajo de la banda inferior mientras el NVI está por encima de su EMA. La posición se cierra cuando el NVI cae por debajo de su EMA. Están disponibles porcentajes opcionales de stop-loss y take-profit.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Cierre < banda inferior y NVI > EMA del NVI.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - **Largo**: NVI < EMA del NVI.
- **Stops**: Opcional, porcentaje del precio de entrada.
- **Valores predeterminados**:
  - `ChannelType` = "BB"
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
  - `NviEmaLength` = 200
  - `EnableStopLoss` = false
  - `StopLossPercent` = 0
  - `EnableTakeProfit` = false
  - `TakeProfitPercent` = 0
- **Filtros**:
  - Categoría: Canal
  - Dirección: Solo largos
  - Indicadores: Bollinger Bands o Keltner Channels, EMA, NVI
  - Stops: Opcional
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
