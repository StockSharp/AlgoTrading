# Estrategia de ruptura de rango basada en el tiempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una adaptación directa del MetaTrader4 asesor experto `Tttttt_www_forex-instruments_info.mq4`. Crea niveles de ruptura intradía una vez al día a una hora configurable. Siempre que el precio cierra más allá de esos niveles, la estrategia abre una posición en la dirección de ruptura. Las salidas se gestionan mediante distancias dinámicas de pérdidas y ganancias que se derivan de un promedio de rangos diarios históricos.

## Lógica principal
1. **Hora de la instantánea diaria**: a las `CheckHour:CheckMinute` la estrategia congela los máximos y mínimos del día actual y cierra cualquier posición abierta.
2. **Cálculo del rango promedio**: el algoritmo agrega las últimas estadísticas `DaysToCheck`:
   - *CheckMode = 1*: utiliza el rango máximo/bajo completo de cada día completado.
   - *CheckMode = 2*: utiliza la diferencia absoluta entre los cierres de check-time de días consecutivos.
3. **Construcción de niveles**: el valor promedio se divide por `OffsetFactor` para crear una banda de ruptura superior e inferior alrededor del máximo/mínimo del día actual. El mismo promedio se divide por `ProfitFactor` y `LossFactor` para derivar las distancias dinámicas de toma de ganancias y parada.
4. **Ventana de entrada** – Después de la instantánea diaria, la estrategia observa cómo se cierra la vela hasta las 23:00. Si un precio de cierre atraviesa la banda superior y no hay ninguna posición abierta, compra; si se rompe la banda inferior, se vende. El número de entradas por día está limitado por `TradesPerDay`.
5. **Gestión de salida**: mientras está en una posición, la estrategia compara el precio de cierre con el precio de entrada promedio (`Strategy.PositionPrice`). Una vez que el movimiento a favor o en contra alcanza las distancias de ganancia o pérdida configuradas, la posición se cierra a mercado. Si es `CloseMode = 2`, cualquier posición sobrante también se cierra al comienzo del siguiente día de negociación.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CheckHour` | Hora (0-23) en la que se toma la instantánea del rango diario. | `8` |
| `CheckMinute` | Minuto (0-59) en el que se toma la instantánea. | `0` |
| `DaysToCheck` | Número de días históricos utilizados para promediar. | `7` |
| `CheckMode` | `1` = usa el rango máximo/bajo diario, `2` = usa la diferencia absoluta entre cierres de hora de verificación consecutivos. | `1` |
| `ProfitFactor` | Divide el valor promedio para obtener la distancia objetivo de ganancias. | `2` |
| `LossFactor` | Divide el valor promedio para obtener la distancia de pérdida. | `2` |
| `OffsetFactor` | Divide el valor promedio para obtener el desplazamiento de ruptura entre máximo y mínimo. | `2` |
| `CloseMode` | `1` = mantener posiciones durante la noche, `2` = aplanar cuando cambia el día calendario. | `1` |
| `TradesPerDay` | Número máximo de entradas permitidas por día. | `1` |
| `CandleType` | Serie de velas utilizada para todos los cálculos (el valor predeterminado es velas de 15 minutos). | `15m` período de tiempo |

Todos los parámetros se crean a través de `Strategy.Param` para que admitan la optimización desde el primer momento.

## Diferencias con la versión MQL
- MetaTrader rastrea directamente las ganancias flotantes; el puerto StockSharp lo reconstruye a partir de `Position` y `PositionPrice` al evaluar las salidas.
- El código MT4 contaba los pedidos activos a través de bucles de tickets. El puerto utiliza `TradesPerDay` junto con la posición agregada para mantener bajo control el número de operaciones del mismo día.
- El script original se basaba en buffers históricos (por ejemplo, `Highest`, `Lowest`). La versión StockSharp almacena estadísticas diarias internamente, evitando buffers de indicadores explícitos y respetando las pautas de alto nivel API.
- Se enviaron órdenes protectoras de stop-loss y take-profit junto con la entrada al mercado en MT4. El puerto realiza un control de riesgo equivalente al monitorear el cierre de velas y enviar órdenes de salida del mercado cuando se alcanzan los umbrales.

## Notas de uso
- Utilice una serie de velas que coincida con el tamaño de barra de la configuración original MQL (en el archivo de referencia se utilizaron barras de 15 minutos).
- Proporcione al menos `DaysToCheck` días completos de datos históricos antes de comenzar la estrategia; de lo contrario, los niveles de ruptura permanecerán inactivos.
- Al optimizar, mantenga los factores positivos para mantener umbrales de riesgo y ruptura significativos.
