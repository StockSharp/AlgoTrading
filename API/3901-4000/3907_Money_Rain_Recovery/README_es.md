# Estrategia de recuperación de la lluvia de dinero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del MetaTrader asesor experto 4 **MoneyRain.mq4** al API de alto nivel de StockSharp.
- Negocia al cierre de velas terminadas utilizando un filtro oscilador DeMarker.
- Mantiene las salidas fijas originales de stop-loss/take-profit y el bloque de recuperación de volumen que aumenta el tamaño de la siguiente orden después de una secuencia de pérdidas.

## Lógica de trading
1. Suscríbase al `CandleType` configurado (predeterminado: velas de 1 hora) y calcule DeMarker con el período `DeMarkerPeriod`.
2. Cuando no hay ninguna posición activa y no hay ninguna orden pendiente:
   - Compre si el valor actual de DeMarker es superior a `Threshold`.
   - Vender de otra manera.
   - El tamaño de la orden es el volumen base o el volumen de recuperación calculado a partir de pérdidas anteriores.
3. Mientras una posición está abierta, la estrategia observa cada vela completada:
   - Los largos cierran cuando el mínimo de la vela toca el nivel de stop (`StopLossPoints` por debajo de la entrada) o el máximo de la vela alcanza el objetivo (`TakeProfitPoints` por encima de la entrada).
   - Los pantalones cortos reflejan las mismas reglas con niveles invertidos.
4. Después de cada salida, el bloque de gestión de dinero actualiza los contadores de pérdidas consecutivos y prepara el siguiente tamaño de orden. Cuando la racha de pérdidas alcanza `LossesLimit`, la estrategia deja de abrir nuevas posiciones y registra una advertencia.

## Gestión monetaria
- `BaseVolume` está normalizado según las reglas de intercambio (`Security.VolumeStep`, `Security.MinVolume`, `Security.MaxVolume`). Si el tamaño normalizado cae por debajo del lote mínimo, se omite la entrada.
- Después de cada operación perdedora, la estrategia almacena el volumen utilizado (escalado por el lote base) y reinicia el contador de ganancias consecutivas. La siguiente operación rentable utiliza la fórmula original de MoneyRain `baseLot × lossesVolume × (StopLoss + spread) / (TakeProfit − spread)` para recuperar pérdidas. Las ganancias posteriores vuelven al volumen base y el acumulador de pérdidas se liquida después de dos o más ganancias consecutivas.
- Si `FastOptimization` está habilitado, el bloque de recuperación se omite y cada entrada utiliza el volumen base normalizado.
- El diferencial de la fórmula de recuperación se estima a partir de la mejor oferta/demanda de nivel 1 más reciente. Si las cotizaciones no están disponibles, el diferencial vuelve a cero.

## Parámetros
| Parámetro | Descripción | Predeterminado | Notas |
|-----------|-------------|---------|-------|
| `DeMarkerPeriod` | Longitud del oscilador DeMarker. | `10` | Debe ser mayor que cero. |
| `TakeProfitPoints` | Distancia a la toma de ganancias en pasos de precio. | `50` | Convertido multiplicando por `Security.PriceStep`. |
| `StopLossPoints` | Distancia al stop-loss en pasos de precio. | `50` | Debe mantenerse positivo para que la fórmula de recuperación siga siendo válida. |
| `BaseVolume` | Volumen de pedidos inicial. | `1` | Normalizado a los límites del instrumento antes del envío. |
| `LossesLimit` | Se permiten operaciones perdedoras consecutivas máximas. | `1 000 000` | Cuando se alcanza, las entradas se pausan hasta que se restablece la estrategia. |
| `FastOptimization` | Deshabilite el tamaño de recuperación durante las ejecuciones del optimizador. | `true` | Mantiene el modelo liviano para pruebas masivas. |
| `Threshold` | Umbral de DeMarker que separa las señales de compra y venta. | `0.5` | Coincidencia de la constante MT4 del código fuente. |
| `CandleType` | Serie de datos de velas utilizadas para señales. | `1h` | Cambie por otros períodos de tiempo o agregaciones personalizadas. |

## Notas de uso
- Establezca los valores correctos de `Security.PriceStep`, `Security.VolumeStep`, `Security.MinVolume` y `Security.MaxVolume` para que las conversiones de precio/volumen sigan siendo válidas.
- Se requieren `StopLossPoints` y `TakeProfitPoints` positivos. Dejarlos en cero evita salidas, divergiendo del EA original.
- La estrategia espera los cumplimientos reales antes de actualizar su estado interno, por lo que maneja los cumplimientos parciales siguiendo el precio de salida ponderado.
- Cuando se activa el límite de pérdidas, no se realiza la siguiente operación rentable: reinicie o restablezca la estrategia para reanudar la operación.
