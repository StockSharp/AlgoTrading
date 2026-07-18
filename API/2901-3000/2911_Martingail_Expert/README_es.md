# Estrategia Martingail Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Puerto del asesor experto de MetaTrader 5 **MartingailExpert.mq5**.
- Usa un cruce de oscilador estocástico con parámetros configurables %K, %D y ralentización para abrir posiciones.
- Implementa una cuadrícula de estilo martingala con entradas de promediado y ruptura que escalan el volumen geométricamente.
- Diseñado para portafolios de posición neta: la estrategia mantiene una única posición larga o corta agregada.

## Lógica de trading
### Criterios de entrada
1. La estrategia procesa velas cerradas del marco temporal `CandleType`.
2. Los valores estocásticos se toman de la vela terminada anterior para imitar la llamada MQL `iStochastic(..., 1)`.
3. Se activa una entrada larga cuando:
   - El %K anterior es mayor que el %D anterior.
   - El %D anterior está por encima de `BuyLevel`.
   - No existe ninguna posición abierta.
4. Se activa una entrada corta cuando:
   - El %K anterior es menor que el %D anterior.
   - El %D anterior está por debajo de `SellLevel`.
   - No existe ninguna posición abierta.
5. Todas las órdenes de mercado usan el valor `Volume` normalizado (redondeado al `Security.VolumeStep` más cercano).

### Escalado de posición
- `ProfitPips` define la distancia (en pips) requerida para agregar otra posición base en la dirección de la ganancia.
  - En largo: si el máximo de la vela alcanza `lastEntryPrice + ProfitPips * positionCount`, se envía una nueva orden con el `Volume` base.
  - En corto: si el mínimo de la vela alcanza `lastEntryPrice - ProfitPips * positionCount`, se envía una orden base de venta.
- `StepPips` define la distancia de promediado (en pips) para aplicar el multiplicador martingala.
  - En largo: si el mínimo de la vela toca `lastEntryPrice - StepPips`, el siguiente volumen de orden es `lastVolume * Multiplier`.
  - En corto: si el máximo de la vela toca `lastEntryPrice + StepPips`, se aplica el mismo dimensionamiento martingala.
- Cada operación ejecutada actualiza `lastEntryPrice`, `lastVolume` y el recuento interno de posiciones activas.

### Lógica de salida
- El precio del último trade ejecutado se almacena por dirección.
- Si el precio alcanza `lastEntryPrice ± ProfitPips` (usando máximos de vela para largos y mínimos para cortos), todas las posiciones abiertas se cierran mediante orden de mercado.
- Una vez que la posición agregada vuelve a cero, las variables de estado martingala se reinician.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Volume` | `0.03` | Tamaño de lote base para la orden inicial y los complementos basados en ganancia. |
| `Multiplier` | `1.6` | Multiplicador martingala para entradas de promediado. |
| `StepPips` | `25` | Distancia en pips que activa órdenes de promediado contra la tendencia. |
| `ProfitPips` | `9` | Distancia en pips usada tanto para salidas de ganancia como para complementos de ruptura. |
| `KPeriod` | `5` | Período de lookback del cálculo estocástico %K. |
| `DPeriod` | `3` | Período de suavizado de la línea estocástica %D. |
| `Slowing` | `3` | Suavizado aplicado a la línea %K (estocástico lento). |
| `BuyLevel` | `20` | Valor mínimo de %D requerido para permitir entradas largas. |
| `SellLevel` | `55` | Valor máximo de %D requerido para permitir entradas cortas. |
| `CandleType` | marco temporal de 5 minutos | Marco temporal usado para construir velas e indicadores. |

## Notas de implementación
- La distancia en pips se calcula a partir de `Security.PriceStep`. Los instrumentos con cotizaciones de 3 o 5 decimales se ajustan automáticamente multiplicando el paso de precio por 10 para coincidir con la lógica de pip MQL original.
- Los volúmenes se redondean hacia abajo al `Security.VolumeStep` más cercano. Los valores que caen por debajo del paso negociable mínimo son ignorados.
- La estrategia depende de los máximos y mínimos de las velas para aproximar los disparadores intra-barra porque la API de alto nivel opera sobre velas terminadas.
- `OnOwnTradeReceived` rastrea los precios y volúmenes de ejecución reales para reproducir fielmente la secuencia de escalada martingala.

## Consejos de uso
- Alinee `CandleType` con el marco temporal utilizado en la plantilla original de MetaTrader (comúnmente M5) para obtener un comportamiento similar.
- Asegúrese de que los metadatos del instrumento (paso de precio, paso de volumen) estén completados; de lo contrario, ajuste `Volume`, `StepPips` y `ProfitPips` manualmente para adaptarse a las especificaciones del bróker.
- Considere habilitar gestión de riesgo externa (stops o límites de capital) porque la lógica martingala aumenta intencionalmente la exposición durante movimientos adversos.

## Diferencias con el asesor experto original
- La versión de StockSharp procesa velas completadas en lugar de cada tick; las verificaciones de umbral usan máximos/mínimos de velas para aproximar el comportamiento intra-barra.
- Las verificaciones de margen de cuenta específicas de MetaTrader no están disponibles en las estrategias de alto nivel de StockSharp; asegúrese de que el capital adecuado esté configurado externamente.
- La ejecución de órdenes y el seguimiento de posiciones aprovechan el modelo de netting de StockSharp; el modo de cobertura no es compatible.
