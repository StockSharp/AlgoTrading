# Estrategia de dos bandas de cuatro niveles MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia recrea el MetaTrader asesor experto `ytg_2MA_4Level`. Compara un promedio móvil rápido con uno más lento y activa entradas cuando la curva rápida cruza la curva lenta, ya sea directamente o dentro de cuatro bandas de compensación configurables. Las posiciones están protegidas por distancias simétricas de stop-loss y take-profit expresadas en pips, al igual que en la implementación original.

## Lógica de señal
1. Se calculan dos medias móviles sobre la serie de velas seleccionada. Tanto el método de promedio (SMA, EMA, SMMA, LWMA) como el precio aplicado se pueden ajustar de forma independiente para las líneas rápidas y lentas.
2. En cada vela terminada, la estrategia toma muestras de las medias móviles `CalculationBar` barras hacia atrás (por defecto `1`) y también una barra antes. Esto refleja la llamada MetaTrader `iMA(..., shift)` y garantiza que solo las velas cerradas generen operaciones.
3. Una señal de **compra** se activa cuando el promedio rápido cruza por encima del lento, o cuando el cruce ocurre por encima/por debajo del promedio lento desplazado en `UpperLevel1`, `UpperLevel2`, `LowerLevel1` o `LowerLevel2` pips.
4. Una señal de **venta** utiliza las condiciones reflejadas con el promedio rápido cruzando por debajo de la línea lenta (y las mismas cuatro bandas de compensación).
5. La estrategia solo abre una nueva posición de mercado cuando no hay órdenes activas y la posición actual es plana, lo que coincide con el comportamiento de ticket único del experto MQL.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `TakeProfitPips` | `int` | `130` | Distancia de toma de ganancias en pips. Establezca en `0` para desactivar el objetivo. |
| `StopLossPips` | `int` | `1000` | Distancia de stop-loss en pips. Establezca en `0` para desactivar la parada de protección. |
| `TradeVolume` | `decimal` | `1` | Tamaño de lote base enviado con cada pedido (ajustado automáticamente a `VolumeStep`). |
| `CalculationBar` | `int` | `1` | Número de barras utilizadas como ancla para la comparación MA (MetaTrader `shift`). |
| `FastPeriod` / `SlowPeriod` | `int` | `14` / `180` | Duraciones de los períodos de las medias móviles. |
| `FastMethod` / `SlowMethod` | `MovingAverageMethod` | `Smoothed` | Técnica de promediado: `Simple`, `Exponential`, `Smoothed` o `LinearWeighted`. |
| `FastPrice` / `SlowPrice` | `CandlePrice` | `Median` | Precio aplicado utilizado por cada media móvil. |
| `UpperLevel1` / `UpperLevel2` | `int` | `500` / `250` | Compensaciones positivas (en pips) agregadas al MA lento para controles de tolerancia. |
| `LowerLevel1` / `LowerLevel2` | `int` | `500` / `250` | Compensaciones negativas (en pips) restadas del MA lento para controles de tolerancia. |
| `CandleType` | `DataType` | `15m` período de tiempo | Serie de velas sobre las que operan los indicadores. |

## Notas de implementación
- Las órdenes de stop-loss y take-profit se emulan a través de `StartProtection` con distancias convertidas de pips a unidades de precio utilizando el `PriceStep` del instrumento. Las cotizaciones FX de cinco dígitos reciben automáticamente el multiplicador MetaTrader estilo `*10`.
- Las colas internas almacenan sólo los datos necesarios para reproducir la lógica `shift`; no se acumula ningún historial completo de velas.
- Los pedidos se emiten con `BuyMarket` / `SellMarket` y heredan el volumen normalizado para que la interfaz de usuario refleje el tamaño del lote activo.
- La salida del gráfico dibuja la serie de velas junto con los promedios móviles y las operaciones ejecutadas para una inspección visual rápida.
- Todos los comentarios en línea están en inglés para cumplir con las pautas del proyecto.

## Consejos de uso
- Elija el mismo intervalo de vela que usaría en MetaTrader; la serie predeterminada de `15` minutos se puede cambiar a través de `CandleType`.
- Reduzca los niveles de compensación para hacer que las señales sean más selectivas o amplíelos para aceptar cruces más amplios.
- Establecer `CalculationBar` en `0` hace que la estrategia reaccione a la última vela cerrada (sin demora), mientras que los valores más altos mueven el disparador más hacia el pasado para una confirmación adicional.
- Desactive las patas protectoras (`StopLossPips = 0`, `TakeProfitPips = 0`) si las salidas deben gestionarse manualmente o mediante otro módulo.
