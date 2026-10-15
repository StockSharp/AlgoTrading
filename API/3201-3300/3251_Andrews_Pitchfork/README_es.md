# Estrategia de Andrews Pitchfork
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Puerto del asesor experto MetaTrader "Andrew's Pitchfork". El script original esperaba un objeto Andrews Pitchfork dibujado manualmente y lo combinaba con filtros de Momentum, medias móviles de múltiples temporalidades y MACD. La versión de StockSharp mantiene el conjunto de indicadores, reemplaza el dibujo manual con detección automática de tendencia y recrea la lógica de protección (límites de múltiples entradas, stop-loss, take-profit, punto de equilibrio y gestión de trailing).

## Lógica de la estrategia

1. **Indicadores**
   - Dos *Medias Móviles Ponderadas Linealmente* (LWMA) calculadas sobre el precio típico de la serie de velas seleccionada.
   - Un oscilador *Momentum* en la misma temporalidad, evaluado por la desviación absoluta del nivel de equilibrio 100.
   - Un par de líneas de señal *MACD (12, 26, 9)* clásico.
2. **Reglas de entrada**
   - Los trades **largos** requieren que la LWMA rápida esté por encima de la LWMA lenta, que al menos una de las últimas tres desviaciones de Momentum supere el `MomentumBuyThreshold`, y que la línea MACD esté por encima de su línea de señal.
   - Los trades **cortos** invierten estas condiciones.
   - La estrategia hace piramidación añadiendo repetidamente el `Volume` base mientras la posición absoluta esté por debajo de `Volume * MaxPyramids`. Las señales opuestas cierran la exposición actual antes de abrir la nueva dirección.
3. **Gestión de riesgos**
   - Los niveles iniciales de stop-loss y take-profit se colocan en pasos de precio alrededor de la entrada. Ambos se actualizan cuando cambia el tamaño de la posición.
   - La lógica de punto de equilibrio mueve el stop después de que el precio haya recorrido un número configurable de pasos a favor de la posición.
   - La lógica del stop de seguimiento sigue el precio más rentable con una distancia de margen adicional.

En comparación con la versión MQL, el puerto de StockSharp infiere automáticamente la tendencia usando la pendiente LWMA en lugar de verificar la orientación de un objeto Pitchfork dibujado por el usuario. Todos los demás filtros (Momentum, MACD, límite de órdenes múltiples) y herramientas de gestión monetaria fueron reproducidos con la API de alto nivel de StockSharp.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
|------|------|---------|-------------|
| `CandleType` | `DataType` | Marco temporal de 15 minutos | Serie de velas principal utilizada por todos los indicadores. |
| `FastMaPeriod` | `int` | 6 | Longitud de la LWMA rápida sobre el precio típico. |
| `SlowMaPeriod` | `int` | 85 | Longitud de la LWMA lenta sobre el precio típico. |
| `MomentumPeriod` | `int` | 14 | Retrospectiva del indicador Momentum. |
| `MomentumBuyThreshold` | `decimal` | 0.3 | Mínimo \|Momentum - 100\| para entradas largas. |
| `MomentumSellThreshold` | `decimal` | 0.3 | Mínimo \|Momentum - 100\| para entradas cortas. |
| `MaxPyramids` | `int` | 1 | Número máximo de lotes base permitidos en la misma dirección. |
| `StopLossSteps` | `int` | 20 | Distancia del stop-loss expresada en pasos de precio. |
| `TakeProfitSteps` | `int` | 50 | Distancia del take-profit expresada en pasos de precio. |
| `EnableTrailing` | `bool` | `true` | Habilita el stop de seguimiento dinámico. |
| `TrailingTriggerSteps` | `int` | 40 | Beneficio en pasos requerido antes de que se active el stop de seguimiento. |
| `TrailingDistanceSteps` | `int` | 40 | Distancia en pasos mantenida entre el extremo de precio y el stop de seguimiento. |
| `TrailingPadSteps` | `int` | 10 | Margen extra aplicado al stop de seguimiento. |
| `EnableBreakEven` | `bool` | `true` | Habilita el ajuste del stop al punto de equilibrio. |
| `BreakEvenTriggerSteps` | `int` | 30 | Beneficio en pasos necesario antes de mover el stop al punto de equilibrio. |
| `BreakEvenOffsetSteps` | `int` | 30 | Offset en pasos más allá de la entrada cuando se aplica el punto de equilibrio. |

## Notas

- La estrategia requiere un `PriceStep` válido del valor seleccionado para convertir distancias basadas en pasos en precios. Si falta el paso, la lógica de trailing y punto de equilibrio permanece inactiva.
- Las órdenes protectoras (stop y take-profit) se recrean cuando cambia el tamaño de la posición, asegurando que el escalado o la reversión alinee las órdenes con la nueva exposición.
- Los parámetros predeterminados coinciden con la configuración original del EA pero pueden optimizarse mediante los rangos de `StrategyParam` integrados.
