# Estrategia cruzada de Cronex DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Cronex DeMarker Crossover reproduce el indicador MetaTrader **Cronex DeMarker** y lo transforma en un sistema de comercio automatizado. El indicador original traza el oscilador DeMarker junto con dos promedios móviles ponderados lineales (LWMA). La estrategia refleja esa configuración, evalúa los cruces alcistas y bajistas entre las líneas suavizadas del oscilador y las convierte en órdenes de mercado. Esto permite que la lógica comercial reaccione inmediatamente cuando el impulso cambia de presión bajista a presión alcista (y viceversa) según el indicador.

## Construcción de indicadores
1. **Oscilador DeMarker** – Mide la relación entre la vela actual y la vela anterior:
   - Si el máximo actual es más alto que el máximo anterior, la presión positiva es igual a la diferencia de los máximos; en caso contrario es cero.
   - Si el mínimo actual es más bajo que el mínimo anterior, la presión negativa es igual a la distancia entre los mínimos; en caso contrario es cero.
   - Las sumas de la presión positiva y negativa sobre `DeMarkerPeriod` barras forman el valor del oscilador `deMax / (deMax + deMin)`.
2. **LWMA rápido**: se aplica una media móvil ponderada lineal con período `FastMaPeriod` a los valores sin procesar de DeMarker para enfatizar los últimos cambios del oscilador.
3. **LWMA lento**: otra media móvil ponderada lineal con período `SlowMaPeriod` suaviza el mismo flujo de DeMarker para crear una línea de confirmación más lenta.

La estrategia alimenta cada vela terminada a esta pila de indicadores, coincidiendo exactamente con los cálculos del buffer del archivo MQ4 original.

## Lógica comercial
1. Espere hasta que el oscilador DeMarker y ambos LWMA estén completamente formados.
2. Después de cada vela completa, calcule el nuevo valor de DeMarker y actualice ambos promedios móviles.
3. Detecta cruces entre las series LWMA rápida y lenta:
   - **Cruce alcista** – La LWMA rápida se mueve de abajo hacia arriba de la LWMA lenta. La estrategia cierra cualquier exposición corta y abre una posición larga en el mercado.
   - **Cruce bajista** – La LWMA rápida se mueve desde arriba hacia debajo de la LWMA lenta. La estrategia cierra cualquier exposición larga y abre una posición corta en el mercado.
4. Las órdenes se omiten mientras la estrategia aún no se haya formado, mientras esté fuera de línea o cuando el comercio esté deshabilitado.

Las posiciones se invierten inmediatamente ante señales opuestas. La exposición existente se cierra añadiendo la cantidad requerida a la nueva orden de mercado.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `DeMarkerPeriod` | Número de velas utilizadas para construir el oscilador DeMarker. | `25` |
| `FastMaPeriod` | Período de la media móvil ponderada lineal rápida que reacciona a nuevos valores del oscilador. | `14` |
| `SlowMaPeriod` | Periodo de la media móvil ponderada lineal lenta que confirma la dirección. | `25` |
| `CandleType` | Serie de velas procesadas por la estrategia (plazo u otro `DataType`). | `1 Hour` período de tiempo |

## Detalles de implementación
- Utiliza el nivel alto `SubscribeCandles` API. Los indicadores se actualizan solo cuando una vela alcanza el estado `Finished` para evitar que se vuelva a pintar a mitad de la barra.
- La estrategia se basa en los indicadores integrados `DeMarker` y `WeightedMovingAverage` de StockSharp para replicar fielmente los buffers MQ4.
- Se crea automáticamente un área del gráfico, que traza las velas de precios junto con el oscilador y ambos promedios móviles para una confirmación visual.
- `StartProtection()` se invoca durante el inicio para que la protección de posición se active exactamente una vez, como lo exigen las pautas del proyecto.

## Uso
1. Adjunte la estrategia al valor deseado y asigne el tipo de vela preferido (por ejemplo, velas con un marco de tiempo de 1 hora).
2. Configure el DeMarker y los períodos de media móvil para que coincidan con el indicador original o ajústelos para optimizarlos.
3. Ejecute la estrategia. Comenzará a operar una vez que los indicadores estén completamente formados y se permita el comercio.
4. Supervise el gráfico trazado para ver el oscilador DeMarker y las señales cruzadas LWMA que impulsan las entradas.
