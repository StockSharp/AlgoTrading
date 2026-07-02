# Estrategias cruzadas de precios medios móviles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Este paquete contiene dos puertos de estrategia C# de los 5 ejemplos de MetaTrader ubicados en `MQL/50198`:

* **`MovingAveragePriceCrossStrategy`**: un sistema minimalista de cruce de precios versus promedio móvil que negocia una sola posición a la vez.
* **`MovingAverageMartingaleStrategy`**: una versión mejorada que aplica el tamaño de las posiciones estilo martingala después de las pérdidas y al mismo tiempo conserva la misma lógica de cruce precio/promedio.

Ambas implementaciones se basan en StockSharp API de alto nivel, utilizan suscripciones de velas para la evaluación de señales y exponen parámetros compatibles con MetaTrader para distancias de stop-loss y take-profit.

## Archivos

| Archivo | Descripción |
| --- | --- |
| `CS/MovingAveragePriceCrossStrategy.cs` | Cruce precio base/MA utilizando volumen fijo y órdenes de protección estáticas. |
| `CS/MovingAverageMartingaleStrategy.cs` | Variante Martingale que escala el volumen y las distancias de protección después de perder operaciones. |

## Lógica comercial

### Estrategia cruzada de precio medio en movimiento

1. Se suscribe a velas del período de tiempo configurado y calcula una media móvil simple (`SMA`).
2. Evalúa señales solo en velas terminadas para imitar el comportamiento experto de MT5.
3. Detecta cruces entre el SMA y el precio de cierre de la vela utilizando las dos últimas velas completadas:
   * **Vender** cuando la media móvil sube por encima del cierre de la vela (el precio cruza por debajo de la media).
   * **Comprar** cuando la media móvil cae por debajo del cierre de la vela (el precio cruza por encima de la media).
4. Coloca una orden de mercado única por señal si no hay ninguna posición abierta actualmente.
5. Aplica protección automática a través de `StartProtection` con MetaTrader distancias de puntos convertidas en compensaciones de precios absolutas.

### En movimientoPromedioMartingalaEstrategia

1. Comparte la misma suscripción de vela y generación de señal SMA que la estrategia base.
2. Realiza un seguimiento del PnL realizado después de cada posición cerrada y almacena el último resultado comercial.
3. Cuando aparece una nueva señal de cruce y no hay ninguna posición abierta:
   * Si la última operación **genera pérdidas**, multiplica el siguiente volumen de operación por `VolumeMultiplier` (con un límite de `MaxVolume`) y aumenta las distancias de parada de pérdidas y obtención de ganancias en `TargetMultiplier`.
   * Si la última operación fue **rentable**, restablece el volumen de operaciones y las distancias de protección a sus valores iniciales.
4. Aplica `StartProtection` con las compensaciones ajustadas dinámicamente inmediatamente antes de enviar la orden de mercado.
5. Continúa negociando solo una posición a la vez, coincidiendo con la lógica original del asesor experto.

## Gestión de riesgos

* Los niveles de protección se expresan en MetaTrader puntos y se traducen automáticamente en compensaciones de precio absoluto utilizando el tamaño del pip detectado (`PriceStep` ajustado para símbolos FX de 3/5 decimales).
* La estrategia martingala mantiene acotados los multiplicadores de stop-loss y take-profit para evitar distancias descontroladas.
* El volumen de posición está alineado con el `VolumeStep`, `MinVolume` y el `MaxVolume` opcional del instrumento para evitar órdenes no válidas.

## Parámetros

### Entradas compartidas

| Parámetro | Estrategia | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | ambos | `1 minute` | Tipo de datos de vela utilizado para el cálculo de la señal. |
| `MaPeriod` | ambos | `50` | Longitud de la media móvil simple. |

### Estrategia cruzada de precio medio en movimiento

| Parámetro | Predeterminado | Descripción |
| --- | --- | --- |
| `OrderVolume` | `1` | Volumen de pedido alineado con el paso del instrumento. |
| `TakeProfitPoints` | `150` | Distancia de toma de ganancias en MetaTrader puntos (0 inhabilitaciones). |
| `StopLossPoints` | `150` | Distancia de stop-loss en MetaTrader puntos (0 inhabilitaciones). |

### En movimientoPromedioMartingalaEstrategia

| Parámetro | Predeterminado | Descripción |
| --- | --- | --- |
| `StartingVolume` | `1` | Volumen base restaurado después de operaciones rentables. |
| `MaxVolume` | `5` | Volumen máximo después de aplicar multiplicadores. |
| `TakeProfitPoints` | `100` | Distancia inicial de obtención de beneficios en MetaTrader puntos. |
| `StopLossPoints` | `300` | Distancia inicial de stop-loss en MetaTrader puntos. |
| `VolumeMultiplier` | `2` | Factor aplicado al siguiente volumen de orden después de una pérdida. |
| `TargetMultiplier` | `2` | Factor aplicado a las distancias de stop-loss y take-profit después de una pérdida. |

## Notas de uso

* MetaTrader “puntos” corresponden a un `PriceStep` para la mayoría de los instrumentos; las estrategias se multiplican automáticamente por 10 para que los símbolos FX de 3 o 5 decimales coincidan con el comportamiento de MT5.
* Ambas estrategias requieren solo un valor e ignorarán las señales mientras una posición esté abierta, reproduciendo la guardia `PositionsTotal()` de los expertos originales.
* Habilite la optimización de los parámetros expuestos dentro del diseñador StockSharp para replicar el ajuste de entrada MT5.
