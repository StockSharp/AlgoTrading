# Estrategia de Media de Suavizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia opera en torno a una media móvil simple (SMA) con un desplazamiento de suavizado adicional. Intenta explotar las desviaciones del precio respecto a la media móvil entrando en posiciones cuando el precio de cierre cruza una distancia de desplazamiento desde la media.

## Cómo funciona
- Calcular una SMA del tipo de vela elegido.
- Si no hay posición abierta:
  - Entrar en posición corta cuando el precio de cierre esté por debajo de `SMA + Smoothing`.
  - Entrar en posición larga cuando el precio de cierre esté por encima de `SMA - Smoothing`.
- Para una posición corta abierta:
  - Cerrar la posición cuando el precio de cierre suba por encima de `SMA + Smoothing`.
- Para una posición larga abierta:
  - Cerrar la posición cuando el precio de cierre caiga por debajo de `SMA - Smoothing`.

La estrategia utiliza órdenes de mercado y trabaja únicamente con velas finalizadas.

## Parámetros
- **MA Period** – periodo de retrospección para la SMA.
- **Smoothing** – desplazamiento de precio añadido o restado de la SMA al generar señales.
- **Candle Type** – marco temporal de las velas utilizadas para los cálculos.

## Notas
Esta conversión está basada en el script MQL4 original `smoothingaverage.mq4`.
