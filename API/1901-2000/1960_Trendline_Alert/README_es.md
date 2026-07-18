# Estrategia de Alerta de Línea de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia monitorea dos líneas de tendencia definidas por el usuario y reacciona cuando el precio las rompe. Las líneas superior e inferior representan niveles de resistencia y soporte. Cuando el precio de cierre cruza por encima de la línea superior, se abre una posición larga. Cuando el precio cae por debajo de la línea inferior, se abre una posición corta. La lógica opcional de trailing stop protege las posiciones abiertas moviendo el nivel del stop en la dirección del trade.

## Parámetros

- `Breakout Points` – puntos adicionales añadidos a los niveles de la línea de tendencia para definir el umbral de ruptura.
- `Upper Line` – nivel de precio para la ruptura alcista.
- `Lower Line` – nivel de precio para la ruptura bajista.
- `Start Hour` – hora de inicio del trading en horas.
- `End Hour` – hora de fin del trading en horas.
- `Use Trailing Stop` – activa la gestión del trailing stop.
- `Trailing Stop Points` – distancia en puntos para el trailing stop.
- `Candle Type` – marco temporal de velas usado para el análisis.

## Cómo Funciona

1. La estrategia se suscribe a la serie de velas seleccionada.
2. Para cada vela cerrada verifica que el tiempo esté dentro de la ventana de trading especificada.
3. Se detecta una ruptura cuando el cierre de la vela cruza por encima de la línea superior o por debajo de la línea inferior, ajustado por el umbral de puntos de ruptura.
4. Cuando ocurre una ruptura, se envía una orden de mercado en la dirección de la ruptura si no hay posición existente.
5. Si el trailing stop está activado, el nivel del stop sigue al precio hasta que se activa.

## Notas

- La estrategia es una conversión simplificada del asesor experto original TrendlineAlert de MetaTrader. El dibujo manual de líneas de tendencia es reemplazado por niveles de precio fijos definidos por parámetros.
- No se realizan órdenes fuera de las horas de trading especificadas.
