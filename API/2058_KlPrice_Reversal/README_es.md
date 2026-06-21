# Estrategia de Reversión KlPrice
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión en C# del experto MQL5 original **exp_i-KlPrice.mq5**. Implementa un sistema de reversión basado en un oscilador de precio normalizado. El oscilador compara el precio actual con una banda de precio suavizada derivada de una media móvil y el rango verdadero promedio (ATR). Cruzar los límites predefinidos genera señales de trading.

## Cómo Funciona

1. Una media móvil simple (SMA) suaviza el precio de cierre.
2. Un Rango Verdadero Promedio (ATR) estima la volatilidad del mercado.
3. El oscilador se calcula como:
   
   `jres = 100 * (Close - (SMA - ATR)) / (2 * ATR) - 50`
4. El valor del oscilador se asigna a cinco zonas de color:
   - **4** – por encima del nivel superior
   - **3** – entre cero y el nivel superior
   - **2** – entre los niveles superior e inferior
   - **1** – entre el nivel inferior y cero
   - **0** – por debajo del nivel inferior
5. Una posición larga se abre cuando el oscilador sale de la zona 4. Una posición corta se abre cuando sale de la zona 0. Las posiciones existentes se cierran cuando el oscilador cruza cero.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `CandleType` | Marco temporal para los datos de precio. |
| `PriceMaLength` | Período de SMA para suavizar el precio. |
| `AtrLength` | Período de ATR para calcular la banda de precio. |
| `UpLevel` | Umbral superior del oscilador. |
| `DownLevel` | Umbral inferior del oscilador. |
| `EnableBuy` | Permitir apertura de posiciones largas. |
| `EnableSell` | Permitir apertura de posiciones cortas. |

## Uso

1. Crear una instancia de `KlPriceReversalStrategy`.
2. Establecer los parámetros deseados.
3. Adjuntar la estrategia a un portafolio y un activo.
4. Iniciar la estrategia para recibir señales y colocar órdenes.

La estrategia usa órdenes de mercado mediante `BuyMarket` y `SellMarket`. La protección de posición se activa a través de `StartProtection()`.

## Notas

- La implementación aproxima el indicador MQL original usando indicadores integrados de StockSharp (`SimpleMovingAverage` y `AverageTrueRange`).
- Todos los cálculos se realizan únicamente en velas completadas.
