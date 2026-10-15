# Estrategia de tres canales cruzados MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de tres canales cruzados MA** convierte el Expert Advisor de MetaTrader `3MaCross_EA` en la API de alto nivel de StockSharp. Monitorea tres promedios móviles configurables y abre operaciones cuando los promedios más rápidos cruzan el más lento. Opcionalmente se utiliza un canal de precios Donchian para gestionar las salidas, imitando fielmente el EA original que hacía referencia al indicador "Canal de precios".

## Lógica de trading
- **Entrada larga**: Se genera cuando las medias móviles rápida y media cierran por encima de la media móvil lenta y cualquiera de las dos medias móviles más rápidas cruza por encima de la lenta en la barra actual.
- **Entrada corta**: Se activa cuando los promedios móviles rápido y medio cierran por debajo del promedio móvil lento y cualquiera de los dos promedios móviles más rápidos cruza por debajo del lento.
- **Posición de salida**:
  - Señal de cruce opuesta.
  - Parada de canal Donchian opcional: las posiciones largas se cierran si el precio cae por debajo de la banda inferior; Las posiciones cortas se cierran si el precio sube por encima de la banda superior.
  - Distancias fijas opcionales de toma de ganancias o stop loss medidas en unidades de precio absoluto.

La estrategia siempre espera a que se completen las velas, coincidiendo con el comportamiento `TradeAtCloseBar` del script original. Sólo se mantiene una posición direccional a la vez; cuando aparece una señal contra una posición existente, la operación actual se cierra antes de que se abra una nueva.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
|------|------|---------|-------------|
| `FastLength` | `int` | `2` | Mirando hacia atrás para ver la media móvil rápida. |
| `MediumLength` | `int` | `4` | Mirando hacia atrás para la media móvil media. |
| `SlowLength` | `int` | `30` | Mirando hacia atrás para ver el promedio móvil lento. |
| `ChannelLength` | `int` | `15` | Donchian ventana de canal utilizada para salidas basadas en canales. |
| `FastType` | `MovingAverageTypeEnum` | `EMA` | Algoritmo de media móvil aplicado al promedio rápido (SMA, EMA, SMMA, WMA). |
| `MediumType` | `MovingAverageTypeEnum` | `EMA` | Algoritmo de media móvil aplicado a la media media. |
| `SlowType` | `MovingAverageTypeEnum` | `EMA` | Algoritmo de media móvil aplicado a la media lenta. |
| `TakeProfit` | `decimal` | `0` | Objetivo de beneficio en unidades de precio absoluto. Establezca en `0` para desactivar. |
| `StopLoss` | `decimal` | `0` | Límite de pérdida en unidades de precio absoluto. Establezca en `0` para desactivar. |
| `UseChannelStop` | `bool` | `true` | Habilita las salidas de canal Donchian. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Tipo de vela utilizada para los cálculos. |

## Notas
- Todos los promedios móviles utilizan precios de cierre y se pueden configurar individualmente para que coincidan con las opciones `FasterMode`, `MediumMode` y `SlowerMode` del EA original.
- `TakeProfit` y `StopLoss` utilizan distancias de precios absolutas (por ejemplo, `0.0010` corresponde a 10 pips en un símbolo Forex de 5 dígitos). Se evalúan en el cierre de velas, replicando la gestión de cierre de barras del EA.
- Cuando `UseChannelStop` está habilitado, la estrategia reproduce el comportamiento automático de stop-loss que se basaba en el indicador personalizado `Price Channel`.
- La estrategia dibuja los tres promedios móviles, el canal Donchian y los marcadores comerciales en el gráfico para confirmación visual.
