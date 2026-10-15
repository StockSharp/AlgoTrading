# Estrategia de MA Trend 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Convertida del asesor experto de MetaTrader 5 `MA Trend 2.mq5`.
- Usa una media móvil configurable para detectar si el precio opera por encima o por debajo de la media desplazada.
- Las posiciones se gestionan con stop-loss, take-profit, trailing stop y características de gestión de dinero opcionales.

## Lógica de la estrategia
1. Suscribirse a la serie de velas seleccionada por el usuario y calcular la media móvil con el método, período, desplazamiento y fuente de precio elegidos.
2. En cada vela completada, almacenar el último valor de la media móvil para que una muestra desplazada (barra anterior más `MaShift`) pueda compararse con el precio de cierre actual.
3. Generar señales de compra cuando el precio cruza por encima de la media de referencia y el filtro de dirección permite operaciones largas. Generar señales de venta para la condición opuesta. Cuando `ReverseSignals` está habilitado, estas reglas se invierten.
4. Antes de entrar en una operación, verificar las banderas `OnlyOnePosition` y `CloseOppositePositions`. La estrategia puede omitir entradas cuando existe la exposición opuesta o cerrarla en la misma orden para dar vuelta la posición.
5. El dimensionamiento de posición usa ya sea un volumen fijo o un modelo de porcentaje de riesgo derivado del EA original. El modo porcentual estima el volumen requerido para que la pérdida a la distancia de stop configurada coincida con el presupuesto de riesgo.
6. Un trailing stop replica la lógica de pasos original: una vez que el beneficio supera `TrailingStopPips + TrailingStepPips`, mueve el stop en pasos sin aflojarlo nunca. Si el precio cruza el trailing stop, la posición se cierra al mercado.
7. Las protecciones opcionales de stop-loss y take-profit se adjuntan a través del ayudante de alto nivel `StartProtection` para que el modelo del corredor pueda cerrar posiciones entre actualizaciones de velas.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `StopLossPips` | Distancia del stop-loss en pips. Establecer en `0` para deshabilitar. | `50` |
| `TakeProfitPips` | Distancia del take-profit en pips. Establecer en `0` para deshabilitar. | `140` |
| `TrailingStopPips` | Distancia base para el trailing stop en pips. | `15` |
| `TrailingStepPips` | Ganancia mínima adicional antes de que el trailing stop se ajuste. | `5` |
| `LotMode` | `FixedVolume` usa `LotOrRiskValue` directamente. `RiskPercent` lo interpreta como porcentaje de riesgo de cuenta. | `RiskPercent` |
| `LotOrRiskValue` | Tamaño de orden fijo o porcentaje de riesgo dependiendo de `LotMode`. | `3` |
| `MaPeriod` | Período de la media móvil. | `12` |
| `MaShift` | Número de velas completadas entre la barra actual y la muestra de la media móvil usada para señales. | `3` |
| `MaMethod` | Método de media móvil (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `LinearWeighted` |
| `MaPrice` | Precio de vela utilizado por la media móvil (cierre, apertura, ponderado, etc.). | `Weighted` |
| `CandleType` | Tipo de datos de vela suscrito por la estrategia. | `1 minute time frame` |
| `Direction` | Dirección permitida (`BuyOnly`, `SellOnly`, `Both`). | `Both` |
| `OnlyOnePosition` | Permitir solo una posición abierta. | `false` |
| `ReverseSignals` | Invertir la lógica de compra/venta. | `false` |
| `CloseOppositePositions` | Cerrar exposición opuesta antes de abrir una nueva operación. | `false` |

## Gestión de dinero
- Cuando `LotMode = RiskPercent`, la estrategia convierte la distancia del stop-loss (en pips) a unidades de precio usando metadatos del valor (`PriceStep`, `StepPrice`).
- El riesgo se calcula desde el valor del portafolio (`CurrentValue` con un fallback a `BeginValue`).
- El volumen solicitado se redondea al `VolumeStep` más cercano para evitar rechazos del intercambio.

## Trailing stop
- La distancia y el paso del trailing se expresan en pips; el código deriva la distancia de precio real usando el tamaño de pip del instrumento.
- Las posiciones largas mueven el stop hacia arriba una vez que el cierre supera la entrada por al menos `TrailingStopPips + TrailingStepPips`. El stop permanece fijo si la ganancia retrocede.
- Las posiciones cortas reflejan la misma lógica con verificaciones de precio simétricas.

## Notas de conversión
- Todas las acciones de trading usan la API de alto nivel de `Strategy` (`BuyMarket`, `SellMarket`, `StartProtection`).
- La estrategia mantiene solo un historial corto de media móvil (desplazamiento + búfer) para replicar la referencia de barra anterior sin almacenar grandes conjuntos de datos.
- Los comentarios se proporcionan en inglés para documentar cada bloque principal de lógica.
