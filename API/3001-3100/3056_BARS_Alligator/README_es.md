# Estrategia BARS Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia BARS Alligator es un port directo del asesor experto de MetaTrader con el mismo nombre. Se basa en el indicador Alligator de Bill Williams para detectar tendencias emergentes: cuando la línea verde de los labios (lips) cruza por encima de la línea azul de la mandíbula (jaw), el sistema lo trata como un rupturo alcista, mientras que un cruce hacia abajo señala impulso bajista. Las salidas dependen de que los labios crucen la línea roja de los dientes (teeth) para que las posiciones se cierren tan pronto como el impulso se desvanece. Las distancias de stop-loss de protección, take-profit y trailing stop se configuran en pips y se convierten automáticamente a unidades de precio según el paso de precio del instrumento y la precisión decimal.

## Lógica de trading

1. **Construcción del indicador**
   - Tres medias móviles con longitudes, desplazamientos y tipo configurables (simple, exponencial, suavizada o ponderada) forman el Alligator.
   - El precio aplicado puede ser el cierre, apertura, máximo, mínimo, mediano, típico o precio ponderado de cada vela.
   - Los desplazamientos se respetan almacenando un pequeño buffer rotativo para cada línea para que las cruces usen los mismos valores que aparecerían en un gráfico de MetaTrader.
2. **Condiciones de entrada**
   - **Largo**: la línea lips en la barra anterior está por encima de la jaw y estaba por debajo dos barras atrás (cruce alcista hacia arriba).
   - **Corto**: la línea lips en la barra anterior está por debajo de la jaw y estaba por encima dos barras atrás (cruce bajista hacia abajo).
   - Las nuevas entradas solo se permiten si la posición actual es plana o ya está alineada con la dirección de la señal y el tamaño agregado de la posición permanece por debajo de `MaxPositions × OrderVolume` (o el equivalente dimensionado por riesgo).
3. **Condiciones de salida**
   - **Salida larga**: la línea lips cruza por debajo de la línea teeth y la posición es rentable respecto al precio de entrada promedio.
   - **Salida corta**: la línea lips cruza por encima de la línea teeth y la posición es rentable.
   - Las salidas también ocurren cuando se superan los niveles estáticos de stop-loss o take-profit.
4. **Stop dinámico**
   - Cuando está habilitado, un trailing stop reposiciona el stop de protección una vez que el precio se mueve más allá de `TrailingStopPips + TrailingStepPips` en la dirección de la operación. El stop entonces sigue al precio a una distancia de `TrailingStopPips` pips pero solo avanza si el precio hace un nuevo progreso de al menos `TrailingStepPips` pips.
5. **Gestión monetaria**
   - Con `MoneyMode = FixedVolume`, las órdenes usan el tamaño de `OrderVolume` directamente.
   - Con `MoneyMode = RiskPercent`, la estrategia asigna volumen de manera que el porcentaje configurado `MoneyValue` del capital del portafolio se perdería si se golpeara el stop-loss. El riesgo por unidad equivale a la distancia del stop-loss expresada en unidades de precio. El resultado se redondea hacia abajo al `VolumeStep` más cercano (o a 1 cuando falta información del step).

## Parámetros

| Parámetro | Tipo | Por defecto | Descripción |
|-----------|------|-------------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Marco temporal usado para cálculos del Alligator. |
| `OrderVolume` | `decimal` | `0.1` | Volumen de operación fijo cuando `MoneyMode` es `FixedVolume`. |
| `MoneyMode` | `MoneyManagementMode` | `FixedVolume` | Elige entre volumen fijo y dimensionamiento por porcentaje de riesgo. |
| `MoneyValue` | `decimal` | `1` | Porcentaje de riesgo aplicado cuando `MoneyMode` es `RiskPercent`; ignorado en caso contrario. |
| `MaxPositions` | `int` | `1` | Número máximo de entradas aditivas por dirección (expresado como múltiplos del volumen de orden calculado). |
| `StopLossPips` | `int` | `150` | Distancia de stop-loss en pips. Cero desactiva el stop de protección. |
| `TakeProfitPips` | `int` | `150` | Distancia de take-profit en pips. Cero desactiva el objetivo de beneficio. |
| `TrailingStopPips` | `int` | `5` | Distancia del trailing stop en pips. Cero desactiva el trailing. |
| `TrailingStepPips` | `int` | `5` | Distancia extra que debe recorrer el precio antes de que avance el trailing stop. Debe ser positivo cuando el trailing está habilitado. |
| `JawPeriod` | `int` | `13` | Longitud de la media móvil jaw. |
| `JawShift` | `int` | `8` | Desplazamiento hacia adelante (en barras) aplicado a la serie jaw. |
| `TeethPeriod` | `int` | `8` | Longitud de la media móvil teeth. |
| `TeethShift` | `int` | `5` | Desplazamiento hacia adelante aplicado a la serie teeth. |
| `LipsPeriod` | `int` | `5` | Longitud de la media móvil lips. |
| `LipsShift` | `int` | `3` | Desplazamiento hacia adelante aplicado a la serie lips. |
| `MaType` | `MovingAverageType` | `Smoothed` | Algoritmo de media móvil usado para las tres líneas del Alligator. |
| `AppliedPrice` | `AppliedPriceType` | `Median` | Precio de la vela suministrado a las medias móviles (cierre, apertura, máximo, mínimo, mediano, típico o ponderado). |

### Conversión de pips

La estrategia multiplica la configuración de pips por el `PriceStep` del instrumento. Cuando el instrumento usa 3 o 5 decimales, el valor se ajusta por ×10 para imitar la definición de pip de MetaTrader para cotizaciones fraccionarias. Si no hay ningún paso de precio disponible, se asume un valor de 1.

## Notas de implementación

- `MaxPositions` actúa sobre el tamaño de posición agregada porque StockSharp opera en modo netting. Las entradas adicionales aumentan el precio promedio en lugar de crear tickets de posición separados.
- El stop-loss y el take-profit se rastrean internamente y se ejecutan con órdenes de mercado en la primera vela que viola los umbrales, coincidiendo con el comportamiento del experto MQL original.
- El dimensionamiento basado en riesgo requiere una distancia de stop-loss no nula; de lo contrario, el sistema recurre al `OrderVolume` fijo.
- Todos los valores de los indicadores se actualizan solo en velas terminadas (`CandleStates.Finished`) para evitar señales prematuras.
