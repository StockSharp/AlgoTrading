# Estrategia de Ilan iMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Ilan iMA** es un port de StockSharp del asesor experto de MetaTrader 5 `Ilan iMA.mq5`. El asesor combina un filtro de tendencia de media móvil desplazada con una cuadrícula de promediación estilo martingala. La versión de StockSharp reimplementa las mismas ideas con la API de alto nivel: cuando la media móvil ponderada confirma una tendencia, la estrategia abre una orden de mercado y sigue añadiendo operaciones cada vez que el precio se mueve contra la posición por un paso configurable. Toda la cesta se cierra cuando se alcanza un objetivo de ganancia, trailing stop o stop-loss explícito, reproduciendo el modelo de gestión de dinero del EA original.

## Lógica de trading
1. Suscribirse al marco temporal seleccionado (`CandleType`) y alimentar una media móvil configurable (`MaMethod`, `MaPeriod`, `PriceMode`). Un `MaShift` positivo desplaza el indicador hacia adelante, por lo que la estrategia evalúa valores históricos para imitar el comportamiento de MT5.
2. Esperar a que la vela cierre. Solo las barras finalizadas generan señales y actualizan la lógica de trailing/stop.
3. Detectar la tendencia comparando cuatro valores consecutivos de media móvil desplazados por `MaShift` barras:
   - valores estrictamente decrecientes señalan una tendencia bajista;
   - valores estrictamente crecientes señalan una tendencia alcista.
4. Cuando no hay ninguna cesta abierta:
   - en tendencia bajista, si el cierre está por encima del valor de la media móvil, abrir un short con `StartVolume`;
   - en tendencia alcista, si el cierre está por debajo del valor de la media móvil, abrir un long con `StartVolume`.
5. Cuando existe una cesta:
   - si el precio se mueve contra la posición al menos `GridStepPips`, abrir otra orden cuyo tamaño crece por `LotExponent` pero está limitado por `LotMaximum` y los límites de volumen del intercambio;
   - el precio de entrada promedio, el precio de compra más bajo y el precio de venta más alto se rastrean internamente para mantener el comportamiento cerca de la lógica de MT5.
6. Condiciones de cierre:
   - una vez que el beneficio flotante de una cesta con más de una operación alcanza `ProfitMinimum` (en moneda de cuenta), cerrar todas las órdenes en esa dirección;
   - si el beneficio flotante alcanza `TakeProfitPips` o la pérdida llega a `StopLossPips`, cerrar la cesta;
   - la protección de trailing se activa después de `TrailingStopPips + TrailingStepPips` puntos de movimiento favorable y se mueve en pasos de `TrailingStepPips`.

## Gestión de riesgos y dimensionamiento
- `StartVolume` replica el parámetro `StartLots` de MT5. Cada orden adicional multiplica el tamaño anterior por `LotExponent` respetando `LotMaximum` y los límites del lugar (`Security.MinVolume`, `Security.VolumeStep`, `Security.MaxVolume`).
- `ProfitMinimum` preserva el comportamiento de "liberación de bloqueo" de la versión MT5: una vez que la cuadrícula se recuperó de una cobertura e imprime el beneficio solicitado, todas las operaciones en esa dirección se cierran.
- Las distancias de stop-loss y take-profit se miden en pips (`StopLossPips`, `TakeProfitPips`). El método helper convierte pips en pasos de precio del intercambio usando `Security.PriceStep`.
- El bloque de trailing emula la implementación de MT5: el trailing comienza solo después de que el precio supera `TrailingStopPips + TrailingStepPips` y se actualiza en pasos discretos para evitar ajustes prematuros del stop.

## Parámetros
| Nombre | Tipo | Por defecto | Contraparte MT5 | Descripción |
| --- | --- | --- | --- | --- |
| `MaPeriod` | `int` | `15` | `Inp_MA_ma_period` | Período de la media móvil del filtro de tendencia. |
| `MaShift` | `int` | `5` | `Inp_MA_ma_shift` | Desplazamiento hacia adelante de la línea de media móvil en barras. |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | `Inp_MA_ma_method` | Algoritmo de suavizado (SMA, EMA, SMMA, LWMA). |
| `PriceMode` | `CandlePrice` | `Weighted` | `Inp_MA_applied_price` | Precio de vela alimentado al indicador. |
| `StartVolume` | `decimal` | `1` | `InpStartLots` | Volumen base de la orden para la primera operación de una cesta. |
| `GridStepPips` | `decimal` | `30` | `InpStep` | Distancia (en pips) entre entradas de promediación. |
| `LotExponent` | `decimal` | `1.6` | `InpLotExponent` | Multiplicador aplicado al tamaño de la orden anterior. |
| `LotMaximum` | `decimal` | `15` | `InpLotMaximum` | Límite máximo para un solo volumen de orden. |
| `ProfitMinimum` | `decimal` | `15` | `InpProfitMinimum` | Beneficio flotante mínimo requerido para cerrar una cesta con varias operaciones. |
| `StopLossPips` | `decimal` | `0` | `InpStopLoss` | Distancia del stop-loss expresada en pips (0 deshabilita el stop). |
| `TakeProfitPips` | `decimal` | `100` | `InpTakeProfit` | Distancia del take-profit expresada en pips. |
| `TrailingStopPips` | `decimal` | `15` | `InpTrailingStop` | Umbral de beneficio que activa el trailing stop. |
| `TrailingStepPips` | `decimal` | `5` | `InpTrailingStep` | Beneficio adicional mínimo antes de que el trailing stop se mueva de nuevo. |
| `CandleType` | `DataType` | Marco temporal de 15 minutos | período del gráfico | Marco temporal utilizado para el cálculo de señales. |

## Diferencias del EA original
- StockSharp funciona en un entorno de netting, por lo que solo existe una posición neta por dirección. La estrategia mantiene una lista interna de precios de entrada y volúmenes para emular la contabilidad de cesta de MT5.
- Los límites de volumen específicos del intercambio siempre se respetan al redondear volúmenes, mientras que el código de MT5 dependía de verificaciones manuales. Esto previene órdenes que serían rechazadas por el conector del broker.
- La lógica de stop-loss, take-profit y trailing se expresa a través de salidas de mercado en lugar de modificar posiciones existentes de MT5. El comportamiento funcional sigue siendo el mismo, pero la gestión de órdenes es manejada por StockSharp.

## Notas de uso
- Asegúrese de que los metadatos del instrumento (`PriceStep`, `StepPrice`, `MinVolume`, `VolumeStep`, `MaxVolume`) estén completos en el conector para que las conversiones de pip a precio y el redondeo de volumen funcionen correctamente.
- El bloque de trailing asume que el tamaño del pip es igual al paso de precio del intercambio. Ajuste `GridStepPips`, `StopLossPips` y `TrailingStopPips` para instrumentos con tamaños de tick no convencionales.
- Las cuadrículas de martingala son inherentemente arriesgadas. Pruebe la estrategia en datos históricos y use configuraciones realistas de comisión/slippage antes de implementar en producción.
