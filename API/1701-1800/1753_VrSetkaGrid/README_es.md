# Estrategia de Cuadrícula VR Setka
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una implementación en StockSharp del sistema de cuadrícula "VR---SETKAa3hM" de MetaTrader. Abre una secuencia de órdenes de compra o venta basadas en la desviación porcentual del rango diario y opcionalmente incrementa el volumen usando un multiplicador de martingala. El precio de entrada promedio de todas las órdenes abiertas se rastrea para colocar un objetivo unificado de take-profit.

## Parámetros
- `Distance`: Distancia en puntos entre los niveles de la cuadrícula.
- `TakeProfit`: Objetivo de beneficio en puntos para la orden inicial.
- `Correction`: Beneficio adicional en puntos añadido al precio promedio cuando hay más de una orden abierta.
- `SignalPercent`: Umbral porcentual usado para detectar desviación del rango diario.
- `UseMartingale`: Multiplicar el volumen por el número de órdenes abiertas.
- `CandleType`: Marco temporal de velas usado para los cálculos de señal.

## Lógica
1. Cuando aparece una vela finalizada, se calcula el cierre actual en relación con el máximo y mínimo del día.
2. Si la vela anterior era alcista y el cierre está suficientemente por debajo del máximo del día, se inicia o continúa una cuadrícula de compra.
3. Si la vela anterior era bajista y el cierre está suficientemente por encima del mínimo del día, se inicia o continúa una cuadrícula de venta.
4. Se colocan órdenes adicionales cada vez que el precio se mueve en contra de la posición en `Distance` puntos.
5. Una vez que el precio regresa al precio de entrada promedio más `Correction` para compras o menos `Correction` para ventas, todas las posiciones se cierran con una orden a mercado.
