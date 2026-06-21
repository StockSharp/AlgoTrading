# Gestor de Stops Virtuales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia convertida desde el asesor de MetaTrader "VR---STEALS-3-EN". Implementa funciones ocultas de gestión de órdenes: stop-loss, take-profit, trailing stop y punto de equilibrio. La estrategia abre una posición larga en la primera vela y gestiona los niveles de salida virtualmente sin colocar órdenes de protección visibles en el mercado.

## Parámetros
- **Volume**: volumen de orden.
- **Take Profit (points)**: distancia en puntos para cerrar la posición con ganancia.
- **Stop Loss (points)**: distancia en puntos para cerrar la posición con pérdida.
- **Trailing Stop (points)**: distancia del trailing stop desde el precio más alto.
- **Breakeven (points)**: ganancia en puntos tras la cual el stop-loss se mueve al precio de entrada.
- **Candle Type**: serie de velas usada para el procesamiento.
