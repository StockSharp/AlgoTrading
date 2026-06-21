# Estrategia Two Direction Martin Stylized
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un enfoque martingala bidireccional simplificado. Al inicio abre posiciones largas y cortas simultáneamente y coloca órdenes limitadas a una distancia configurable para capturar ganancias.

## Cómo funciona
1. Calcula el spread y establece la distancia del take profit como porcentaje del precio ask actual.
2. Envía una orden de venta de mercado inicial con un objetivo de compra limitada por debajo del bid y una orden de compra de mercado con un objetivo de venta limitada por encima del ask.
3. Cuando una de las órdenes limitadas falta o el precio se mueve fuera del rango predefinido, el algoritmo recalcula los volúmenes usando `Same Side %` y reemplaza las órdenes pendientes. Se envían órdenes de mercado adicionales para equilibrar la posición si es necesario.
4. Todas las órdenes se dividen en partes que no superen el parámetro `Volume Limit`.

## Parámetros
- **Take Profit %** – distancia desde el precio actual para los objetivos de ganancia.
- **Base Volume** – volumen mínimo para cada orden inicial.
- **Volume Limit** – volumen máximo para una sola parte de orden.
- **Same Side %** – porcentaje del volumen total asignado al lado dominante.
- **Candle Type** – tipo de vela utilizado como motor de tiempo.
