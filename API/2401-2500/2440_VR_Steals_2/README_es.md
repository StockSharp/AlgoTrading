# Estrategia VR Steals 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión de StockSharp del experto de MetaTrader 5 "VR---STEALS-2". Abre una única posición larga y demuestra una gestión simple de posiciones sin indicadores.

## Cómo funciona
1. Al iniciar, la estrategia compra usando `BuyMarket` y registra el precio de entrada.
2. Los datos de velas (1 minuto por defecto) se suscriben mediante `SubscribeCandles`.
3. Para cada vela completada:
   - Cuando el precio se ha movido `Breakeven` pasos a favor de la operación, el nivel de stop se mueve por encima de la entrada en `BreakevenOffset` pasos.
   - Si el precio alcanza la entrada más `TakeProfit` pasos, la posición se cierra.
   - Si el precio cae al nivel del stop (inicial `StopLoss` por debajo de la entrada o el stop de break-even movido), la posición se cierra.
4. Tras la salida, la estrategia no abre nuevas posiciones.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| TakeProfit | Distancia en pasos de precio al nivel de take-profit. | 50 |
| StopLoss | Distancia inicial del stop en pasos de precio. | 50 |
| Breakeven | Ganancia en pasos necesaria para activar el stop de break-even. | 20 |
| BreakevenOffset | Desplazamiento sobre la entrada cuando se establece el stop de break-even. | 9 |
| CandleType | Tipo de vela utilizado para el procesamiento de precios. | Marco temporal de 1 minuto |

Se usa `StartProtection()` para habilitar la protección integrada de posiciones.
