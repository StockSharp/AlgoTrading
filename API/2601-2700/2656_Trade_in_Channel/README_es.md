# Estrategia de Operación en Canal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia contraria de canal que desvanece los extremos del canal Donchian cuando el ancho de banda permanece sin cambios. El sistema compara el último máximo/mínimo contra los límites previos del canal y un pivote calculado desde el cierre anterior para decidir si desvanece el movimiento. Los stops protectores dependen de la distancia ATR y un trailing stop opcional mantiene las ganancias una vez que el precio avanza a favor de la posición.

## Detalles

- **Criterios de entrada**:
  - Corto: banda superior del canal sin cambios y el último máximo de la vela tocó la banda superior o el cierre previo está entre el pivote y la banda superior.
  - Largo: banda inferior del canal sin cambios y el último mínimo de la vela tocó la banda inferior o el cierre previo está entre el pivote y la banda inferior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cerrar largo si la banda superior está plana y el precio la toca, o si se activa el stop ATR o el trailing stop.
  - Cerrar corto si la banda inferior está plana y el precio la toca, o si se activa el stop ATR o el trailing stop.
- **Stops**:
  - Stop inicial para largos en `support - ATR` y para cortos en `resistance + ATR`.
  - El trailing stop se mueve detrás del mejor precio una vez que la ganancia supera la distancia `TrailingStopPips` (convertida en pasos de precio).
- **Valores predeterminados**:
  - `ChannelPeriod` = 20 (lookback de Donchian)
  - `AtrPeriod` = 4 (suavizado ATR)
  - `Volume` = 1 contrato/lote
  - `TrailingStopPips` = 30 pasos de precio
  - `CandleType` = marco temporal de 1 hora
- **Filtros**:
  - Categoría: Canal / Reversión a la media
  - Dirección: Largo y Corto
  - Indicadores: Donchian Channel, ATR
  - Stops: Stop fijo ATR + trailing stop
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Notas

- El pivote es igual a `(banda superior + banda inferior + cierre previo) / 3`, coincidiendo con la implementación MQL original.
- La estrategia mantiene solo una posición neta y cambia de dirección solo después de que el trade anterior esté completamente cerrado.
- La distancia del trailing se especifica en pasos de precio ("pips"); se multiplica por el `PriceStep` del instrumento para obtener el offset de precio real.
