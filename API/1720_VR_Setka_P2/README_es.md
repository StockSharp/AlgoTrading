# Estrategia VR Setka P2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un enfoque de cuadrícula traducido del experto MetaTrader 4 `VR---SETKAp2`.
Opera cuando el cierre diario se desvía del máximo o mínimo del día en un porcentaje determinado.
La estrategia abre posiciones largas tras una caída significativa desde el máximo diario y
posiciones cortas tras un alza significativa desde el mínimo diario. Una vez en posición,
sale con una distancia de take profit fija. El volumen puede aumentar opcionalmente mediante un esquema simple de Martingale.

## Parámetros
- **TakeProfit** – distancia al objetivo de ganancia en pasos de precio.
- **Lot** – volumen base para cada operación.
- **Percent** – umbral porcentual calculado a partir del rango diario.
- **UseMartingale** – activa el aumento de volumen al añadir a una posición perdedora.
- **Slippage** – deslizamiento de precio permitido para las órdenes.
- **Correlation** – desplazamiento aplicado al calcular los niveles de cuadrícula.
- **Candle Type** – marco temporal usado para los cálculos (diario por defecto).

## Lógica
1. Suscribirse a velas diarias.
2. Para cada vela finalizada, calcular las desviaciones porcentuales desde el máximo y mínimo diarios.
3. Entrar en largo o corto según la desviación y la dirección de la vela anterior.
4. Cerrar la posición cuando se alcanza el nivel de take profit.

Esta implementación demuestra cómo se puede portar un experto de cuadrícula clásico de MetaTrader a la API de alto nivel de StockSharp.
