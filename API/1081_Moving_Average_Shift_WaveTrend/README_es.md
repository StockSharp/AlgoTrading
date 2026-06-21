# Estrategia de Media Móvil Shift WaveTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina una media móvil configurable con un oscilador de estilo WaveTrend. Las operaciones largas ocurren cuando el precio está por encima de la media móvil y el oscilador sube, confirmando una tendencia alcista con una EMA a largo plazo y un filtro de volatilidad. Las posiciones cortas se activan en condiciones opuestas. Las posiciones están protegidas por stop loss, take profit y trailing stop porcentuales.

## Detalles

- **Criterios de entrada**:
  - **Largo**: precio por encima de la MA, oscilador > 0 y subiendo, tendencia a largo plazo alcista, ATR por encima de su media, dentro del horario de trading, no ya en ola.
  - **Corto**: precio por debajo de la MA, oscilador < 0 y bajando, tendencia a largo plazo bajista, ATR por encima de su media, dentro del horario de trading, no ya en ola.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Reversión del oscilador con cruce de precio y MA, o trailing stop, o protecciones.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MaType` = SMA
  - `MaLength` = 40
  - `OscLength` = 15
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 1
  - `TrailPercent` = 1
  - `LongMaLength` = 200
  - `AtrLength` = 14
  - `StartHour` = 9
  - `EndHour` = 17
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA, Hull MA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
