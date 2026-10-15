# Estrategia de agotamiento de la envolvente MA de Firebird
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el experto en reversión de sobres de Firebird v0.60. Mide una media móvil simple y la compensa en un porcentaje para formar las envolventes superior e inferior. Cuando el precio atraviesa la banda superior, la estrategia vende, y cuando la banda inferior se rompe, compra. Las posiciones adicionales se promedian solo si el precio se mueve al menos un pip configurable más allá de la entrada anterior. El stop loss total se comparte entre todas las entradas, lo que evita que las tendencias desbocadas vuelvan a entrar repetidamente en la misma dirección.

## Detalles

- **Criterios de entrada**:
  - Calcule un SMA en la apertura de cualquiera de las velas o en el punto medio alto/bajo.
  - Sobre superior = SMA × (1 + Porcentaje/100); sobre inferior = SMA × (1 − Porcentaje/100).
  - Ingrese en corto en un cierre por encima de la banda superior (a menos que una parada reciente bloquee los cortos), ingrese en largo en un cierre por debajo de la banda inferior (a menos que los largos estén bloqueados).
  - Se permiten operaciones de entrada promedio una vez que el precio se mueve `PipStep` pips (opcionalmente escalado por potencia) más allá del último llenado.
- **Largo/Corto**: Largo y corto.
- **Criterios de salida**:
  - Toma de ganancias compartida al precio de entrada promedio ± `TakeProfit` pips.
  - Stop loss compartido al precio de entrada promedio ∓ `StopLoss / position count` pips.
  - La bandera de bloqueo impide el reingreso en la misma dirección hasta que se activa una señal opuesta después de una parada.
- **Paradas**: Sí, stop loss agregado y toma de ganancias.
- **Valores predeterminados**:
  - `MaLength` = 10
  - `Percent` = 0,3
  - `TradeOnFriday` = verdadero
  - `UseHighLow` = falso (usar abre)
  - `PipStep` = 30
  - `IncreasementPower` = 0
  - `TakeProfit` = 30
  - `StopLoss` = 200
  - `TradeVolume` = 1
- **Filtros**:
  - Categoría: Reversión media
  - Dirección: Ambos
  - Indicadores: SMA sobres
  - Paradas: Sí
  - Complejidad: Media
  - Plazo: Cualquiera
  - Estacionalidad: filtro de viernes opcional
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto debido al promedio
