# Estrategia BandsPrice
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación del experto **i-BandsPrice** de MetaTrader. Utiliza las Bandas de Bollinger para medir la posición relativa del precio dentro del canal y reacciona cuando el valor sale de las zonas extremas.

## Lógica

1. Construir Bandas de Bollinger con período y desviación configurables.
2. Calcular la posición del precio dentro de la banda como un valor entre -50 y +50.
3. Suavizar el valor con una media móvil simple.
4. Generar un código de color:
   - `4` cuando el valor suavizado está por encima del nivel superior.
   - `0` cuando el valor suavizado está por debajo del nivel inferior.
   - Otros números representan zonas intermedias.
5. Se abre una posición larga cuando el indicador sale de la zona superior (`4` → no `4`).
6. Se abre una posición corta cuando el indicador sale de la zona inferior (`0` → positivo).
7. Las posiciones largas se cierran cuando el valor se vuelve no positivo.
8. Las posiciones cortas se cierran cuando el valor se vuelve no negativo.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| **BuyOpen** | Activar entradas largas. |
| **SellOpen** | Activar entradas cortas. |
| **BuyClose** | Activar cierre de posiciones largas. |
| **SellClose** | Activar cierre de posiciones cortas. |
| **BandsPeriod** | Período de las Bandas de Bollinger. |
| **BandsDeviation** | Desviación para las bandas. |
| **Smooth** | Longitud de suavizado para el valor interno. |
| **UpLevel** | Umbral superior, por defecto `25`. |
| **DnLevel** | Umbral inferior, por defecto `-25`. |
| **CandleType** | Marco temporal de velas usado para los cálculos. |

## Notas

Esta estrategia demuestra cómo migrar la lógica basada en indicadores de MetaTrader a StockSharp usando la API de alto nivel con `SubscribeCandles` y `Bind`.
