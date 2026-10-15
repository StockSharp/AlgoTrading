# Estrategia Geedo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el tiempo que compara los precios de apertura de dos barras pasadas en una hora específica. Si la barra más antigua está por encima de la reciente por un umbral, se abre una operación corta. Si la barra reciente está por encima de la más antigua, se abre una operación larga. Cada posición usa stop loss y take profit fijos y se cierra después de un tiempo máximo de mantenimiento.

## Detalles

- **Criterios de entrada**: A `TradeTime` comparar precios de apertura de `T1` y `T2` barras atrás. Si `Open[T1] - Open[T2]` supera `DeltaShort`, vender; si `Open[T2] - Open[T1]` supera `DeltaLong`, comprar.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss, take profit o `MaxOpenTime` horas tras la entrada.
- **Stops**: Stop loss y take profit fijos en puntos.
- **Valores predeterminados**:
  - `TakeProfitLong` = 39
  - `StopLossLong` = 147
  - `TakeProfitShort` = 15
  - `StopLossShort` = 6000
  - `TradeTime` = 18
  - `T1` = 6
  - `T2` = 2
  - `DeltaLong` = 6
  - `DeltaShort` = 21
  - `Volume` = 0.01
  - `MaxOpenTime` = 504
- **Filtros**:
  - Categoría: Basada en tiempo
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Fijo
  - Complejidad: Principiante
  - Marco temporal: Intradía (1h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
