# Estrategia MA2CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el cruce de una Media Móvil Simple (SMA) rápida y lenta con el Commodity Channel Index (CCI) como filtro de confirmación. Una posición se abre solo cuando tanto las medias móviles como el CCI cruzan sus niveles en la misma dirección. El Average True Range (ATR) define la distancia inicial del stop-loss.

El sistema puede operar en ambas direcciones. No hay take-profit; las posiciones se cierran en una señal opuesta o cuando se activa el stop-loss basado en ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: SMA rápida cruza por encima de la SMA lenta **y** CCI cruza por encima de 0.
  - **Corto**: SMA rápida cruza por debajo de la SMA lenta **y** CCI cruza por debajo de 0.
- **Criterios de salida**:
  - Cruce inverso de SMA.
  - Stop-loss basado en ATR.
- **Indicadores**: SMA, CCI, ATR.
- **Marco temporal**: Configurable mediante `CandleType`.
- **Parámetros predeterminados**:
  - `Fast MA Period` = 4
  - `Slow MA Period` = 8
  - `CCI Period` = 4
  - `ATR Period` = 4
- **Largo/Corto**: Ambos.
- **Stops**: Sí, stop dinámico usando ATR.
