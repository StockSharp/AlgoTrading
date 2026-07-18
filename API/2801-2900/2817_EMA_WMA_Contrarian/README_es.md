# Estrategia EMA WMA Contrarian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema contrario de cruce que compara una media móvil exponencial (EMA) y una media móvil ponderada (WMA) construidas sobre precios de apertura de velas. Cuando la EMA rápida cae por debajo de la WMA, la estrategia compra apostando a un retorno. Cuando la EMA sube de nuevo por encima de la WMA, entra en corto. El tamaño de la operación se deriva del porcentaje de riesgo configurado y la distancia al stop protector, mientras que los niveles opcionales de stop-loss, take-profit y trailing stop mantienen la exposición bajo control.

## Detalles

- **Criterios de entrada**:
  - Largo: EMA(Apertura) cruza de arriba hacia abajo de la WMA(Apertura)
  - Corto: EMA(Apertura) cruza de abajo hacia arriba de la WMA(Apertura)
- **Largo/Corto**: Ambas direcciones
- **Criterios de salida**:
  - Stop-loss fijo en pasos de precio
  - Take-profit fijo en pasos de precio
  - Trailing stop que avanza después de que el precio se mueve `TrailingStopPoints + TrailingStepPoints`
  - El cruce opuesto cierra la posición actual y abre la nueva
- **Stops**: Stop-loss, take-profit y trailing stop
- **Valores predeterminados**:
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossPoints` = 50m
  - `TakeProfitPoints` = 50m
  - `TrailingStopPoints` = 50m
  - `TrailingStepPoints` = 10m
  - `RiskPercent` = 10m
  - `BaseVolume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Media Móvil, Contrarian
  - Dirección: Largo y Corto
  - Indicadores: EMA (apertura), WMA (apertura)
  - Stops: Sí (stop fijo, trailing)
  - Complejidad: Intermedio
  - Marco temporal: Intradía (predeterminado 1 minuto)
  - Estacionalidad: Ninguno
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `EmaPeriod`, `WmaPeriod` | Períodos de retrospección para la EMA y WMA calculadas en las aperturas de velas. |
| `StopLossPoints`, `TakeProfitPoints` | Distancia en pasos de precio para colocar el stop-loss protector y el objetivo de ganancia. |
| `TrailingStopPoints` | Distancia entre el precio y el trailing stop una vez activado. |
| `TrailingStepPoints` | Movimiento favorable adicional requerido antes de que el trailing stop suba/baje. Debe ser positivo cuando el trailing está habilitado. |
| `RiskPercent` | Porcentaje del capital del portafolio arriesgado por operación. El tamaño de posición se calcula como `RiskPercent / (StopLossPoints * PriceStep)`. |
| `BaseVolume` | Tamaño mínimo de operación utilizado cuando el dimensionamiento basado en riesgo no se puede determinar. |
| `CandleType` | Tipo de datos de vela para cálculos (predeterminado 1 minuto). |

## Notas

- Ambas medias móviles consumen precios de apertura de velas, reflejando el asesor experto original de MetaTrader.
- Los trailing stops solo se activan después de que el precio se mueve al menos `TrailingStopPoints + TrailingStepPoints` a favor de la operación, replicando la lógica heredada.
- Si `TrailingStopPoints` está configurado mientras `TrailingStepPoints` es cero o negativo, la estrategia se detiene inmediatamente para evitar un comportamiento de trailing inconsistente.
- El dimensionamiento basado en riesgo recurre a `BaseVolume` si el valor del portafolio, el paso de precio o la distancia del stop no están disponibles.
