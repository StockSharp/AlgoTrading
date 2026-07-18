# Estrategia de Ruptura del Rango de Apertura NY - Stop por MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera Rupturas del rango de apertura de Nueva York de 9:30-9:45 con salidas opcionales basadas en media móvil. Las entradas ocurren en la vela siguiente a la ruptura si está dentro del tiempo límite y el precio se alinea con el filtro de media móvil.

## Detalles

- **Criterios de entrada**:
  - La vela anterior cierra más allá del máximo del rango de apertura (largo) o del mínimo (corto) antes del tiempo de corte.
  - La vela actual es la primera tras la ruptura y satisface el filtro de MA cuando está habilitado.
- **Largo/Corto**: Configurable mediante `TradeDirection`.
- **Criterios de salida**:
  - Stop en el lado opuesto del rango de apertura.
  - Take profit según `TakeProfitType`: riesgo-recompensa fijo, cruce de media móvil o ambos.
- **Stops**: Sí, en los límites del rango.
- **Valores predeterminados**:
  - `CutoffHour` = 12
  - `CutoffMinute` = 0
  - `TradeDirection` = LongOnly
  - `TakeProfitType` = FixedRiskReward
  - `TpRatio` = 2.5
  - `MaType` = SMA
  - `MaLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Configurable
  - Indicadores: Moving Average
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
