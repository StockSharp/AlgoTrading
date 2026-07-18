# Estrategia Good Gbbi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre una única posición a una hora específica del día basándose en la diferencia entre precios de apertura históricos.

## Lógica

* Trabaja con velas horarias por defecto.
* A la hora `TradeTime` la estrategia compara el precio de apertura de hace `T1` barras con el precio de apertura de hace `T2` barras.
* Si la apertura más antigua es mayor que la reciente en `DeltaShort` puntos, se abre una posición corta.
* Si la apertura reciente es mayor que la más antigua en `DeltaLong` puntos, se abre una posición larga.
* Solo se permite un trade por día. El trading se habilita nuevamente cuando la hora supera `TradeTime`.
* Cada posición está protegida por niveles individuales de take-profit y stop-loss y puede cerrarse forzosamente después de `MaxOpenTime` horas.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitLong` | Distancia de take profit en puntos para posiciones largas. |
| `StopLossLong` | Distancia de stop loss en puntos para posiciones largas. |
| `TakeProfitShort` | Distancia de take profit en puntos para posiciones cortas. |
| `StopLossShort` | Distancia de stop loss en puntos para posiciones cortas. |
| `TradeTime` | Hora del día en que se verifican las condiciones de entrada. |
| `T1` | Número de barras hacia atrás para el primer precio de apertura. |
| `T2` | Número de barras hacia atrás para el segundo precio de apertura. |
| `DeltaLong` | Diferencia requerida en puntos para abrir una posición larga. |
| `DeltaShort` | Diferencia requerida en puntos para abrir una posición corta. |
| `MaxOpenTime` | Tiempo máximo de mantenimiento de posición en horas; 0 desactiva la verificación. |
| `CandleType` | Tipo de vela a procesar. |

## Notas

La idea original proviene del asesor experto de MetaTrader *GoodG@bi*. Este puerto utiliza la API de alto nivel de StockSharp y procesa únicamente velas terminadas. Asegúrese de que el `PriceStep` del instrumento esté correctamente configurado para interpretar los valores en puntos.
